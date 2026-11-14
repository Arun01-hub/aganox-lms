import { ApplicationConfig, inject, provideAppInitializer, provideZoneChangeDetection } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideTranslateService } from '@ngx-translate/core';
import { provideTranslateHttpLoader } from '@ngx-translate/http-loader';
import { provideOAuthClient } from 'angular-oauth2-oidc';
import { routes } from './app.routes';
import { authInterceptor } from './core/auth.interceptor';
import { AuthService } from './core/auth.service';
import { LanguageService } from './core/language.service';
import { ThemeService } from './core/theme.service';
import { environment } from '../environments/environment';

export const appConfig: ApplicationConfig = {
  providers: [
    provideZoneChangeDetection({ eventCoalescing: true }),
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor])),
    ...(environment.oidc.enabled ? [provideOAuthClient()] : []),
    ...provideTranslateService({
      lang: 'ta',
      fallbackLang: 'ta',
      loader: provideTranslateHttpLoader({
        prefix: './assets/i18n/',
        suffix: '.json'
      })
    }),
    provideAppInitializer(async () => {
      const auth = inject(AuthService);
      const language = inject(LanguageService);
      const theme = inject(ThemeService);
      theme.init();
      await auth.initOidc();
      await auth.refreshCurrentUser();
      await language.init();
    })
  ]
};
