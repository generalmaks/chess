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
  | 'DrawByAgreement';

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
}

export interface JoinGameResponse {
  team: Team;
  state: GameStateDto;
  opponentJoined: boolean;
}

export interface MoveRequest {
  fromX: number;
  fromY: number;
  toX: number;
  toY: number;
  promotion?: string | null;
}
