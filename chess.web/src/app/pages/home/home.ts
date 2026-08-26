import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';

import { Auth } from '../../core/auth';
import { Game } from '../../core/game';
import { TimeControl } from '../../core/models/game.models';

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

@Component({
  selector: 'app-home',
  imports: [
    FormsModule,
    MatButtonModule,
    MatButtonToggleModule,
    MatCardModule,
    MatFormFieldModule,
    MatInputModule,
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
}
