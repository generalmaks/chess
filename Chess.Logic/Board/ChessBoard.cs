using Chess.Logic.Pieces;
using Chess.Logic.Pieces.Chess;

namespace Chess.Logic.Board;

public class ChessBoard
{
    public Spot[][] Spots { get; }
    public List<MoveRecord> MoveHistory { get; } = [];

    public ChessBoard()
    {
        Spots = new Spot[8][];
        for (int x = 0; x < 8; x++)
        {
            Spots[x] = new Spot[8];
            for (int y = 0; y < 8; y++)
            {
                Spots[x][y] = new Spot(new PieceCord(x, y));
            }
        }

        PlacePawns(1, Team.White);
        PlacePawns(6, Team.Black);
        
        PlaceKnights(0, Team.White);
        PlaceKnights(7, Team.Black);

        PlaceBishops(0, Team.White);
        PlaceBishops(7, Team.Black);

        PlaceRooks(0, Team.White);
        PlaceRooks(7, Team.Black);

        PlaceQueen(0, Team.White);
        PlaceQueen(7, Team.Black);

        PlaceKing(0, Team.White);
        PlaceKing(7, Team.Black);
    }

    private static readonly char[] PromotionPieces = ['Q', 'R', 'B', 'N'];

    public void MakeMove(PieceMove pieceMove, Team team, char? promotionPieceCode = null)
    {
        var startSpot = GetSpot(pieceMove.From);
        var endSpot = GetSpot(pieceMove.To);
        if (startSpot.Piece is null)
            throw new NonExistingPieceException();
        if (startSpot.Piece.Team != team)
            throw new EnemyPieceMovingException();
        if (endSpot.Piece != null && endSpot.Piece.Team == team)
            throw new MovingOverOwnPiecesException();
        if (!startSpot.Piece.PossibleMoves(this, pieceMove.From).Contains(pieceMove))
            throw new NoPossibleMovesException();
        if (MoveLeavesKingInCheck(pieceMove, team))
            throw new MoveLeavesKingInCheckException();

        var movingPiece = startSpot.Piece;
        switch (movingPiece)
        {
            case Pawn pawn when pieceMove.To.Y is 0 or 7:
                var promoteTo = promotionPieceCode ?? 'Q';
                if (!PromotionPieces.Contains(promoteTo))
                    throw new InvalidPromotionPieceException();

                movingPiece = PieceFactory.CreatePiece(promoteTo, team);
                MoveHistory.Add(new MoveRecord(pawn.PieceCode, pieceMove, false, promoteTo));
                break;
            case Pawn pawn:
                pawn.HasMadeFirstMove = true;
                bool isEnPassant = pieceMove.To.X != pieceMove.From.X && endSpot.Piece is null;
                if (isEnPassant)
                    GetSpot(new PieceCord(pieceMove.To.X, pieceMove.From.Y)).SetPiece(null);
                MoveHistory.Add(new MoveRecord(movingPiece.PieceCode, pieceMove, false, IsEnPassant: isEnPassant));
                break;
            case King king:
                king.HasMoved = true;
                if (Math.Abs(pieceMove.To.X - pieceMove.From.X) == 2)
                {
                    CastleRook(pieceMove);
                    MoveHistory.Add(new MoveRecord(movingPiece.PieceCode, pieceMove, true));
                }
                else
                {
                    MoveHistory.Add(new MoveRecord(movingPiece.PieceCode, pieceMove, false));
                }
                break;
            case Rook rook:
                rook.HasMoved = true;
                MoveHistory.Add(new MoveRecord(movingPiece.PieceCode, pieceMove, false));
                break;
            default:
                MoveHistory.Add(new MoveRecord(movingPiece.PieceCode, pieceMove, false));
                break;
        }
        
        endSpot.SetPiece(movingPiece);
        startSpot.SetPiece(null);
    }

    public bool IsSquareAttacked(PieceCord coord, Team byTeam)
    {
        foreach (var column in Spots)
        {
            foreach (var spot in column)
            {
                if (spot.Piece is { } piece && piece.Team == byTeam &&
                    piece.GetAttackedSquares(this, spot.Coord).Contains(coord))
                    return true;
            }
        }

        return false;
    }

    public bool IsInCheck(Team team)
    {
        if (FindKing(team) is not { } kingCoord)
            return false;

        var opponent = team == Team.White ? Team.Black : Team.White;
        return IsSquareAttacked(kingCoord, opponent);
    }

    public bool IsCheckmate(Team team) => IsInCheck(team) && !HasAnyLegalMoves(team);

    public bool IsStalemate(Team team) => !IsInCheck(team) && !HasAnyLegalMoves(team);

    private bool HasAnyLegalMoves(Team team)
    {
        foreach (var column in Spots)
        {
            foreach (var spot in column)
            {
                if (spot.Piece is { } piece && piece.Team == team && GetLegalMoves(spot.Coord).Length > 0)
                    return true;
            }
        }

        return false;
    }

