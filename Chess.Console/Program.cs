using Chess.Logic;
using Chess.Logic.Board;
using Chess.Logic.Pieces.Chess;

var session = new ChessGameSession();
var cursor = new PieceCord(4, 1);
PieceCord? selected = null;
PieceMove[] legalMoves = [];
var statusMessage = string.Empty;

while (true)
{
    Render();

    if (session.Result != GameResult.Ongoing)
    {
        if (Console.ReadKey(intercept: true).Key == ConsoleKey.Q)
            return;
        continue;
    }

    var key = Console.ReadKey(intercept: true).Key;
    switch (key)
    {
        case ConsoleKey.LeftArrow:
            cursor = MoveCursor(cursor, -1, 0);
            break;
        case ConsoleKey.RightArrow:
            cursor = MoveCursor(cursor, 1, 0);
            break;
        case ConsoleKey.UpArrow:
            cursor = MoveCursor(cursor, 0, -1);
            break;
        case ConsoleKey.DownArrow:
            cursor = MoveCursor(cursor, 0, 1);
            break;
        case ConsoleKey.Enter:
        case ConsoleKey.Spacebar:
            HandleSelect();
            break;
        case ConsoleKey.Escape:
            selected = null;
            legalMoves = [];
            statusMessage = string.Empty;
            break;
        case ConsoleKey.R:
            HandleResign();
            break;
        case ConsoleKey.D:
            HandleDrawOffer();
            break;
        case ConsoleKey.Q:
            return;
    }
}

PieceCord MoveCursor(PieceCord from, int dx, int dy) =>
    new(Math.Clamp(from.X + dx, 0, 7), Math.Clamp(from.Y + dy, 0, 7));

void HandleSelect()
{
    if (selected is not { } from)
    {
        var piece = session.Board.Spots[cursor.X][cursor.Y].Piece;
        if (piece is null || piece.Team != session.CurrentTurn)
        {
            statusMessage = "Select one of your own pieces.";
            return;
        }

        selected = cursor;
        legalMoves = session.Board.GetLegalMoves(cursor);
        statusMessage = string.Empty;
        return;
    }

    if (from == cursor)
    {
        selected = null;
        legalMoves = [];
        return;
    }

    try
    {
        char? promotion = null;
        if (session.Board.Spots[from.X][from.Y].Piece is Pawn && cursor.Y is 0 or 7 &&
            legalMoves.Any(m => m.To == cursor))
            promotion = PromptPromotionChoice();

        session.MakeMove(new PieceMove(from, cursor), promotion);
        statusMessage = FormatResultMessage(session.Result);
    }
    catch (IllegalMoveException ex)
    {
        statusMessage = $"Invalid move: {ex.Message}";
    }
    finally
    {
        selected = null;
        legalMoves = [];
    }
}

void HandleResign()
{
    if (!ConfirmAction($"{session.CurrentTurn} resigns - press Y to confirm, any other key to cancel."))
        return;

    session.Resign(session.CurrentTurn);
    statusMessage = FormatResultMessage(session.Result);
    selected = null;
    legalMoves = [];
}

void HandleDrawOffer()
{
    if (!ConfirmAction("Agree to a draw? Press Y to confirm, any other key to cancel."))
        return;

    session.AgreeToDraw();
    statusMessage = FormatResultMessage(session.Result);
    selected = null;
    legalMoves = [];
}

bool ConfirmAction(string prompt)
{
    Render();
    Console.WriteLine(prompt);
    return Console.ReadKey(intercept: true).Key == ConsoleKey.Y;
}

string FormatResultMessage(GameResult result) => result switch
{
    GameResult.WhiteWonByCheckmate => "Checkmate! White wins. Press Q to quit.",
    GameResult.BlackWonByCheckmate => "Checkmate! Black wins. Press Q to quit.",
    GameResult.WhiteWonByResignation => "Black resigns. White wins. Press Q to quit.",
    GameResult.BlackWonByResignation => "White resigns. Black wins. Press Q to quit.",
    GameResult.WhiteWonByAbandonment => "Black abandoned the game. White wins. Press Q to quit.",
    GameResult.BlackWonByAbandonment => "White abandoned the game. Black wins. Press Q to quit.",
    GameResult.StalemateDraw => "Stalemate! The game is a draw. Press Q to quit.",
    GameResult.FiftyMoveRuleDraw => "Draw by the fifty-move rule. Press Q to quit.",
    GameResult.DrawByAgreement => "Draw by agreement. Press Q to quit.",
    _ => string.Empty
};

