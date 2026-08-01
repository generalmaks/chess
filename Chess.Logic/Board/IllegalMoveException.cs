namespace Chess.Logic.Board;

public class IllegalMoveException(string message) : Exception(message);

public class EnemyPieceMovingException() : IllegalMoveException("Can't move enemy piece");

public class MovingOverOwnPiecesException() : IllegalMoveException("Can't move piece over your piece");

public class NoPossibleMovesException() : IllegalMoveException("Move is not legal for this piece");

public class NonExistingPieceException() : IllegalMoveException("Can't move non-existent piece");
