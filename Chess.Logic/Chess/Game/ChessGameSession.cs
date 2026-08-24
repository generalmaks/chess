using Chess.Logic.Board;
using Chess.Logic.Pieces.Chess;

namespace Chess.Logic;

public class GameAlreadyEndedException() : Exception("The game has already ended.");

public class ChessGameSession(TimeControl? timeControl = null, TimeProvider? timeProvider = null)
{
    private const int FiftyMoveRuleHalfMoveLimit = 100;

    private readonly TimeProvider _timeProvider = timeProvider ?? TimeProvider.System;
    private TimeSpan? _whiteTimeRemaining = timeControl?.InitialTime;
    private TimeSpan? _blackTimeRemaining = timeControl?.InitialTime;
    private DateTimeOffset _turnStartedAt = (timeProvider ?? TimeProvider.System).GetUtcNow();
    private bool _clockRunning;

    public ChessBoard Board { get; } = new();
    public Team CurrentTurn { get; private set; } = Team.White;
    public GameResult Result { get; private set; } = GameResult.Ongoing;
    public TimeControl? TimeControl { get; } = timeControl;

    // Half-moves since the last pawn move or capture; the 50-move rule triggers at 100 (50 by each side).
    public int HalfmoveClock { get; private set; }

    public IReadOnlyList<Piece> PiecesCapturedByWhite => _piecesCapturedByWhite;
    public IReadOnlyList<Piece> PiecesCapturedByBlack => _piecesCapturedByBlack;

    private readonly List<Piece> _piecesCapturedByWhite = [];
    private readonly List<Piece> _piecesCapturedByBlack = [];

    public TimeSpan? TimeRemaining(Team team)
    {
        if (TimeControl is null)
            return null;

        var stored = (team == Team.White ? _whiteTimeRemaining : _blackTimeRemaining)!.Value;
        if (!_clockRunning || Result != GameResult.Ongoing || team != CurrentTurn)
            return stored;

        var remaining = stored - (_timeProvider.GetUtcNow() - _turnStartedAt);
        return remaining < TimeSpan.Zero ? TimeSpan.Zero : remaining;
    }

    public void StartClock()
    {
        _turnStartedAt = _timeProvider.GetUtcNow();
        _clockRunning = true;
    }

    public bool CheckTimeout()
    {
        if (TimeControl is null || Result != GameResult.Ongoing)
            return false;

        if (TimeRemaining(CurrentTurn) != TimeSpan.Zero)
            return false;

        Result = CurrentTurn == Team.White ? GameResult.BlackWonOnTime : GameResult.WhiteWonOnTime;
        return true;
    }

    public void MakeMove(PieceMove move, char? promotionPieceCode = null)
    {
        if (Result != GameResult.Ongoing)
            throw new GameAlreadyEndedException();

        var movingTeam = CurrentTurn;
        var isPawnMove = Board.Spots[move.From.X][move.From.Y].Piece is Pawn;
        var targetPiece = Board.Spots[move.To.X][move.To.Y].Piece;
        // A possible en passant victim, snapshotted before the board mutates the square.
        // Only used below if Board.MakeMove's own MoveHistory record confirms en passant.
        // Used because en passant is special case where capturing piece doesn't stand on captured square
        var enPassantVictim = Board.Spots[move.To.X][move.From.Y].Piece;

        Board.MakeMove(move, movingTeam, promotionPieceCode);

        var capturedPiece = Board.MoveHistory[^1].IsEnPassant ? enPassantVictim : targetPiece;
        if (capturedPiece is not null)
            RecordCapture(capturedPiece);

        HalfmoveClock = isPawnMove || capturedPiece is not null ? 0 : HalfmoveClock + 1;
        TickClock(movingTeam);
        CurrentTurn = movingTeam == Team.White ? Team.Black : Team.White;

        UpdateResult();
    }

    private void TickClock(Team movingTeam)
    {
        if (TimeControl is not { } control)
            return;

        var spent = _timeProvider.GetUtcNow() - _turnStartedAt;
        var updated = (movingTeam == Team.White ? _whiteTimeRemaining : _blackTimeRemaining)!.Value - spent + control.Increment;
        if (updated < TimeSpan.Zero)
            updated = TimeSpan.Zero;

        if (movingTeam == Team.White) _whiteTimeRemaining = updated;
        else _blackTimeRemaining = updated;

        _turnStartedAt = _timeProvider.GetUtcNow();
    }

    public void Resign(Team resigningTeam)
    {
        if (Result != GameResult.Ongoing)
            throw new GameAlreadyEndedException();

        Result = resigningTeam == Team.White ? GameResult.BlackWonByResignation : GameResult.WhiteWonByResignation;
    }

    public void Abandon(Team abandoningTeam)
    {
        if (Result != GameResult.Ongoing)
            throw new GameAlreadyEndedException();

        Result = abandoningTeam == Team.White ? GameResult.BlackWonByAbandonment : GameResult.WhiteWonByAbandonment;
    }

    public void AgreeToDraw()
    {
        if (Result != GameResult.Ongoing)
            throw new GameAlreadyEndedException();

        Result = GameResult.DrawByAgreement;
    }

    private void RecordCapture(Piece captured)
    {
        var capturedBy = captured.Team == Team.White ? _piecesCapturedByBlack : _piecesCapturedByWhite;
        capturedBy.Add(captured);
    }

    private void UpdateResult()
    {
        if (Board.IsCheckmate(CurrentTurn))
            Result = CurrentTurn == Team.White ? GameResult.BlackWonByCheckmate : GameResult.WhiteWonByCheckmate;
        else if (Board.IsStalemate(CurrentTurn))
            Result = GameResult.StalemateDraw;
        else if (HalfmoveClock >= FiftyMoveRuleHalfMoveLimit)
            Result = GameResult.FiftyMoveRuleDraw;
    }
}
