using Chess.Logic.Pieces;
using Chess.Logic.Pieces.Chess;

namespace Chess.Logic.Board;

public class ChessBoard
{
    public Spot[][] Spots { get; }
    private List<PieceMove> MoveHistory { get; } = [];

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
    }

    public void MakeMove(PieceMove pieceMove, Team team)
    {
        var startSpot = GetSpot(pieceMove.From);
        var endSpot = GetSpot(pieceMove.To);
        if (startSpot.Piece is null)
            throw new ArgumentException("Can't move non-existent piece");
        if (startSpot.Piece.Team != team)
            throw new ArgumentException("Can't move enemy piece");
        if (endSpot.Piece != null && endSpot.Piece.Team == team)
            throw new ArgumentException("Can't move piece over your piece");
        if (!startSpot.Piece.PossibleMoves(this, pieceMove.From).Contains(pieceMove))
            throw new ArgumentException("Move is not legal for this piece");

        var movingPiece = startSpot.Piece;
        if (movingPiece is Pawn pawn)
            pawn.HasMadeFirstMove = true;

        MoveHistory.Add(pieceMove);
        endSpot.SetPiece(movingPiece);
        startSpot.SetPiece(null);
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
}