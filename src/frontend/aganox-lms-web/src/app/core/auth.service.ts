import { Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { tap } from 'rxjs/operators';
import { environment } from '../../environments/environment';

export interface CurrentUser {
  id: string;
  email: string;
  userName: string;
  employeeId?: string | null;
  mustChangePassword: boolean;
  roles: string[];
  permissions: string[];
}

export interface AuthResponse {
  accessToken: string;
  expiresIn: number;
  refreshToken: string;
  user: CurrentUser;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly storageKey = 'aganox.lms.auth';
  readonly user = signal<CurrentUser | null>(this.read()?.user ?? null);

  constructor(
    private readonly http: HttpClient,
    private readonly router: Router
  ) {}

  get accessToken(): string | null {
    return this.read()?.accessToken ?? null;
  }

  get isAuthenticated(): boolean {
    return !!this.accessToken;
  }

  login(email: string, password: string) {
    return this.http
      .post<AuthResponse>(`${environment.apiBaseUrl}/auth/login`, { email, password })
      .pipe(
        tap((response) => {
          localStorage.setItem(this.storageKey, JSON.stringify(response));
          this.user.set(response.user);
        })
      );
  }

  logout(): void {
    const refreshToken = this.read()?.refreshToken;
    if (refreshToken && this.accessToken) {
      this.http
        .post(`${environment.apiBaseUrl}/auth/logout`, { refreshToken })
        .subscribe({ error: () => undefined });
    }
    localStorage.removeItem(this.storageKey);
    this.user.set(null);
    void this.router.navigateByUrl('/login');
  }

  private read(): AuthResponse | null {
    const raw = localStorage.getItem(this.storageKey);
    if (!raw) {
      return null;
    }
    try {
      return JSON.parse(raw) as AuthResponse;
    } catch {
      return null;
    }
  }
}
