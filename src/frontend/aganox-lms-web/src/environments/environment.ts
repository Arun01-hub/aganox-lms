export const environment = {
  production: false,
  apiBaseUrl: '/api/v1',
  oidc: {
    enabled: false,
    issuer: 'https://localhost:7036',
    clientId: 'aganox-lms-web',
    redirectUri: 'http://localhost:4200/login',
    postLogoutRedirectUri: 'http://localhost:4200/login',
    scope: 'openid profile email aganox.lms.api',
    responseType: 'code',
    showDebugInformation: true
  }
};
