export const environment = {
  production: true,
  apiBaseUrl: '/api/v1',
  oidc: {
    enabled: false,
    issuer: '',
    clientId: '',
    redirectUri: '',
    postLogoutRedirectUri: '',
    scope: 'openid profile email',
    responseType: 'code',
    showDebugInformation: false
  }
};
