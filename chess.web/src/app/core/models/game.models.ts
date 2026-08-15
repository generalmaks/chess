export type Team = 'White' | 'Black';

export type GameResult =
  | 'Ongoing'
  | 'WhiteWon'
  | 'BlackWon'
  | 'StalemateDraw'
  | 'FiftyMoveRuleDraw'
  | 'DrawByAgreement';

export interface CreateGameResponse {
  gameId: string;
  team: Team;
}

export interface GameStateDto {
  board: (string | null)[][];
  currentTurn: Team;
  result: GameResult;
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
