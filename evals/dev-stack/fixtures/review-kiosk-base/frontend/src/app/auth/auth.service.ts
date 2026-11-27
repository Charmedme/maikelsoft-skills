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

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  // The token lives in memory only. Scripts that run in the page cannot read it from web storage.
  private readonly current = signal<string | null>(null);

  login(email: string, password: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>('/api/auth/login', { email, password }).pipe(
      tap((response) => this.current.set(response.token)),
    );
  }

  logout(): void {
    this.current.set(null);
  }

  token(): string | null {
    return this.current();
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
