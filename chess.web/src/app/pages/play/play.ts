import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';

import { Game } from '../../core/game';

@Component({
  selector: 'app-play',
  imports: [FormsModule, RouterLink],
  templateUrl: './play.html',
  styleUrl: './play.scss',
})
export class Play {
  private readonly game = inject(Game);
  private readonly router = inject(Router);

  protected readonly isCreating = signal(false);
  protected readonly joinGameId = signal('');
  protected readonly errorMessage = signal<string | null>(null);

  protected async createGame(color?: 'white' | 'black'): Promise<void> {
    if (this.isCreating()) {
      return;
    }

    this.errorMessage.set(null);
    this.isCreating.set(true);

    try {
      const response = await this.game.createGame(color);
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
