import { Component, inject, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';

import { Auth } from '../../core/auth';
import { Game } from '../../core/game';
import { GameHistoryEntry, TimeControl } from '../../core/models/game.models';

interface TimeControlOption {
  label: string;
  value: TimeControl | null;
}

const TIME_CONTROL_OPTIONS: TimeControlOption[] = [
  { label: 'Untimed', value: null },
  { label: '5 min', value: { initialMinutes: 5, incrementSeconds: 0 } },
  { label: '10 min', value: { initialMinutes: 10, incrementSeconds: 0 } },
  { label: '15 | 10', value: { initialMinutes: 15, incrementSeconds: 10 } },
];

const HISTORY_PAGE_SIZE = 10;

@Component({
  selector: 'app-home',
  imports: [
    DatePipe,
    FormsModule,
    MatButtonModule,
    MatButtonToggleModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
    MatProgressSpinnerModule,
  ],
  templateUrl: './home.html',
  styleUrl: './home.scss',
})
export class Home {
  protected readonly auth = inject(Auth);
  private readonly game = inject(Game);
  private readonly router = inject(Router);

  protected readonly isCreating = signal(false);
  protected readonly joinGameId = signal('');
  protected readonly errorMessage = signal<string | null>(null);

  protected readonly timeControlOptions = TIME_CONTROL_OPTIONS;
  protected readonly selectedTimeControlIndex = signal(0);

  protected readonly history = signal<GameHistoryEntry[]>([]);
  protected readonly historyPage = signal(1);
  protected readonly historyTotalPages = signal(0);
  protected readonly historyLoading = signal(false);
  protected readonly historyError = signal(false);

  constructor() {
    this.loadHistory(1);
  }

  protected logout(): void {
    this.auth.logout();
    this.router.navigateByUrl('/login');
  }

  protected async createGame(color?: 'white' | 'black'): Promise<void> {
    if (this.isCreating()) {
      return;
    }

    this.errorMessage.set(null);
    this.isCreating.set(true);

    try {
      const timeControl = this.timeControlOptions[this.selectedTimeControlIndex()].value ?? undefined;
      const response = await this.game.createGame(color, timeControl);
      await this.router.navigate(['/game', response.gameId]);
    } catch {
      this.errorMessage.set('Could not create a game.');
    } finally {
      this.isCreating.set(false);
    }
  }

  protected async joinGame(): Promise<void> {
    const gameId = this.joinGameId().trim();
    if (!gameId) {
      return;
    }

    await this.router.navigate(['/game', gameId]);
  }

  protected async loadHistory(page: number): Promise<void> {
    this.historyLoading.set(true);
    this.historyError.set(false);

    try {
      const result = await this.game.getHistory(page, HISTORY_PAGE_SIZE);
      this.history.set(result.items);
      this.historyPage.set(result.page);
      this.historyTotalPages.set(result.totalPages);
    } catch {
      this.historyError.set(true);
    } finally {
      this.historyLoading.set(false);
    }
  }

  protected openGame(entry: GameHistoryEntry): void {
    this.router.navigate(['/game', entry.gameId]);
  }
}
