namespace Chess.Logic;

public enum GameResult
{
    Ongoing,
    WhiteWonByCheckmate,
    BlackWonByCheckmate,
    WhiteWonByResignation,
    BlackWonByResignation,
    WhiteWonByAbandonment,
    BlackWonByAbandonment,
    StalemateDraw,
    FiftyMoveRuleDraw,
    DrawByAgreement
}
