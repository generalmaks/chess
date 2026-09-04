using Chess.Model.Chess.Pieces.Chess;

namespace Chess.Model.Chess.Board;

public static class SanNotation
{
    public static string ForMove(
        ChessBoard boardAfterMove, PieceMove move, Team team, char pieceCode,
        bool isCapture, bool isCastling, char? promotedTo, bool isCheck, bool isCheckmate)
    {
        string core;
        if (isCastling)
        {
            core = move.To.X > move.From.X ? "O-O" : "O-O-O";
        }
        else if (pieceCode == 'P')
        {
            core = isCapture ? $"{FileChar(move.From.X)}x{Square(move.To)}" : Square(move.To);
            if (promotedTo is { } promoted)
                core += $"={promoted}";
        }
        else
        {
            var disambiguation = pieceCode == 'K' ? string.Empty : Disambiguate(boardAfterMove, move, team, pieceCode);
            core = $"{pieceCode}{disambiguation}{(isCapture ? "x" : string.Empty)}{Square(move.To)}";
        }

        return core + (isCheckmate ? "#" : isCheck ? "+" : string.Empty);
    }

    // Board is the position after the move, so the mover sits on move.To. Put it back on
    // move.From so other same-type pieces report their pre-move reach, then restore.
    private static string Disambiguate(ChessBoard board, PieceMove move, Team team, char pieceCode)
    {
        var fromSpot = board.Spots[move.From.X][move.From.Y];
        var toSpot = board.Spots[move.To.X][move.To.Y];
        var mover = toSpot.Piece!;

        toSpot.SetPiece(null);
        fromSpot.SetPiece(mover);
        try
        {
            var sharesFile = false;
            var sharesRank = false;
            var contested = false;

            foreach (var column in board.Spots)
            {
                foreach (var spot in column)
                {
                    if (spot.Coord == move.From || spot.Piece is not { } piece)
                        continue;
                    if (piece.Team != team || piece.PieceCode != pieceCode)
                        continue;
                    if (piece.PossibleMoves(board, spot.Coord).All(m => m.To != move.To))
                        continue;

                    contested = true;
                    sharesFile |= spot.Coord.X == move.From.X;
                    sharesRank |= spot.Coord.Y == move.From.Y;
                }
            }

            if (!contested)
                return string.Empty;
            if (!sharesFile)
                return FileChar(move.From.X).ToString();
            if (!sharesRank)
                return (move.From.Y + 1).ToString();
            return Square(move.From);
        }
        finally
        {
            fromSpot.SetPiece(null);
            toSpot.SetPiece(mover);
        }
    }

    private static char FileChar(int x) => (char)('a' + x);

    private static string Square(PieceCord coord) => $"{FileChar(coord.X)}{coord.Y + 1}";
}
