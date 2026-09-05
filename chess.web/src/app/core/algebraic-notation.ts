export type Board = (string | null)[][];

export interface BoardMove {
  fromX: number;
  fromY: number;
  toX: number;
  toY: number;
  promotion?: string | null;
}

const FILES = 'abcdefgh';

export function squareName(x: number, y: number): string {
  return `${FILES[x]}${y + 1}`;
}

// Mechanically applies a known-good move (no legality checks) to produce the next
// board. Used to replay a persisted move list into board snapshots after a reload.
export function applyBoardMove(board: Board, move: BoardMove): Board {
  const piece = board[move.fromX]?.[move.fromY];
  if (!piece) {
    throw new Error(`No piece at ${squareName(move.fromX, move.fromY)}`);
  }

  const team = piece[0];
  const type = piece[1];
  const next = board.map((column) => [...column]);
  next[move.fromX][move.fromY] = null;

  if (type === 'K' && Math.abs(move.toX - move.fromX) === 2) {
    const step = move.toX > move.fromX ? 1 : -1;
    const rookFromX = step === 1 ? 7 : 0;
    const rookToX = move.fromX + step;
    next[rookToX][move.fromY] = next[rookFromX][move.fromY];
    next[rookFromX][move.fromY] = null;
  }

  if (type === 'P' && move.fromX !== move.toX && board[move.toX][move.toY] === null) {
    next[move.toX][move.fromY] = null;
  }

  next[move.toX][move.toY] = move.promotion ? `${team}${move.promotion.toUpperCase()}` : piece;
  return next;
}
