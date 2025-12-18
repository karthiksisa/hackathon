import { bootstrapApplication } from '@angular/platform-browser';
import { AppComponent } from './app.component';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { ApplicationConfig } from '@angular/core';
import { authInterceptor } from './services/auth.interceptor';

export const appConfig: ApplicationConfig = {
    providers: [
        provideHttpClient(withInterceptors([authInterceptor]))
    ]
};

bootstrapApplication(AppComponent, appConfig)
    .catch((err) => console.error(err));
