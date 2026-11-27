import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';

export interface LoginResponse {
  token: string;
  email: string;
  role: string;
}

export interface TokenPayload {
  sub?: string;
  email?: string;
  role?: string;
  exp?: number;
}

const TOKEN_KEY = 'token';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly current = signal<string | null>(localStorage.getItem(TOKEN_KEY));

  login(email: string, password: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>('/api/auth/login', { email, password }).pipe(
      tap((response) => {
        localStorage.setItem(TOKEN_KEY, response.token);
        this.current.set(response.token);
      }),
    );
  }

  logout(): void {
    localStorage.removeItem(TOKEN_KEY);
    this.current.set(null);
  }

  token(): string | null {
    return localStorage.getItem(TOKEN_KEY) ?? this.current();
  }

  isLoggedIn(): boolean {
    return this.token() !== null;
  }

  isAdmin(): boolean {
    return this.payload()?.role === 'admin';
  }

  payload(): TokenPayload | null {
    const token = this.token();
    const part = token?.split('.')[1];
    if (!part) {
      return null;
    }

    try {
      return JSON.parse(atob(part.replace(/-/g, '+').replace(/_/g, '/'))) as TokenPayload;
    } catch {
      return null;
    }
  }
}
