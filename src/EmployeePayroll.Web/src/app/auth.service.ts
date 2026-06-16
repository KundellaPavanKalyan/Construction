import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { environment } from '../environments/environment';

const STORAGE_KEY = 'payroll_jwt';

export interface LoginResponse {
  token: string;
  username: string;
  expiresInSeconds: number;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly base = environment.apiUrl;

  readonly token = signal<string | null>(typeof sessionStorage !== 'undefined' ? sessionStorage.getItem(STORAGE_KEY) : null);

  isLoggedIn(): boolean {
    return !!this.token();
  }

  login(username: string, password: string): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${this.base}/api/auth/login`, { username, password }).pipe(
      tap((res) => {
        sessionStorage.setItem(STORAGE_KEY, res.token);
        this.token.set(res.token);
      })
    );
  }

  logout(): void {
    sessionStorage.removeItem(STORAGE_KEY);
    this.token.set(null);
    void this.router.navigateByUrl('/login');
  }
}
