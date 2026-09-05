import { describe, expect, it } from 'vitest';

import { Board, applyBoardMove } from './algebraic-notation';

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

describe('applyBoardMove', () => {
  it('moves a piece and clears its origin', () => {
    const before = withPieces({ e2: 'wP' });
    const after = applyBoardMove(before, { fromX: 4, fromY: 1, toX: 4, toY: 3 });
    expect(after[4][3]).toBe('wP');
    expect(after[4][1]).toBeNull();
  });

  it('removes the captured piece on a normal capture', () => {
    const before = withPieces({ e4: 'wP', d5: 'bP' });
    const after = applyBoardMove(before, { fromX: 4, fromY: 3, toX: 3, toY: 4 });
    expect(after[3][4]).toBe('wP');
  });

  it('removes the victim pawn on en passant', () => {
    const before = withPieces({ e5: 'wP', d5: 'bP' });
    const after = applyBoardMove(before, { fromX: 4, fromY: 4, toX: 3, toY: 5 });
    expect(after[3][5]).toBe('wP');
    expect(after[3][4]).toBeNull();
  });

  it('moves the rook too on castling', () => {
    const before = withPieces({ e1: 'wK', h1: 'wR' });
    const after = applyBoardMove(before, { fromX: 4, fromY: 0, toX: 6, toY: 0 });
    expect(after[6][0]).toBe('wK');
    expect(after[5][0]).toBe('wR');
    expect(after[7][0]).toBeNull();
  });

  it('places the promoted piece', () => {
    const before = withPieces({ e7: 'wP' });
    const after = applyBoardMove(before, { fromX: 4, fromY: 6, toX: 4, toY: 7, promotion: 'Q' });
    expect(after[4][7]).toBe('wQ');
  });
});
