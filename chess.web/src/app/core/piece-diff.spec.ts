import { describe, expect, it } from 'vitest';

import { Board } from './algebraic-notation';
import { diffPieceViews, PieceView } from './piece-diff';

function emptyBoard(): Board {
  return Array.from({ length: 8 }, () => Array.from({ length: 8 }, () => null));
}

function withPieces(pieces: Record<string, string>): Board {
  const board = emptyBoard();
  for (const [square, code] of Object.entries(pieces)) {
    const x = square.charCodeAt(0) - 'a'.charCodeAt(0);
    const y = Number(square[1]) - 1;
    board[x][y] = code;
  }
  return board;
}

function find(views: PieceView[], x: number, y: number): PieceView | undefined {
  return views.find((v) => v.x === x && v.y === y);
}

describe('diffPieceViews', () => {
  it('assigns fresh ids to every piece on the first board', () => {
    const views = diffPieceViews([], withPieces({ e2: 'wP', e7: 'bP' }));
    expect(views).toHaveLength(2);
    expect(new Set(views.map((v) => v.id)).size).toBe(2);
  });

  it('keeps the same id when a piece moves', () => {
    const start = diffPieceViews([], withPieces({ e2: 'wP' }));
    const after = diffPieceViews(start, withPieces({ e4: 'wP' }));

    expect(after).toHaveLength(1);
    expect(after[0].id).toBe(start[0].id);
    expect(after[0]).toMatchObject({ x: 4, y: 3, piece: 'wP' });
  });

  it('drops a captured piece and keeps the capturing piece identity', () => {
    const start = diffPieceViews([], withPieces({ e4: 'wP', d5: 'bP' }));
    const after = diffPieceViews(start, withPieces({ d5: 'wP' }));

    const white = start.find((v) => v.piece === 'wP')!;
    expect(after).toHaveLength(1);
    expect(after[0].id).toBe(white.id);
    expect(find(after, 3, 4)?.piece).toBe('wP');
  });

  it('gives a promoted piece a new id instead of reusing the pawn that vanished', () => {
    const start = diffPieceViews([], withPieces({ e7: 'wP' }));
    const after = diffPieceViews(start, withPieces({ e8: 'wQ' }));

    expect(after).toHaveLength(1);
    expect(after[0].id).not.toBe(start[0].id);
    expect(after[0]).toMatchObject({ x: 4, y: 7, piece: 'wQ' });
  });

  it('matches castling as two independent moves (king and rook)', () => {
    const start = diffPieceViews([], withPieces({ e1: 'wK', h1: 'wR' }));
    const after = diffPieceViews(start, withPieces({ g1: 'wK', f1: 'wR' }));

    const king = start.find((v) => v.piece === 'wK')!;
    const rook = start.find((v) => v.piece === 'wR')!;

    expect(find(after, 6, 0)).toMatchObject({ id: king.id, piece: 'wK' });
    expect(find(after, 5, 0)).toMatchObject({ id: rook.id, piece: 'wR' });
  });
});
