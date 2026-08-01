using Chess.Logic.Board;
using Chess.Logic.Pieces.Chess;

var board = new ChessBoard();
var playerMoving = Team.White;
var cursor = new PieceCord(4, 1);
PieceCord? selected = null;
PieceMove[] legalMoves = [];
var statusMessage = string.Empty;

while (true)
{
    Render();

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
        var piece = board.Spots[cursor.X][cursor.Y].Piece;
        if (piece is null || piece.Team != playerMoving)
        {
            statusMessage = "Select one of your own pieces.";
            return;
        }

        selected = cursor;
        legalMoves = board.GetLegalMoves(cursor);
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
        if (board.Spots[from.X][from.Y].Piece is Pawn && cursor.Y is 0 or 7)
            promotion = PromptPromotionChoice();

        board.MakeMove(new PieceMove(from, cursor), playerMoving, promotion);
        playerMoving = playerMoving == Team.White ? Team.Black : Team.White;
        statusMessage = string.Empty;
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

    foreach (var record in board.MoveHistory)
    {
        Console.WriteLine(FormatMove(record));
    }

    Console.Write("-----\n");
    PrintFileHeader();

    for (var y = 0; y < board.Spots.Length; y++)
    {
        Console.Write($"{y} ");
        for (var x = 0; x < board.Spots.Length; x++)
        {
            DrawSquare(x, y);
        }

        Console.ResetColor();
        Console.Write($" {y}");
        Console.WriteLine();
    }

    PrintFileHeader();
    Console.Write("-----\n");

    Console.WriteLine($"{playerMoving}: arrows to move, Enter to select/move, Esc to cancel, Q to quit.");
    if (board.IsInCheck(playerMoving))
        Console.WriteLine($"{playerMoving} is in check!");
    if (!string.IsNullOrEmpty(statusMessage))
        Console.WriteLine(statusMessage);
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

    var piece = board.Spots[x][y].Piece;
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
    for (var file = 0; file < board.Spots.Length; file++)
    {
        Console.Write(file);
    }

    Console.WriteLine();
}
