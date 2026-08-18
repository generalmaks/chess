import { Service, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import * as signalR from '@microsoft/signalr';
import { firstValueFrom } from 'rxjs';

import { API_BASE_URL } from './api-config';
import { Auth } from './auth';
import {
  CreateGameResponse,
  GameResult,
  GameStateDto,
  JoinGameResponse,
  MoveRequest,
  Team,
  TimeControl,
} from './models/game.models';

// Well inside the backend's 60-minute JWT expiry, so a long-running game never
// gets kicked mid-match while the token stored for reconnects goes stale.
const TOKEN_REFRESH_INTERVAL_MS = 20 * 60 * 1000;

const CLOCK_TICK_INTERVAL_MS = 200;

@Service()
export class Game {
  private readonly http = inject(HttpClient);
  private readonly auth = inject(Auth);

  private connection: signalR.HubConnection | null = null;
  private refreshTimer: ReturnType<typeof setInterval> | null = null;
  private tickTimer: ReturnType<typeof setInterval> | null = null;
  private stateReceivedAtMs = Date.now();

  private readonly gameIdSignal = signal<string | null>(null);
  private readonly myTeamSignal = signal<Team | null>(null);
  private readonly boardSignal = signal<(string | null)[][] | null>(null);
  private readonly currentTurnSignal = signal<Team | null>(null);
  private readonly resultSignal = signal<GameResult>('Ongoing');
  private readonly errorSignal = signal<string | null>(null);
  private readonly opponentJoinedSignal = signal(false);
  private readonly drawOfferedByOpponentSignal = signal(false);
  private readonly whiteTimeRemainingBaseSignal = signal<number | null>(null);
  private readonly blackTimeRemainingBaseSignal = signal<number | null>(null);
  private readonly nowSignal = signal(Date.now());

  readonly gameId = this.gameIdSignal.asReadonly();
  readonly myTeam = this.myTeamSignal.asReadonly();
  readonly board = this.boardSignal.asReadonly();
  readonly currentTurn = this.currentTurnSignal.asReadonly();
  readonly result = this.resultSignal.asReadonly();
  readonly error = this.errorSignal.asReadonly();
  readonly opponentJoined = this.opponentJoinedSignal.asReadonly();
  readonly drawOfferedByOpponent = this.drawOfferedByOpponentSignal.asReadonly();

  readonly isMyTurn = computed(
    () => this.result() === 'Ongoing' && this.opponentJoined() && this.currentTurn() === this.myTeam()
  );

  readonly whiteTimeRemainingSeconds = computed(() => this.liveTimeRemaining('White'));
  readonly blackTimeRemainingSeconds = computed(() => this.liveTimeRemaining('Black'));

  createGame(color?: 'white' | 'black', timeControl?: TimeControl): Promise<CreateGameResponse> {
    const params = new URLSearchParams();
    if (color) params.set('color', color);
    if (timeControl) {
      params.set('initialMinutes', String(timeControl.initialMinutes));
      params.set('incrementSeconds', String(timeControl.incrementSeconds));
    }

    const query = params.size > 0 ? `?${params.toString()}` : '';
    return firstValueFrom(this.http.post<CreateGameResponse>(`${API_BASE_URL}/games${query}`, {}));
  }

  async joinGame(gameId: string): Promise<void> {
    await this.ensureConnected();

    const response = await this.connection!.invoke<JoinGameResponse>('JoinGame', gameId);

    this.gameIdSignal.set(gameId);
    this.myTeamSignal.set(response.team);
    this.opponentJoinedSignal.set(response.opponentJoined);
    this.applyState(response.state);

    this.refreshTimer = setInterval(() => {
      firstValueFrom(this.auth.refresh()).catch(() => {});
    }, TOKEN_REFRESH_INTERVAL_MS);
  }

  async makeMove(fromX: number, fromY: number, toX: number, toY: number, promotion?: string): Promise<void> {
    this.errorSignal.set(null);

    const request: MoveRequest = { fromX, fromY, toX, toY, promotion: promotion ?? null };
    try {
      await this.connection!.invoke('MakeMove', request);
    } catch (err) {
      this.errorSignal.set(errorMessage(err));
    }
  }

  async resign(): Promise<void> {
    try {
      await this.connection!.invoke('Resign');
    } catch (err) {
      this.errorSignal.set(errorMessage(err));
    }
  }

  async offerDraw(): Promise<void> {
    try {
      await this.connection!.invoke('OfferDraw');
    } catch (err) {
      this.errorSignal.set(errorMessage(err));
    }
  }

  async respondToDraw(accept: boolean): Promise<void> {
    this.drawOfferedByOpponentSignal.set(false);
    try {
      await this.connection!.invoke('RespondToDraw', accept);
    } catch (err) {
      this.errorSignal.set(errorMessage(err));
    }
  }

  async leave(): Promise<void> {
    if (this.refreshTimer !== null) {
      clearInterval(this.refreshTimer);
      this.refreshTimer = null;
    }
    if (this.tickTimer !== null) {
      clearInterval(this.tickTimer);
      this.tickTimer = null;
    }

    await this.connection?.stop();
    this.connection = null;
    this.gameIdSignal.set(null);
    this.myTeamSignal.set(null);
    this.boardSignal.set(null);
    this.currentTurnSignal.set(null);
    this.resultSignal.set('Ongoing');
    this.errorSignal.set(null);
    this.opponentJoinedSignal.set(false);
    this.drawOfferedByOpponentSignal.set(false);
    this.whiteTimeRemainingBaseSignal.set(null);
    this.blackTimeRemainingBaseSignal.set(null);
  }

  private async ensureConnected(): Promise<void> {
    if (this.connection && this.connection.state === signalR.HubConnectionState.Connected) {
      return;
    }

    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${API_BASE_URL}/hubs/chess`, { accessTokenFactory: () => this.auth.getToken() ?? '' })
      .withAutomaticReconnect()
      .build();

    this.connection.on('StateUpdated', (state: GameStateDto) => this.applyState(state));
    this.connection.on('PlayerJoined', () => {
      this.opponentJoinedSignal.set(true);
      this.stateReceivedAtMs = Date.now();
      this.nowSignal.set(this.stateReceivedAtMs);
    });
    this.connection.on('DrawOffered', () => this.drawOfferedByOpponentSignal.set(true));
    this.connection.on('DrawDeclined', () => this.drawOfferedByOpponentSignal.set(false));

    await this.connection.start();
  }

  private applyState(state: GameStateDto): void {
    this.boardSignal.set(state.board);
    this.currentTurnSignal.set(state.currentTurn);
    this.resultSignal.set(state.result);
    this.whiteTimeRemainingBaseSignal.set(state.whiteTimeRemainingSeconds);
    this.blackTimeRemainingBaseSignal.set(state.blackTimeRemainingSeconds);
    this.stateReceivedAtMs = Date.now();
    this.nowSignal.set(this.stateReceivedAtMs);

    if (state.whiteTimeRemainingSeconds !== null && this.tickTimer === null) {
      this.tickTimer = setInterval(() => this.nowSignal.set(Date.now()), CLOCK_TICK_INTERVAL_MS);
    }
  }

  private liveTimeRemaining(team: Team): number | null {
    const base = team === 'White' ? this.whiteTimeRemainingBaseSignal() : this.blackTimeRemainingBaseSignal();
    if (base === null) {
      return null;
    }
    if (!this.opponentJoinedSignal() || this.resultSignal() !== 'Ongoing' || this.currentTurnSignal() !== team) {
      return base;
    }

    const elapsedSeconds = (this.nowSignal() - this.stateReceivedAtMs) / 1000;
    return Math.max(0, base - elapsedSeconds);
  }
}

function errorMessage(err: unknown): string {
  if (err instanceof Error) {
    return err.message;
  }
  return 'Something went wrong.';
}
