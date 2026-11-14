import { AuthConfig } from 'angular-oauth2-oidc';
import { environment } from '../../environments/environment';

export function buildOidcAuthConfig(): AuthConfig {
  return {
    issuer: environment.oidc.issuer,
    clientId: environment.oidc.clientId,
    redirectUri: environment.oidc.redirectUri,
    postLogoutRedirectUri: environment.oidc.postLogoutRedirectUri,
    responseType: environment.oidc.responseType,
    scope: environment.oidc.scope,
    showDebugInformation: environment.oidc.showDebugInformation,
    requireHttps: false,
    strictDiscoveryDocumentValidation: false,
    useSilentRefresh: false
  };
}
