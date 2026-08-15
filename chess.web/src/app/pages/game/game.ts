import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';

import { Game as GameService } from '../../core/game';
import { Team } from '../../core/models/game.models';

interface Square {
  x: number;
  y: number;
  piece: string | null;
}

const RESULT_LABELS: Record<string, string> = {
  Ongoing: '',
  WhiteWon: 'White wins',
  BlackWon: 'Black wins',
  StalemateDraw: 'Draw by stalemate',
  FiftyMoveRuleDraw: 'Draw by fifty-move rule',
  DrawByAgreement: 'Draw by agreement',
};


@Component({
  selector: 'app-game',
  imports: [RouterLink],
  templateUrl: './game.html',
  styleUrl: './game.scss',
})
export class Game implements OnInit, OnDestroy {
  protected readonly game = inject(GameService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  protected readonly joinError = signal<string | null>(null);
  protected readonly selected = signal<{ x: number; y: number } | null>(null);

  protected readonly resultLabel = computed(() => RESULT_LABELS[this.game.result()] ?? this.game.result());

  protected readonly rows = computed<Square[][]>(() => {
    const board = this.game.board();
    if (!board) {
      return [];
    }

    const flip = this.game.myTeam() === 'Black';
    const ranks = flip ? range(0, 8) : range(0, 8).reverse();
    const files = flip ? range(0, 8).reverse() : range(0, 8);

    return ranks.map((y) => files.map((x) => ({ x, y, piece: board[x]?.[y] ?? null })));
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
      await this.router.navigate(['/play']);
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

  protected pieceImage(piece: string | null): string | null {
    return piece ? `/pieces/${piece}.svg` : null;
  }

  protected isSelected(x: number, y: number): boolean {
    const sel = this.selected();
    return sel !== null && sel.x === x && sel.y === y;
  }

  protected async onSquareClick(square: Square): Promise<void> {
    if (!this.game.isMyTurn()) {
      return;
    }

    const sel = this.selected();

    if (sel) {
      this.selected.set(null);
      if (sel.x === square.x && sel.y === square.y) {
        return;
      }
      await this.game.makeMove(sel.x, sel.y, square.x, square.y);
      return;
    }

    if (square.piece && ownsSquare(square.piece, this.game.myTeam())) {
      this.selected.set({ x: square.x, y: square.y });
    }
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
}

function range(start: number, end: number): number[] {
  return Array.from({ length: end - start }, (_, i) => start + i);
}

function ownsSquare(piece: string, team: Team | null): boolean {
  if (!team) {
    return false;
  }
  return piece.startsWith(team === 'White' ? 'w' : 'b');
}
