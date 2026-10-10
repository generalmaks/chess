using Chess.Model.Chess.Pieces.Chess;
using Chess.Model.Tests.Support;

namespace Chess.Model.Tests.Board;

public class MaterialTests
{
    [Fact]
    public void IsDeadPosition_StartingPosition_ReturnsFalse() =>
        Assert.False(new Chess.Board.ChessBoard().IsDeadPosition());

    [Theory]
    [InlineData('N')]
    [InlineData('B')]
    public void IsDeadPosition_KingAndMinorVersusKing_ReturnsTrue(char minor)
    {
        var board = KingsOnly();
        board.Place(PieceFactory(minor), 3, 3);

        Assert.True(board.IsDeadPosition());
    }

    [Fact]
    public void IsDeadPosition_BishopsOnSameColor_ReturnsTrue()
    {
        var board = KingsOnly();
        board.Place(new Bishop(Team.White), 2, 2);
        board.Place(new Bishop(Team.White), 4, 2);
        board.Place(new Bishop(Team.Black), 3, 5);

        Assert.True(board.IsDeadPosition());
    }

    [Fact]
    public void IsDeadPosition_BishopsOnOppositeColors_ReturnsFalse()
    {
        var board = KingsOnly();
        board.Place(new Bishop(Team.White), 2, 2);
        board.Place(new Bishop(Team.Black), 2, 3);

        Assert.False(board.IsDeadPosition());
    }

    [Fact]
    public void HasMatingMaterial_LoneKnightAgainstBareKing_ReturnsFalse()
    {
        var board = KingsOnly();
        board.Place(new Knight(Team.White), 3, 3);

        Assert.False(board.HasMatingMaterial(Team.White));
    }

    [Fact]
    public void HasMatingMaterial_LoneKnightAgainstPawn_ReturnsTrue()
    {
        var board = KingsOnly();
        board.Place(new Knight(Team.White), 3, 3);
        board.Place(new Pawn(Team.Black), 5, 5);

        Assert.True(board.HasMatingMaterial(Team.White));
    }

    [Fact]
    public void HasMatingMaterial_TwoKnights_ReturnsTrue()
    {
        var board = KingsOnly();
        board.Place(new Knight(Team.White), 3, 3);
        board.Place(new Knight(Team.White), 4, 3);

        Assert.True(board.HasMatingMaterial(Team.White));
    }

    [Fact]
    public void HasMatingMaterial_BareKing_ReturnsFalse()
    {
        var board = KingsOnly();
        board.Place(new Queen(Team.Black), 3, 3);

        Assert.False(board.HasMatingMaterial(Team.White));
    }

    private static Chess.Board.ChessBoard KingsOnly()
    {
        var board = TestBoard.Empty();
        board.Place(new King(Team.White), 0, 0);
        board.Place(new King(Team.Black), 7, 7);
        return board;
    }

    private static Piece PieceFactory(char code) =>
        Chess.Pieces.PieceFactory.CreatePiece(code, Team.White);
}