char PromptPromotionChoice()
{
    Render();
    Console.WriteLine("Promote to: (Q)ueen, (R)ook, (B)ishop, (N)ight");
    while (true)
    {
        switch (Console.ReadKey(intercept: true).Key)
        {
            case ConsoleKey.Q: return 'Q';
            case ConsoleKey.R: return 'R';
            case ConsoleKey.B: return 'B';
            case ConsoleKey.N: return 'N';
        }
    }
}

void Render()
{
    Console.Clear();

    foreach (var record in session.Board.MoveHistory)
    {
        Console.WriteLine(FormatMove(record));
    }

    Console.Write("-----\n");
    Console.WriteLine($"Captured by White: {FormatCaptured(session.PiecesCapturedByWhite)}");
    Console.WriteLine($"Captured by Black: {FormatCaptured(session.PiecesCapturedByBlack)}");
    Console.Write("-----\n");
    PrintFileHeader();

    for (var y = 0; y < session.Board.Spots.Length; y++)
    {
        Console.Write($"{y} ");
        for (var x = 0; x < session.Board.Spots.Length; x++)
        {
            DrawSquare(x, y);
        }

        Console.ResetColor();
        Console.Write($" {y}");
        Console.WriteLine();
    }

    PrintFileHeader();
    Console.Write("-----\n");

    Console.WriteLine($"{session.CurrentTurn}: arrows to move, Enter to select/move, Esc to cancel, R to resign, D to draw, Q to quit.");
    if (session.Result == GameResult.Ongoing && session.Board.IsInCheck(session.CurrentTurn))
        Console.WriteLine($"{session.CurrentTurn} is in check!");
    if (!string.IsNullOrEmpty(statusMessage))
        Console.WriteLine(statusMessage);
}

string FormatCaptured(IEnumerable<Piece> pieces)
{
    var codes = pieces.Select(p => p.PieceCode).ToArray();
    return codes.Length == 0 ? "-" : string.Join(' ', codes);
}

string FormatMove(MoveRecord record)
{
    if (record.IsCastling)
        return record.Move.To.X > record.Move.From.X ? "O-O" : "O-O-O";

    var suffix = record.PromotedTo is { } promotedTo ? $"={promotedTo}"
        : record.IsEnPassant ? " e.p."
        : string.Empty;
    return $"{record.PieceCode}{record.Move.From.X}{record.Move.From.Y}->{record.PieceCode}{record.Move.To.X}{record.Move.To.Y}{suffix}";
}

void DrawSquare(int x, int y)
{
    var coord = new PieceCord(x, y);
    var isCursor = coord == cursor;
    var isSelected = selected == coord;
    var isLegalTarget = legalMoves.Any(m => m.To == coord);

    Console.BackgroundColor = isCursor ? ConsoleColor.Yellow
        : isSelected ? ConsoleColor.Cyan
        : isLegalTarget ? ConsoleColor.Green
        : ConsoleColor.Gray;

    var piece = session.Board.Spots[x][y].Piece;
    if (piece != null)
    {
        Console.ForegroundColor = piece.Team == Team.White ? ConsoleColor.White : ConsoleColor.Black;
        Console.Write(piece.PieceCode);
    }
    else
    {
        Console.ForegroundColor = isCursor || isSelected || isLegalTarget ? ConsoleColor.Black : ConsoleColor.Gray;
        Console.Write(isLegalTarget ? '*' : 'x');
    }
}

void PrintFileHeader()
{
    Console.Write("  ");
    for (var file = 0; file < session.Board.Spots.Length; file++)
    {
        Console.Write(file);
    }

    Console.WriteLine();
}
