import { ApplicationConfig, LOCALE_ID } from '@angular/core';
import { registerLocaleData } from '@angular/common';
import localeId from '@angular/common/locales/id';
import { provideRouter } from '@angular/router';
import { provideHttpClient, withInterceptors } from '@angular/common/http';

import { routes } from './app.routes';
import { authInterceptor } from './services/auth/auth.interceptor';

// Register Indonesian locale so Angular pipes can safely use 'id-ID'.
registerLocaleData(localeId, 'id-ID');

export const appConfig: ApplicationConfig = {
  providers: [
    { provide: LOCALE_ID, useValue: 'id-ID' },
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor]))
  ]
};
