using Chess.Model.Chess.Board;
using Chess.Model.Chess.Pieces.Chess;

namespace Chess.Model.Chess.Game;

// Immutable snapshot of a match at the moment it was captured - won't reflect later moves.
public sealed record Game(
    string Id,
    GameStateSnapshot State,
    bool BothPlayersJoined,
    Guid? WhitePlayerId,
    Guid? BlackPlayerId,
    Team? DrawOfferedBy,
    IReadOnlyList<MoveRecord> MoveHistory,
    string? WhitePlayerName = null,
    string? BlackPlayerName = null)
{
    public Team? TeamForPlayer(Guid playerId)
    {
        if (playerId == WhitePlayerId) return Team.White;
        if (playerId == BlackPlayerId) return Team.Black;
        return null;
    }
}
