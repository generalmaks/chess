import { Component, OnDestroy, OnInit, computed, effect, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';

import { Game as GameService } from '../../core/game';
import { Team } from '../../core/models/game.models';
import { diffPieceViews, PieceView } from '../../core/piece-diff';
import { PieceSprite } from './piece-sprite';

interface Square {
  x: number;
  y: number;
  piece: string | null;
}

interface MovePair {
  number: number;
  white: string;
  whitePly: number;
  black: string | null;
  blackPly: number | null;
}

const RESULT_LABELS: Record<string, string> = {
  Ongoing: '',
  WhiteWonByCheckmate: 'White wins by checkmate',
  BlackWonByCheckmate: 'Black wins by checkmate',
  WhiteWonByResignation: 'White wins by resignation',
  BlackWonByResignation: 'Black wins by resignation',
  WhiteWonByAbandonment: 'White wins, Black abandoned',
  BlackWonByAbandonment: 'Black wins, White abandoned',
  WhiteWonOnTime: 'White wins on time',
  BlackWonOnTime: 'Black wins on time',
  StalemateDraw: 'Draw by stalemate',
  FiftyMoveRuleDraw: 'Draw by fifty-move rule',
  DrawByAgreement: 'Draw by agreement',
};


@Component({
  selector: 'app-game',
  imports: [RouterLink, MatButtonModule, MatCardModule, PieceSprite],
  templateUrl: './game.html',
  styleUrl: './game.scss',
})
export class Game implements OnInit, OnDestroy {
  protected readonly game = inject(GameService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly joinError = signal<string | null>(null);
  protected readonly selected = signal<{ x: number; y: number } | null>(null);
  protected readonly copied = signal(false);
  // null means "follow the live position"; a number pins the board to that ply.
  protected readonly viewPly = signal<number | null>(null);

  protected readonly isViewingHistory = computed(
    () => this.viewPly() !== null && this.viewPly() !== this.game.moveHistory().length
  );

  protected readonly displayBoard = computed<(string | null)[][] | null>(() => {
    const ply = this.viewPly();
    return ply === null ? this.game.board() : (this.game.positions()[ply] ?? this.game.board());
  });

  private readonly legalTargetsSignal = signal<{ x: number; y: number }[]>([]);
  protected readonly legalTargets = this.legalTargetsSignal.asReadonly();

  // Bookkeeping for animating piece moves; not a signal itself, only pieceViewsSignal
  // (the rendered output) needs to be reactive. See diffPieceViews() for the matching.
  private previousPieceViews: PieceView[] = [];
  private readonly pieceViewsSignal = signal<PieceView[]>([]);
  protected readonly pieceViews = this.pieceViewsSignal.asReadonly();

  constructor() {
    // A pinned view only makes sense until the opponent moves — once they do, the
    // position you were looking at is no longer "current" in any useful sense.
    effect(() => {
      const lastPly = this.game.moveHistory().length;
      const myTeam = this.game.myTeam();
      if (lastPly === 0 || !myTeam) {
        return;
      }

      const moverTeam: Team = lastPly % 2 === 1 ? 'White' : 'Black';
      if (moverTeam !== myTeam) {
        this.viewPly.set(null);
      }
    });

    // Fetches the selected piece's legal moves from the backend, which accounts for
    // checks/pins/castling/en passant rather than reimplementing chess rules here.
    effect(() => {
      const sel = this.selected();
      if (!sel) {
        this.legalTargetsSignal.set([]);
        return;
      }

      this.game.getLegalMoves(sel.x, sel.y).then((targets) => {
        // Drop stale responses if the selection changed while this was in flight.
        const current = this.selected();
        if (current && current.x === sel.x && current.y === sel.y) {
          this.legalTargetsSignal.set(targets);
        }
      });
    });

    // Re-matches pieces to their new squares (by identity, not by square) whenever the
    // displayed position changes — from a move arriving or from browsing move history —
    // so the template can key on pv.id and each <app-piece-sprite> instance persists
    // across the update, letting it play its own move animation (see piece-sprite.ts).
    effect(() => {
      const board = this.displayBoard();
      const next = board ? diffPieceViews(this.previousPieceViews, board) : [];
      this.previousPieceViews = next;
      this.pieceViewsSignal.set(next);
    });
  }

  protected readonly resultLabel = computed(() => RESULT_LABELS[this.game.result()] ?? this.game.result());

  protected readonly myTimeRemaining = computed(() => this.timeRemainingFor(this.game.myTeam()));
  protected readonly opponentTimeRemaining = computed(() => this.timeRemainingFor(this.opponentTeam()));

  protected readonly myTimeLabel = computed(() => formatClock(this.myTimeRemaining()));
  protected readonly opponentTimeLabel = computed(() => formatClock(this.opponentTimeRemaining()));

  protected readonly isMyClockLow = computed(() => isLow(this.myTimeRemaining()));
  protected readonly isOpponentClockLow = computed(() => isLow(this.opponentTimeRemaining()));

  protected readonly rows = computed<Square[][]>(() => {
    const board = this.displayBoard();
    if (!board) {
      return [];
    }

    const flip = this.game.myTeam() === 'Black';
    const ranks = flip ? range(0, 8) : range(0, 8).reverse();
    const files = flip ? range(0, 8).reverse() : range(0, 8);

    return ranks.map((y) => files.map((x) => ({ x, y, piece: board[x]?.[y] ?? null })));
  });

  protected readonly movePairs = computed<MovePair[]>(() => {
    const moves = this.game.moveHistory();
    const pairs: MovePair[] = [];
    for (let i = 0; i < moves.length; i += 2) {
      pairs.push({
        number: i / 2 + 1,
        white: moves[i],
        whitePly: i + 1,
        black: moves[i + 1] ?? null,
        blackPly: moves[i + 1] !== undefined ? i + 2 : null,
      });
    }
    return pairs;
  });

  protected readonly rankLabels = computed<number[]>(() => {
    const flip = this.game.myTeam() === 'Black';
    const ranks = flip ? range(0, 8) : range(0, 8).reverse();
    return ranks.map((y) => y + 1);
  });

  protected readonly fileLabels = computed<string[]>(() => {
    const flip = this.game.myTeam() === 'Black';
    const files = flip ? range(0, 8).reverse() : range(0, 8);
    return files.map((x) => String.fromCharCode(65 + x));
  });

  async ngOnInit(): Promise<void> {
    const gameId = this.route.snapshot.paramMap.get('gameId');
    if (!gameId) {
      await this.router.navigate(['/']);
      return;
    }

    try {
      await this.game.joinGame(gameId);
    } catch {
      this.joinError.set('Could not join that game. Check the game ID and try again.');
    }
  }

  async ngOnDestroy(): Promise<void> {
    await this.game.leave();
  }

  protected isSelected(x: number, y: number): boolean {
    const sel = this.selected();
    return sel !== null && sel.x === x && sel.y === y;
  }

  protected isLegalTarget(x: number, y: number): boolean {
    return this.legalTargets().some((s) => s.x === x && s.y === y);
  }

  // Board-coordinate -> on-screen column/row (0-7), matching rows()'s flip logic, then
  // expressed as a translate() so the CSS transition animates a GPU-composited transform
  // instead of layout properties (left/top), which is both faster and more reliable.
  protected pieceTransform(pv: PieceView): string {
    const flip = this.game.myTeam() === 'Black';
    const col = flip ? 7 - pv.x : pv.x;
    const row = flip ? pv.y : 7 - pv.y;
    return `translate(${col * 100}%, ${row * 100}%)`;
  }

  protected viewMove(ply: number): void {
    this.selected.set(null);
    this.viewPly.set(ply === this.game.moveHistory().length ? null : ply);
  }

  protected returnToLive(): void {
    this.selected.set(null);
    this.viewPly.set(null);
  }

  protected async onSquareClick(square: Square): Promise<void> {
    if (!this.game.isMyTurn() || this.isViewingHistory()) {
      return;
    }

    const sel = this.selected();

    if (sel) {
      if (sel.x === square.x && sel.y === square.y) {
        this.selected.set(null);
        return;
      }
      if (square.piece && ownsSquare(square.piece, this.game.myTeam())) {
        this.selected.set({ x: square.x, y: square.y });
        return;
      }
      this.selected.set(null);
      await this.game.makeMove(sel.x, sel.y, square.x, square.y);
      return;
    }

    if (square.piece && ownsSquare(square.piece, this.game.myTeam())) {
      this.selected.set({ x: square.x, y: square.y });
    }
  }

  protected async copyGameId(): Promise<void> {
    const gameId = this.game.gameId();
    if (!gameId) {
      return;
    }

    await navigator.clipboard.writeText(gameId);
    this.copied.set(true);
    setTimeout(() => this.copied.set(false), 1500);
  }

  protected async resign(): Promise<void> {
    await this.game.resign();
  }

  protected async offerDraw(): Promise<void> {
    await this.game.offerDraw();
  }

  protected async respondToDraw(accept: boolean): Promise<void> {
    await this.game.respondToDraw(accept);
  }

  private opponentTeam(): Team | null {
    const team = this.game.myTeam();
    if (!team) {
      return null;
    }
    return team === 'White' ? 'Black' : 'White';
  }

  private timeRemainingFor(team: Team | null): number | null {
    if (!team) {
      return null;
    }
    return team === 'White' ? this.game.whiteTimeRemainingSeconds() : this.game.blackTimeRemainingSeconds();
  }
}

function range(start: number, end: number): number[] {
  return Array.from({ length: end - start }, (_, i) => start + i);
}

function formatClock(seconds: number | null): string | null {
  if (seconds === null) {
    return null;
  }
  const total = Math.max(0, Math.ceil(seconds));
  const minutes = Math.floor(total / 60);
  const secs = total % 60;
  return `${minutes}:${secs.toString().padStart(2, '0')}`;
}

function isLow(seconds: number | null): boolean {
  return seconds !== null && seconds < 30;
}

function ownsSquare(piece: string, team: Team | null): boolean {
  if (!team) {
    return false;
  }
  return piece.startsWith(team === 'White' ? 'w' : 'b');
}
