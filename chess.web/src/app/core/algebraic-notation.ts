import { GameResult } from './models/game.models';

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

// Converts one backend move into Standard Algebraic Notation (e.g. "Nf3", "exd5", "O-O", "e8=Q+").
export function moveToAlgebraicNotation(
  boardBefore: Board,
  move: BoardMove,
  boardAfter: Board,
  resultAfter: GameResult
): string {
  const piece = boardBefore[move.fromX]?.[move.fromY];
  if (!piece) {
    throw new Error(`No piece at ${squareName(move.fromX, move.fromY)}`);
  }

  const team = piece[0] as 'w' | 'b';
  const type = piece[1];
  const opponent: 'w' | 'b' = team === 'w' ? 'b' : 'w';

  if (type === 'K' && Math.abs(move.toX - move.fromX) === 2) {
    const castle = move.toX > move.fromX ? 'O-O' : 'O-O-O';
    return withCheckSuffix(castle, opponent, boardAfter, resultAfter);
  }

  const targetBefore = boardBefore[move.toX]?.[move.toY];
  const isEnPassant = type === 'P' && move.fromX !== move.toX && targetBefore === null;
  const isCapture = targetBefore !== null || isEnPassant;

  let san: string;
  if (type === 'P') {
    san = isCapture ? `${FILES[move.fromX]}x${squareName(move.toX, move.toY)}` : squareName(move.toX, move.toY);
    if (move.promotion) {
      san += `=${move.promotion.toUpperCase()}`;
    }
  } else {
    san = type + disambiguation(boardBefore, move, piece) + (isCapture ? 'x' : '') + squareName(move.toX, move.toY);
  }

  return withCheckSuffix(san, opponent, boardAfter, resultAfter);
}

function withCheckSuffix(san: string, opponent: 'w' | 'b', boardAfter: Board, resultAfter: GameResult): string {
  if (resultAfter === 'WhiteWonByCheckmate' || resultAfter === 'BlackWonByCheckmate') {
    return `${san}#`;
  }
  return isKingInCheck(boardAfter, opponent) ? `${san}+` : san;
}

// Disambiguates by file, then rank, then full square, per SAN rules. Ignores pins.
function disambiguation(board: Board, move: BoardMove, piece: string): string {
  const type = piece[1];
  if (type === 'P' || type === 'K') return '';

  const candidates: { x: number; y: number }[] = [];
  for (let x = 0; x < 8; x++) {
    for (let y = 0; y < 8; y++) {
      if (x === move.fromX && y === move.fromY) continue;
      if (board[x][y] !== piece) continue;
      if (attackedSquares(board, x, y).some((s) => s.x === move.toX && s.y === move.toY)) {
        candidates.push({ x, y });
      }
    }
  }

  if (candidates.length === 0) return '';

  const sameFile = candidates.some((c) => c.x === move.fromX);
  const sameRank = candidates.some((c) => c.y === move.fromY);

  if (!sameFile) return FILES[move.fromX];
  if (!sameRank) return String(move.fromY + 1);
  return squareName(move.fromX, move.fromY);
}

function isKingInCheck(board: Board, kingTeam: 'w' | 'b'): boolean {
  let kingX = -1;
  let kingY = -1;
  for (let x = 0; x < 8; x++) {
    for (let y = 0; y < 8; y++) {
      const code = board[x][y];
      if (code === `${kingTeam}K`) {
        kingX = x;
        kingY = y;
      }
    }
  }
  if (kingX === -1) return false;

  for (let x = 0; x < 8; x++) {
    for (let y = 0; y < 8; y++) {
      const code = board[x][y];
      if (code && code[0] !== kingTeam && attackedSquares(board, x, y).some((s) => s.x === kingX && s.y === kingY)) {
        return true;
      }
    }
  }
  return false;
}

const KNIGHT_OFFSETS = [
  [1, 2],
  [2, 1],
  [2, -1],
  [1, -2],
  [-1, -2],
  [-2, -1],
  [-2, 1],
  [-1, 2],
];
const KING_OFFSETS = [
  [1, 0],
  [1, 1],
  [0, 1],
  [-1, 1],
  [-1, 0],
  [-1, -1],
  [0, -1],
  [1, -1],
];
const BISHOP_DIRS = [
  [1, 1],
  [1, -1],
  [-1, 1],
  [-1, -1],
];
const ROOK_DIRS = [
  [1, 0],
  [-1, 0],
  [0, 1],
  [0, -1],
];

function inBounds(x: number, y: number): boolean {
  return x >= 0 && x < 8 && y >= 0 && y < 8;
}

// Squares this piece threatens; pawns attack diagonally, not the square they move to.
function attackedSquares(board: Board, x: number, y: number): { x: number; y: number }[] {
  const code = board[x][y];
  if (!code) return [];
  const team = code[0];
  const type = code[1];
  const squares: { x: number; y: number }[] = [];

  switch (type) {
    case 'P': {
      const dir = team === 'w' ? 1 : -1;
      for (const dx of [-1, 1]) {
        const nx = x + dx;
        const ny = y + dir;
        if (inBounds(nx, ny)) squares.push({ x: nx, y: ny });
      }
      break;
    }
    case 'N':
      for (const [dx, dy] of KNIGHT_OFFSETS) {
        const nx = x + dx;
        const ny = y + dy;
        if (inBounds(nx, ny)) squares.push({ x: nx, y: ny });
      }
      break;
    case 'K':
      for (const [dx, dy] of KING_OFFSETS) {
        const nx = x + dx;
        const ny = y + dy;
        if (inBounds(nx, ny)) squares.push({ x: nx, y: ny });
      }
      break;
    case 'B':
    case 'R':
    case 'Q': {
      const dirs = type === 'B' ? BISHOP_DIRS : type === 'R' ? ROOK_DIRS : [...BISHOP_DIRS, ...ROOK_DIRS];
      for (const [dx, dy] of dirs) {
        let nx = x + dx;
        let ny = y + dy;
        while (inBounds(nx, ny)) {
          squares.push({ x: nx, y: ny });
          if (board[nx][ny]) break;
          nx += dx;
          ny += dy;
        }
      }
      break;
    }
  }

  return squares;
}
