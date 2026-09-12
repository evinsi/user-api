import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { tap } from 'rxjs';
import { API_URL } from '../constants';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private readonly tokenKey = 'token';

  // API'ye giriş isteği at, dönen token'ı tarayıcıda sakla
  login(email: string, password: string) {
    return this.http
      .post<{ token: string }>(`${API_URL}/auth/login`, { email, password })
      .pipe(tap((res) => localStorage.setItem(this.tokenKey, res.token)));
  }

  logout() {
    localStorage.removeItem(this.tokenKey);
  }

  get token(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  isLoggedIn(): boolean {
    return !!this.token;
  }
}
