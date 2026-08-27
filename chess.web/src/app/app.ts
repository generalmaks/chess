import { Component, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { MatIconButton } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';

import { Theme } from './core/theme';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, MatIconButton, MatIconModule],
  templateUrl: './app.html',
  styleUrl: './app.scss'
})
export class App {
  protected readonly title = signal('chess.web');
  protected readonly theme = inject(Theme);
}
