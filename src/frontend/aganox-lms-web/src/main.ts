import { bootstrapApplication } from '@angular/platform-browser';
import { registerLocaleData } from '@angular/common';
import localeTa from '@angular/common/locales/ta';
import localeEn from '@angular/common/locales/en';
import { appConfig } from './app/app.config';
import { AppComponent } from './app/app.component';

registerLocaleData(localeTa);
registerLocaleData(localeEn);

bootstrapApplication(AppComponent, appConfig).catch((err) => console.error(err));