    // Pseudo-legal moves filtered down to ones that don't leave the mover's own
    // king in check. This is also what stops a player from making any move at all
    // while already in check unless the move resolves it.
    public PieceMove[] GetLegalMoves(PieceCord from)
    {
        if (GetSpot(from).Piece is not { } piece)
            return [];

        return piece.PossibleMoves(this, from)
            .Where(move => !MoveLeavesKingInCheck(move, piece.Team))
            .ToArray();
    }

    private PieceCord? FindKing(Team team)
    {
        foreach (var column in Spots)
        {
            foreach (var spot in column)
            {
                if (spot.Piece is King && spot.Piece.Team == team)
                    return spot.Coord;
            }
        }

        return null;
    }

    // Occupancy is all that matters for a self-check simulation (a piece never
    // threatens its own king), so this moves the actual piece rather than
    // recreating promotion/capture logic, then restores every square it touched.
    private bool MoveLeavesKingInCheck(PieceMove move, Team team)
    {
        var fromSpot = GetSpot(move.From);
        var toSpot = GetSpot(move.To);
        var movingPiece = fromSpot.Piece!;
        var capturedPiece = toSpot.Piece;

        Spot? enPassantSpot = null;
        Piece? enPassantPiece = null;
        if (movingPiece is Pawn && move.To.X != move.From.X && toSpot.Piece is null)
        {
            enPassantSpot = GetSpot(new PieceCord(move.To.X, move.From.Y));
            enPassantPiece = enPassantSpot.Piece;
            enPassantSpot.SetPiece(null);
        }

        Spot? rookFromSpot = null;
        Spot? rookToSpot = null;
        Piece? rookPiece = null;
        if (movingPiece is King && Math.Abs(move.To.X - move.From.X) == 2)
        {
            int step = move.To.X > move.From.X ? 1 : -1;
            int rookFromX = step == 1 ? Spots.Length - 1 : 0;
            rookFromSpot = GetSpot(new PieceCord(rookFromX, move.From.Y));
            rookToSpot = GetSpot(new PieceCord(move.From.X + step, move.From.Y));
            rookPiece = rookFromSpot.Piece;
            rookToSpot.SetPiece(rookPiece);
            rookFromSpot.SetPiece(null);
        }

        toSpot.SetPiece(movingPiece);
        fromSpot.SetPiece(null);

        var inCheck = IsInCheck(team);

        fromSpot.SetPiece(movingPiece);
        toSpot.SetPiece(capturedPiece);
        enPassantSpot?.SetPiece(enPassantPiece);
        if (rookFromSpot != null)
        {
            rookFromSpot.SetPiece(rookPiece);
            rookToSpot!.SetPiece(null);
        }

        return inCheck;
    }

    private void CastleRook(PieceMove kingMove)
    {
        int step = kingMove.To.X > kingMove.From.X ? 1 : -1;
        int rookFromX = step == 1 ? Spots.Length - 1 : 0;
        var rookFrom = new PieceCord(rookFromX, kingMove.From.Y);
        var rookTo = new PieceCord(kingMove.From.X + step, kingMove.From.Y);

        var rookSpot = GetSpot(rookFrom);
        var rook = rookSpot.Piece;
        if (rook is Rook r)
            r.HasMoved = true;

        GetSpot(rookTo).SetPiece(rook);
        rookSpot.SetPiece(null);
    }

    private Spot GetSpot(PieceCord coord) => Spots[coord.X][coord.Y];

    private void PlacePawns(int y, Team team)
    {
        for (int x = 0; x < 8; x++)
        {
            GetSpot(new PieceCord(x, y)).SetPiece(PieceFactory.CreatePiece('P', team));
        }
    }

    private void PlaceKnights(int y, Team team)
    {
        GetSpot(new PieceCord(1, y)).SetPiece(PieceFactory.CreatePiece('N', team));
        GetSpot(new PieceCord(6, y)).SetPiece(PieceFactory.CreatePiece('N', team));
    }

    private void PlaceBishops(int y, Team team)
    {
        GetSpot(new PieceCord(2, y)).SetPiece(PieceFactory.CreatePiece('B', team));
        GetSpot(new PieceCord(5, y)).SetPiece(PieceFactory.CreatePiece('B', team));
    }

    private void PlaceRooks(int y, Team team)
    {
        GetSpot(new PieceCord(0, y)).SetPiece(PieceFactory.CreatePiece('R', team));
        GetSpot(new PieceCord(7, y)).SetPiece(PieceFactory.CreatePiece('R', team));
    }

    private void PlaceQueen(int y, Team team)
    {
        GetSpot(new PieceCord(3, y)).SetPiece(PieceFactory.CreatePiece('Q', team));
    }

    private void PlaceKing(int y, Team team)
    {
        GetSpot(new PieceCord(4, y)).SetPiece(PieceFactory.CreatePiece('K', team));
    }
}