import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { beforeEach, describe, expect, it } from 'vitest';

import { Game } from './game';
import { API_BASE_URL } from './api-config';
import { PagedResult, GameHistoryEntry } from './models/game.models';

describe('Game.getHistory', () => {
  let service: Game;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(Game);
    http = TestBed.inject(HttpTestingController);
  });

  it('requests the given page and returns the paged result', async () => {
    const expected: PagedResult<GameHistoryEntry> = {
      items: [],
      page: 3,
      pageSize: 10,
      totalCount: 25,
      totalPages: 3,
    };

    const promise = service.getHistory(3, 10);

    const req = http.expectOne(`${API_BASE_URL}/games/history?page=3&pageSize=10`);
    expect(req.request.method).toBe('GET');
    req.flush(expected);

    await expect(promise).resolves.toEqual(expected);
    http.verify();
  });
});
