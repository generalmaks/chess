import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { beforeEach, describe, expect, it, vi } from 'vitest';

import { Home } from './home';
import { Game } from '../../core/game';
import { Auth } from '../../core/auth';
import { GameHistoryEntry, PagedResult } from '../../core/models/game.models';

function entry(overrides: Partial<GameHistoryEntry> = {}): GameHistoryEntry {
  return {
    gameId: 'g1',
    createdAtUtc: '2026-08-01T12:00:00Z',
    endedAtUtc: '2026-08-01T12:30:00Z',
    result: 'WhiteWonByCheckmate',
    playerColor: 'White',
    outcome: 'Win',
    opponentName: 'rival',
    playerEloBefore: 1200,
    playerEloAfter: 1215,
    moveCount: 30,
    ...overrides,
  };
}

describe('Home game history', () => {
  const gameMock = {
    getHistory: vi.fn<(page: number, pageSize?: number) => Promise<PagedResult<GameHistoryEntry>>>(),
  };
  const authMock = { username: () => 'me', eloRating: () => 1215, logout: vi.fn() };

  beforeEach(() => {
    gameMock.getHistory.mockReset();
    TestBed.configureTestingModule({
      imports: [Home],
      providers: [
        provideRouter([]),
        { provide: Game, useValue: gameMock },
        { provide: Auth, useValue: authMock },
      ],
    });
  });

  it('loads the first page on init and renders each game', async () => {
    gameMock.getHistory.mockResolvedValue({
      items: [entry({ gameId: 'a', opponentName: 'alice' }), entry({ gameId: 'b', opponentName: 'bob' })],
      page: 1,
      pageSize: 10,
      totalCount: 2,
      totalPages: 1,
    });

    const fixture = TestBed.createComponent(Home);
    await fixture.whenStable();
    fixture.detectChanges();

    expect(gameMock.getHistory).toHaveBeenCalledWith(1, 10);
    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('vs alice');
    expect(text).toContain('vs bob');
  });

  it('shows an empty state when there are no games', async () => {
    gameMock.getHistory.mockResolvedValue({ items: [], page: 1, pageSize: 10, totalCount: 0, totalPages: 0 });

    const fixture = TestBed.createComponent(Home);
    await fixture.whenStable();
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain("haven't played any games");
  });

  it('paginates to the next page', async () => {
    gameMock.getHistory
      .mockResolvedValueOnce({ items: [entry()], page: 1, pageSize: 10, totalCount: 15, totalPages: 2 })
      .mockResolvedValueOnce({ items: [entry({ gameId: 'p2' })], page: 2, pageSize: 10, totalCount: 15, totalPages: 2 });

    const fixture = TestBed.createComponent(Home);
    await fixture.whenStable();
    fixture.detectChanges();

    const component = fixture.componentInstance as unknown as { loadHistory(page: number): Promise<void> };
    await component.loadHistory(2);

    expect(gameMock.getHistory).toHaveBeenLastCalledWith(2, 10);
  });
});
