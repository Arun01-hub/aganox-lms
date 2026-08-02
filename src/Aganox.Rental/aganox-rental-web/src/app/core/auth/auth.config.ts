import { AuthConfig } from 'angular-oauth2-oidc';
import { environment } from '../../../environments/environment';

export const authConfig: AuthConfig = {
  issuer: environment.identityServerUrl,
  redirectUri: window.location.origin,
  postLogoutRedirectUri: window.location.origin,
  clientId: 'aganox-rental',
  responseType: 'code',
  scope: 'openid profile email offline_access aganox.api',
  showDebugInformation: !environment.production,
  requireHttps: environment.production,
  strictDiscoveryDocumentValidation: false,
  useSilentRefresh: false
};
