import { Injectable, inject } from '@angular/core';
import { OAuthService } from 'angular-oauth2-oidc';
import { authConfig } from './auth.config';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly oauthService = inject(OAuthService);

  configure(): void {
    this.oauthService.configure(authConfig);
  }

  initialize(): Promise<boolean> {
    this.configure();
    return this.oauthService.loadDiscoveryDocumentAndTryLogin();
  }

  login(): void {
    if (!this.oauthService.discoveryDocumentLoaded) {
      void this.oauthService.loadDiscoveryDocument().then(() => this.oauthService.initCodeFlow());
      return;
    }

    this.oauthService.initCodeFlow();
  }

  logout(): void {
    this.oauthService.logOut();
  }

  get isAuthenticated(): boolean {
    return this.oauthService.hasValidAccessToken();
  }

  get accessToken(): string {
    return this.oauthService.getAccessToken();
  }

  get identityClaims(): Record<string, unknown> {
    return this.oauthService.getIdentityClaims() as Record<string, unknown>;
  }

  get userName(): string {
    const claims = this.identityClaims;
    return (
      (claims['preferred_username'] as string) ??
      (claims['name'] as string) ??
      (claims['email'] as string) ??
      'User'
    );
  }

  get userId(): string {
    return (this.identityClaims['sub'] as string) ?? '';
  }

  get userEmail(): string {
    return (this.identityClaims['email'] as string) ?? this.userName;
  }
}
