import { describe, expect, it } from 'vitest';

import { Board, applyBoardMove, moveToAlgebraicNotation } from './algebraic-notation';

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

function move(board: Board, fromX: number, fromY: number, toX: number, toY: number, promotion?: string): Board {
  const next = board.map((col) => [...col]);
  next[toX][toY] = board[fromX][fromY];
  next[fromX][fromY] = null;
  if (promotion) next[toX][toY] = `${board[fromX][fromY]![0]}${promotion}`;
  return next;
}

describe('moveToAlgebraicNotation', () => {
  it('formats a plain pawn push', () => {
    const before = withPieces({ e2: 'wP' });
    const after = move(before, 4, 1, 4, 3);
    expect(moveToAlgebraicNotation(before, { fromX: 4, fromY: 1, toX: 4, toY: 3 }, after, 'Ongoing')).toBe('e4');
  });

  it('formats a pawn capture', () => {
    const before = withPieces({ e4: 'wP', d5: 'bP' });
    const after = move(before, 4, 3, 3, 4);
    expect(moveToAlgebraicNotation(before, { fromX: 4, fromY: 3, toX: 3, toY: 4 }, after, 'Ongoing')).toBe('exd5');
  });

  it('formats a knight move', () => {
    const before = withPieces({ g1: 'wN' });
    const after = move(before, 6, 0, 5, 2);
    expect(moveToAlgebraicNotation(before, { fromX: 6, fromY: 0, toX: 5, toY: 2 }, after, 'Ongoing')).toBe('Nf3');
  });

  it('disambiguates two knights by file', () => {
    const before = withPieces({ b1: 'wN', d1: 'wN' });
    const after = move(before, 1, 0, 2, 2);
    expect(moveToAlgebraicNotation(before, { fromX: 1, fromY: 0, toX: 2, toY: 2 }, after, 'Ongoing')).toBe('Nbc3');
  });

  it('disambiguates two rooks on the same file by rank', () => {
    const before = withPieces({ a1: 'wR', a8: 'wR', h1: 'bK' });
    const after = move(before, 0, 0, 0, 3);
    expect(moveToAlgebraicNotation(before, { fromX: 0, fromY: 0, toX: 0, toY: 3 }, after, 'Ongoing')).toBe('R1a4');
  });

  it('formats kingside castling', () => {
    const before = withPieces({ e1: 'wK', h1: 'wR' });
    const after = move(before, 4, 0, 6, 0);
    after[5][0] = 'wR';
    after[7][0] = null;
    expect(moveToAlgebraicNotation(before, { fromX: 4, fromY: 0, toX: 6, toY: 0 }, after, 'Ongoing')).toBe('O-O');
  });

  it('formats promotion', () => {
    const before = withPieces({ e7: 'wP' });
    const after = move(before, 4, 6, 4, 7, 'Q');
    expect(
      moveToAlgebraicNotation(before, { fromX: 4, fromY: 6, toX: 4, toY: 7, promotion: 'Q' }, after, 'Ongoing')
    ).toBe('e8=Q');
  });

  it('appends + for a move that gives check', () => {
    const before = withPieces({ d1: 'wQ', e8: 'bK' });
    const after = move(before, 3, 0, 3, 7);
    expect(moveToAlgebraicNotation(before, { fromX: 3, fromY: 0, toX: 3, toY: 7 }, after, 'Ongoing')).toBe('Qd8+');
  });

  it('appends # for a checkmating move', () => {
    const before = withPieces({ d1: 'wQ', e8: 'bK' });
    const after = move(before, 3, 0, 3, 7);
    expect(
      moveToAlgebraicNotation(before, { fromX: 3, fromY: 0, toX: 3, toY: 7 }, after, 'WhiteWonByCheckmate')
    ).toBe('Qd8#');
  });
});

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

  it('round-trips through moveToAlgebraicNotation for a short replayed game', () => {
    let board = withPieces({
      e2: 'wP',
      e7: 'bP',
      g1: 'wN',
      b8: 'bN',
    });
    const moves = [
      { fromX: 4, fromY: 1, toX: 4, toY: 3 }, // e4
      { fromX: 4, fromY: 6, toX: 4, toY: 4 }, // e5
      { fromX: 6, fromY: 0, toX: 5, toY: 2 }, // Nf3
    ];
    const sans = moves.map((move) => {
      const next = applyBoardMove(board, move);
      const san = moveToAlgebraicNotation(board, move, next, 'Ongoing');
      board = next;
      return san;
    });
    expect(sans).toEqual(['e4', 'e5', 'Nf3']);
  });
});
