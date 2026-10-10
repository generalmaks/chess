namespace Chess.Model.Chess.Game;

public enum GameResult
{
    Ongoing,
    WhiteWonByCheckmate,
    BlackWonByCheckmate,
    WhiteWonByResignation,
    BlackWonByResignation,
    WhiteWonByAbandonment,
    BlackWonByAbandonment,
    WhiteWonOnTime,
    BlackWonOnTime,
    StalemateDraw,
    FiftyMoveRuleDraw,
    DrawByAgreement,
    InsufficientMaterialDraw,
    ThreefoldRepetitionDraw,
    TimeoutInsufficientMaterialDraw
}
