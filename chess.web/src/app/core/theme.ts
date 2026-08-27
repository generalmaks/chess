import { computed, Injectable, signal } from '@angular/core';

const STORAGE_KEY = 'chess-theme';
const DARK_CLASS = 'dark-theme';

export type ThemeMode = 'light' | 'dark';

@Injectable({ providedIn: 'root' })
export class Theme {
  private readonly mode = signal<ThemeMode>(this.readStoredMode());
  readonly isDark = computed(() => this.mode() === 'dark');

  constructor() {
    this.applyMode(this.mode());
  }

  toggle(): void {
    const next: ThemeMode = this.mode() === 'dark' ? 'light' : 'dark';
    this.mode.set(next);
    localStorage.setItem(STORAGE_KEY, next);
    this.applyMode(next);
  }

  private readStoredMode(): ThemeMode {
    return localStorage.getItem(STORAGE_KEY) === 'dark' ? 'dark' : 'light';
  }

  private applyMode(mode: ThemeMode): void {
    document.documentElement.classList.toggle(DARK_CLASS, mode === 'dark');
  }
}
