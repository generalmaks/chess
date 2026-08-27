import { Board } from './algebraic-notation';

export interface PieceView {
  id: number;
  x: number;
  y: number;
  piece: string;
}

let nextId = 0;

// Matches pieces on `next` to their counterparts in `previous` (same piece code,
// nearest square) so a piece that moved keeps its id — and therefore its DOM node —
// across renders, letting a CSS transition slide it to its new square instead of it
// popping there. Unmatched next-squares are new pieces (initial setup, promotions);
// unmatched previous pieces were captured and simply disappear.
export function diffPieceViews(previous: PieceView[], next: Board): PieceView[] {
  const nextSquares: { x: number; y: number; piece: string }[] = [];
  for (let x = 0; x < 8; x++) {
    for (let y = 0; y < 8; y++) {
      const piece = next[x]?.[y];
      if (piece) nextSquares.push({ x, y, piece });
    }
  }

  const usedPrev = new Set<number>();
  const usedNext = new Set<number>();
  const result: PieceView[] = [];

  // Pass 1: pieces that stayed exactly where they were.
  previous.forEach((view, pi) => {
    const ni = nextSquares.findIndex(
      (s, i) => !usedNext.has(i) && s.x === view.x && s.y === view.y && s.piece === view.piece
    );
    if (ni !== -1) {
      usedPrev.add(pi);
      usedNext.add(ni);
      result.push(view);
    }
  });

  // Pass 2: match what's left by piece code, nearest square first, so a moved piece
  // keeps its identity rather than being treated as captured-and-replaced.
  const pairs: { pi: number; ni: number; dist: number }[] = [];
  previous.forEach((view, pi) => {
    if (usedPrev.has(pi)) return;
    nextSquares.forEach((s, ni) => {
      if (usedNext.has(ni) || s.piece !== view.piece) return;
      pairs.push({ pi, ni, dist: Math.abs(s.x - view.x) + Math.abs(s.y - view.y) });
    });
  });
  pairs.sort((a, b) => a.dist - b.dist);
  for (const { pi, ni } of pairs) {
    if (usedPrev.has(pi) || usedNext.has(ni)) continue;
    usedPrev.add(pi);
    usedNext.add(ni);
    const view = previous[pi];
    const s = nextSquares[ni];
    result.push({ id: view.id, x: s.x, y: s.y, piece: s.piece });
  }

  // Pass 3: anything left in `next` has no prior counterpart — initial board, or a
  // pawn promoting into a piece type that wasn't on the board before.
  nextSquares.forEach((s, ni) => {
    if (usedNext.has(ni)) return;
    result.push({ id: nextId++, x: s.x, y: s.y, piece: s.piece });
  });

  return result;
}
