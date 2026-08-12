using Chess.Logic;

namespace Chess.Orchestrator;

public static class EloCalculator
{
    private const int KFactor = 32;

    public static (int WhiteRating, int BlackRating) CalculateNewRatings(int whiteRating, int blackRating, GameResult result)
    {
        var whiteScore = result switch
        {
            GameResult.WhiteWon => 1.0,
            GameResult.BlackWon => 0.0,
            GameResult.StalemateDraw or GameResult.FiftyMoveRuleDraw or GameResult.DrawByAgreement => 0.5,
            _ => throw new ArgumentOutOfRangeException(nameof(result), result, "Cannot rate a game that hasn't ended."),
        };

        var expectedWhite = 1.0 / (1.0 + Math.Pow(10, (blackRating - whiteRating) / 400.0));

        var newWhiteRating = whiteRating + KFactor * (whiteScore - expectedWhite);
        var newBlackRating = blackRating + KFactor * ((1.0 - whiteScore) - (1.0 - expectedWhite));

        return ((int)Math.Round(newWhiteRating), (int)Math.Round(newBlackRating));
    }
}
