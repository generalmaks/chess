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

    public void MakeMove(PieceMove pieceMove, Team team)
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

        var movingPiece = startSpot.Piece;
        if (movingPiece is Pawn pawn)
            pawn.HasMadeFirstMove = true;

        MoveHistory.Add(new MoveRecord(movingPiece.PieceCode, pieceMove));
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