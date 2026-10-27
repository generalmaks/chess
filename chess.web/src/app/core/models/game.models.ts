export type Team = 'White' | 'Black';

export type GameResult =
  | 'Ongoing'
  | 'WhiteWonByCheckmate'
  | 'BlackWonByCheckmate'
  | 'WhiteWonByResignation'
  | 'BlackWonByResignation'
  | 'WhiteWonByAbandonment'
  | 'BlackWonByAbandonment'
  | 'WhiteWonOnTime'
  | 'BlackWonOnTime'
  | 'StalemateDraw'
  | 'FiftyMoveRuleDraw'
  | 'DrawByAgreement'
  | 'InsufficientMaterialDraw'
  | 'ThreefoldRepetitionDraw'
  | 'TimeoutInsufficientMaterialDraw';

export type GameOutcome = 'Win' | 'Loss' | 'Draw' | 'Ongoing' | 'Unknown';

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface GameHistoryEntry {
  gameId: string;
  createdAtUtc: string;
  endedAtUtc: string | null;
  result: GameResult;
  playerColor: Team;
  outcome: GameOutcome;
  opponentName: string | null;
  playerEloBefore: number | null;
  playerEloAfter: number | null;
  moveCount: number;
}

export interface TimeControl {
  initialMinutes: number;
  incrementSeconds: number;
}

export interface CreateGameResponse {
  gameId: string;
  team: Team;
}

export interface GameStateDto {
  board: (string | null)[][];
  currentTurn: Team;
  result: GameResult;
  whiteTimeRemainingSeconds: number | null;
  blackTimeRemainingSeconds: number | null;
  lastMove: MoveHistoryEntryDto | null;
}

export interface JoinGameResponse {
  team: Team;
  state: GameStateDto;
  opponentJoined: boolean;
  moves: MoveHistoryEntryDto[];
  whitePlayerName: string | null;
  blackPlayerName: string | null;
}

export interface GameReviewDto {
  gameId: string;
  result: GameResult;
  playerColor: Team | null;
  whitePlayerName: string | null;
  blackPlayerName: string | null;
  moves: MoveHistoryEntryDto[];
}

export interface MoveHistoryEntryDto {
  fromX: number;
  fromY: number;
  toX: number;
  toY: number;
  promotedTo: string | null;
  san: string;
}

export interface SquareCoordinate {
  x: number;
  y: number;
}

export interface MoveRequest {
  fromX: number;
  fromY: number;
  toX: number;
  toY: number;
  promotion?: string | null;
}
