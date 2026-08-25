using Chess.Logic.Board;
using Chess.Logic.Pieces.Chess;

namespace Chess.Logic;

// Immutable snapshot of a match at the moment it was captured - won't reflect later moves.
public sealed record Game(
    string Id,
    GameStateSnapshot State,
    bool BothPlayersJoined,
    Guid? WhitePlayerId,
    Guid? BlackPlayerId,
    Team? DrawOfferedBy,
    IReadOnlyList<MoveRecord> MoveHistory)
{
    public Team? TeamForPlayer(Guid playerId)
    {
        if (playerId == WhitePlayerId) return Team.White;
        if (playerId == BlackPlayerId) return Team.Black;
        return null;
    }
}
