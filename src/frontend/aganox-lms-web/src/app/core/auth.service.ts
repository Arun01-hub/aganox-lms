import { Injectable, inject, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { OAuthService } from 'angular-oauth2-oidc';
import { catchError, tap } from 'rxjs/operators';
import { firstValueFrom, of } from 'rxjs';
import { environment } from '../../environments/environment';
import { buildOidcAuthConfig } from './oidc.config';

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
  private readonly oauth = inject(OAuthService, { optional: true });
  readonly user = signal<CurrentUser | null>(this.read()?.user ?? null);

  readonly oidcEnabled = environment.oidc.enabled;

  constructor(
    private readonly http: HttpClient,
    private readonly router: Router
  ) {}

  get accessToken(): string | null {
    if (this.oidcEnabled && this.oauth?.hasValidAccessToken()) {
      return this.oauth.getAccessToken();
    }

    return this.read()?.accessToken ?? null;
  }

  get isAuthenticated(): boolean {
    if (this.oidcEnabled && this.oauth?.hasValidAccessToken()) {
      return true;
    }

    return !!this.read()?.accessToken;
  }

  hasPermission(permission: string): boolean {
    return this.user()?.permissions.includes(permission) ?? false;
  }

  hasRole(role: string): boolean {
    return this.user()?.roles.includes(role) ?? false;
  }

  async initOidc(): Promise<void> {
    if (!this.oidcEnabled || !this.oauth) {
      return;
    }

    this.oauth.configure(buildOidcAuthConfig());
    try {
      await this.oauth.loadDiscoveryDocumentAndTryLogin();
    } catch {
      // Identity server may be stopped; local LMS login should still work.
      return;
    }

    if (this.oauth.hasValidAccessToken()) {
      await this.refreshCurrentUser();
    }
  }

  loginWithOidc(): void {
    if (!this.oidcEnabled || !this.oauth) {
      return;
    }

    this.oauth.initCodeFlow();
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

  forgotPassword(email: string) {
    return this.http.post<void>(`${environment.apiBaseUrl}/auth/forgot-password`, { email });
  }

  resetPassword(email: string, token: string, newPassword: string) {
    return this.http.post<void>(`${environment.apiBaseUrl}/auth/reset-password`, { email, token, newPassword });
  }

  changePassword(currentPassword: string, newPassword: string) {
    return this.http.post<void>(`${environment.apiBaseUrl}/auth/change-password`, { currentPassword, newPassword });
  }

  /** Reload roles/permissions from the server (e.g. after backend permission seed updates). */
  refreshCurrentUser(): Promise<void> {
    if (!this.accessToken) {
      return Promise.resolve();
    }

    return firstValueFrom(
      this.http.get<CurrentUser>(`${environment.apiBaseUrl}/auth/me`).pipe(
        tap((user) => {
          if (this.oidcEnabled && this.oauth?.hasValidAccessToken()) {
            this.user.set(user);
            return;
          }

          const stored = this.read();
          if (!stored) {
            return;
          }

          const updated: AuthResponse = { ...stored, user };
          localStorage.setItem(this.storageKey, JSON.stringify(updated));
          this.user.set(user);
        }),
        catchError(() => of(null))
      )
    ).then(() => undefined);
  }

  logout(): void {
    if (this.oidcEnabled && this.oauth?.hasValidAccessToken()) {
      this.user.set(null);
      this.oauth.logOut();
      return;
    }

    const refreshToken = this.read()?.refreshToken;
    if (refreshToken && this.read()?.accessToken) {
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
