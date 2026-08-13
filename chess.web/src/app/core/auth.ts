import { Service, computed, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';

import { API_BASE_URL } from './api-config';
import { AuthResponse, LoginRequest, RegisterRequest } from './models/auth.models';

const STORAGE_KEY = 'chess.auth';

interface StoredAuth {
  token: string;
  username: string;
  eloRating: number;
}

@Service()
export class Auth {
  private readonly http = inject(HttpClient);

  private readonly currentAuth = signal<StoredAuth | null>(this.readFromStorage());

  readonly isAuthenticated = computed(() => this.currentAuth() !== null);
  readonly username = computed(() => this.currentAuth()?.username ?? null);
  readonly eloRating = computed(() => this.currentAuth()?.eloRating ?? null);

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${API_BASE_URL}/auth/login`, request)
      .pipe(tap((response) => this.storeAuth(response)));
  }

  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http
      .post<AuthResponse>(`${API_BASE_URL}/auth/register`, request)
      .pipe(tap((response) => this.storeAuth(response)));
  }

  logout(): void {
    this.currentAuth.set(null);
    localStorage.removeItem(STORAGE_KEY);
  }

  getToken(): string | null {
    return this.currentAuth()?.token ?? null;
  }

  private storeAuth(response: AuthResponse): void {
    const auth: StoredAuth = {
      token: response.token,
      username: response.username,
      eloRating: response.eloRating
    };
    this.currentAuth.set(auth);
    localStorage.setItem(STORAGE_KEY, JSON.stringify(auth));
  }

  private readFromStorage(): StoredAuth | null {
    const raw = localStorage.getItem(STORAGE_KEY);
    if (!raw) {
      return null;
    }
    try {
      return JSON.parse(raw) as StoredAuth;
    } catch {
      return null;
    }
  }
}
