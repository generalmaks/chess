import { Service, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import * as signalR from '@microsoft/signalr';
import { firstValueFrom } from 'rxjs';

import { API_BASE_URL } from './api-config';
import { Auth } from './auth';
import { Board, BoardMove, applyBoardMove, moveToAlgebraicNotation } from './algebraic-notation';
import {
  CreateGameResponse,
  GameHistoryEntry,
  GameResult,
  GameReviewDto,
  GameStateDto,
  JoinGameResponse,
  MoveHistoryEntryDto,
  MoveRequest,
  PagedResult,
  SquareCoordinate,
  Team,
  TimeControl,
} from './models/game.models';

const INITIAL_BOARD: Board = [
  ['wR', 'wP', null, null, null, null, 'bP', 'bR'],
  ['wN', 'wP', null, null, null, null, 'bP', 'bN'],
  ['wB', 'wP', null, null, null, null, 'bP', 'bB'],
  ['wQ', 'wP', null, null, null, null, 'bP', 'bQ'],
  ['wK', 'wP', null, null, null, null, 'bP', 'bK'],
  ['wB', 'wP', null, null, null, null, 'bP', 'bB'],
  ['wN', 'wP', null, null, null, null, 'bP', 'bN'],
  ['wR', 'wP', null, null, null, null, 'bP', 'bR'],
];

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

  private readonly reviewModeSignal = signal(false);
  private readonly whiteNameSignal = signal<string | null>(null);
  private readonly blackNameSignal = signal<string | null>(null);

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
  private readonly moveHistorySignal = signal<string[]>([]);
  // positions[0] is the board when this client started observing; positions[i] (i >= 1)
  // is the board right after moveHistory[i - 1]. Lets the UI jump back to any played ply.
  private readonly positionsSignal = signal<Board[]>([]);

  readonly reviewMode = this.reviewModeSignal.asReadonly();
  readonly whiteName = this.whiteNameSignal.asReadonly();
  readonly blackName = this.blackNameSignal.asReadonly();
  readonly gameId = this.gameIdSignal.asReadonly();
  readonly myTeam = this.myTeamSignal.asReadonly();
  readonly board = this.boardSignal.asReadonly();
  readonly currentTurn = this.currentTurnSignal.asReadonly();
  readonly result = this.resultSignal.asReadonly();
  readonly moveHistory = this.moveHistorySignal.asReadonly();
  readonly positions = this.positionsSignal.asReadonly();
  readonly error = this.errorSignal.asReadonly();
  readonly opponentJoined = this.opponentJoinedSignal.asReadonly();
  readonly drawOfferedByOpponent = this.drawOfferedByOpponentSignal.asReadonly();

  readonly isMyTurn = computed(
    () => this.result() === 'Ongoing' && this.opponentJoined() && this.currentTurn() === this.myTeam()
  );

  readonly myName = computed(() => this.nameForTeam(this.myTeamSignal()));
  readonly opponentName = computed(() => this.nameForTeam(this.opponentOf(this.myTeamSignal())));

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

  getHistory(page: number, pageSize = 10): Promise<PagedResult<GameHistoryEntry>> {
    const query = new URLSearchParams({ page: String(page), pageSize: String(pageSize) });
    return firstValueFrom(
      this.http.get<PagedResult<GameHistoryEntry>>(`${API_BASE_URL}/games/history?${query.toString()}`)
    );
  }

  async joinGame(gameId: string): Promise<void> {
    await this.ensureConnected();

    const response = await this.connection!.invoke<JoinGameResponse>('JoinGame', gameId);

    this.gameIdSignal.set(gameId);
    this.myTeamSignal.set(response.team);
    this.whiteNameSignal.set(response.whitePlayerName);
    this.blackNameSignal.set(response.blackPlayerName);
    this.opponentJoinedSignal.set(response.opponentJoined);
    this.applyState(response.state);
    this.replayHistory(response.moves, response.state.result);

    this.refreshTimer = setInterval(() => {
      firstValueFrom(this.auth.refresh()).catch(() => {});
    }, TOKEN_REFRESH_INTERVAL_MS);
  }

  async loadReview(gameId: string): Promise<void> {
    await this.connection?.stop();
    this.connection = null;

    const review = await firstValueFrom(
      this.http.get<GameReviewDto>(`${API_BASE_URL}/games/${gameId}`)
    );

    const { positions, sans } = reconstructGame(review.moves, review.result);

    this.reviewModeSignal.set(true);
    this.whiteNameSignal.set(review.whitePlayerName);
    this.blackNameSignal.set(review.blackPlayerName);
    this.gameIdSignal.set(gameId);
    this.myTeamSignal.set(review.playerColor);
    this.opponentJoinedSignal.set(true);
    this.resultSignal.set(review.result);
    this.currentTurnSignal.set(review.moves.length % 2 === 0 ? 'White' : 'Black');
    this.whiteTimeRemainingBaseSignal.set(null);
    this.blackTimeRemainingBaseSignal.set(null);
    this.boardSignal.set(positions[positions.length - 1]);
    this.positionsSignal.set(positions);
    this.moveHistorySignal.set(sans);
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

  // Delegates to the backend's real move generator (accounts for checks, pins, castling,
  // en passant) rather than reimplementing chess rules on the client.
  async getLegalMoves(x: number, y: number): Promise<SquareCoordinate[]> {
    if (!this.connection) {
      return [];
    }
    try {
      return await this.connection.invoke<SquareCoordinate[]>('GetLegalMoves', x, y);
    } catch {
      return [];
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
    this.reviewModeSignal.set(false);
    this.whiteNameSignal.set(null);
    this.blackNameSignal.set(null);
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
    this.moveHistorySignal.set([]);
    this.positionsSignal.set([]);
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
    this.connection.on('PlayerJoined', (team: Team, name: string | null) => {
      this.opponentJoinedSignal.set(true);
      if (team === 'White') this.whiteNameSignal.set(name);
      else this.blackNameSignal.set(name);
      this.stateReceivedAtMs = Date.now();
      this.nowSignal.set(this.stateReceivedAtMs);
    });
    this.connection.on('DrawOffered', () => this.drawOfferedByOpponentSignal.set(true));
    this.connection.on('DrawDeclined', () => this.drawOfferedByOpponentSignal.set(false));

    await this.connection.start();
  }

  private applyState(state: GameStateDto): void {
    const previousBoard = this.boardSignal();
    const movedTeam = this.currentTurnSignal();

    this.boardSignal.set(state.board);
    this.currentTurnSignal.set(state.currentTurn);
    this.resultSignal.set(state.result);
    this.whiteTimeRemainingBaseSignal.set(state.whiteTimeRemainingSeconds);
    this.blackTimeRemainingBaseSignal.set(state.blackTimeRemainingSeconds);
    this.stateReceivedAtMs = Date.now();
    this.nowSignal.set(this.stateReceivedAtMs);

    if (previousBoard === null) {
      this.positionsSignal.set([state.board]);
    } else if (movedTeam) {
      this.recordMove(previousBoard, movedTeam, state);
    }

    if (state.whiteTimeRemainingSeconds !== null && this.tickTimer === null) {
      this.tickTimer = setInterval(() => this.nowSignal.set(Date.now()), CLOCK_TICK_INTERVAL_MS);
    }
  }

  // Backend only ever pushes full board snapshots, so each move is reconstructed by
  // diffing consecutive boards rather than received as an explicit from/to.
  private recordMove(previousBoard: Board, movedTeam: Team, state: GameStateDto): void {
    if (boardsEqual(previousBoard, state.board)) {
      return;
    }

    const move = inferBoardMove(previousBoard, state.board, movedTeam);
    if (!move) {
      return;
    }

    try {
      const san = moveToAlgebraicNotation(previousBoard, move, state.board, state.result);
      this.moveHistorySignal.update((moves) => [...moves, san]);
      this.positionsSignal.update((positions) => [...positions, state.board]);
    } catch {
      // Unreconstructable diff (e.g. joining mid-game with no prior snapshot); skip it.
    }
  }

  // Rebuilds moveHistory/positions from the persisted moves the hub returns on join,
  // overwriting the single-entry arrays applyState just seeded above.
  private replayHistory(moves: MoveHistoryEntryDto[], finalResult: GameResult): void {
    const { positions, sans } = reconstructGame(moves, finalResult);
    this.positionsSignal.set(positions);
    this.moveHistorySignal.set(sans);
  }

  private nameForTeam(team: Team | null): string | null {
    if (team === 'White') return this.whiteNameSignal();
    if (team === 'Black') return this.blackNameSignal();
    return null;
  }

  private opponentOf(team: Team | null): Team | null {
    if (team === 'White') return 'Black';
    if (team === 'Black') return 'White';
    return null;
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

function reconstructGame(
  moves: MoveHistoryEntryDto[],
  finalResult: GameResult
): { positions: Board[]; sans: string[] } {
  let board = INITIAL_BOARD;
  const positions: Board[] = [board];
  const sans: string[] = [];

  moves.forEach((entry, i) => {
    const move: BoardMove = {
      fromX: entry.fromX,
      fromY: entry.fromY,
      toX: entry.toX,
      toY: entry.toY,
      promotion: entry.promotedTo,
    };
    const next = applyBoardMove(board, move);
    const resultAfter = i === moves.length - 1 ? finalResult : 'Ongoing';
    sans.push(moveToAlgebraicNotation(board, move, next, resultAfter));
    positions.push(next);
    board = next;
  });

  return { positions, sans };
}

function errorMessage(err: unknown): string {
  if (err instanceof Error) {
    return err.message;
  }
  return 'Something went wrong.';
}

function boardsEqual(a: Board, b: Board): boolean {
  for (let x = 0; x < 8; x++) {
    for (let y = 0; y < 8; y++) {
      if (a[x][y] !== b[x][y]) {
        return false;
      }
    }
  }
  return true;
}

// Recovers the from/to squares (and promotion, if any) the mover's team produced,
// since the hub only ever broadcasts full board snapshots, never the move itself.
function inferBoardMove(before: Board, after: Board, movedTeam: Team): BoardMove | null {
  const prefix = movedTeam === 'White' ? 'w' : 'b';
  const vacated: { x: number; y: number }[] = [];
  const occupied: { x: number; y: number }[] = [];

  for (let x = 0; x < 8; x++) {
    for (let y = 0; y < 8; y++) {
      const b = before[x][y];
      const a = after[x][y];
      if (b === a) continue;
      if (b?.[0] === prefix && a === null) vacated.push({ x, y });
      if (a?.[0] === prefix && b?.[0] !== prefix) occupied.push({ x, y });
    }
  }

  let from: { x: number; y: number } | undefined;
  let to: { x: number; y: number } | undefined;

  if (vacated.length === 1 && occupied.length === 1) {
    [from] = vacated;
    [to] = occupied;
  } else if (vacated.length === 2 && occupied.length === 2) {
    // Castling: king and rook both vacate/land; the king square is the SAN move.
    from = vacated.find((v) => before[v.x][v.y]?.[1] === 'K');
    to = occupied.find((o) => after[o.x][o.y]?.[1] === 'K');
  }

  if (!from || !to) {
    return null;
  }

  const fromType = before[from.x][from.y]?.[1];
  const toType = after[to.x][to.y]?.[1];
  const promotion = toType && toType !== fromType ? toType : undefined;

  return { fromX: from.x, fromY: from.y, toX: to.x, toY: to.y, promotion };
}
