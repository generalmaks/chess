using Chess.Logic.Pieces.Chess;

namespace Chess.Logic.Board;

public class Spot
{
    public Piece? Piece { get; private set; }

    public PieceCord Coord { get; }
    public bool IsSpotOccupied => Piece != null;

    public Spot(PieceCord coord)
    {
        Coord = coord;
    }

    public void SetPiece(Piece? piece)
    {
        Piece = piece;
    }
}