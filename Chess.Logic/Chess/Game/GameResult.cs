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
    WhiteWonOnTime,
    BlackWonOnTime,
    StalemateDraw,
    FiftyMoveRuleDraw,
    DrawByAgreement
}
