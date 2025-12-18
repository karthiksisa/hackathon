import { Injectable, signal, effect, PLATFORM_ID, inject } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

export type Theme = 'light' | 'dark';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private platformId = inject(PLATFORM_ID);
  
  theme = signal<Theme>('light');

  constructor() {
    if (isPlatformBrowser(this.platformId)) {
      const storedTheme = localStorage.getItem('theme') as Theme | null;
      const prefersDark = window.matchMedia('(prefers-color-scheme: dark)').matches;
      this.theme.set(storedTheme || (prefersDark ? 'dark' : 'light'));
      
      effect(() => {
        const currentTheme = this.theme();
        localStorage.setItem('theme', currentTheme);
        if (currentTheme === 'dark') {
          document.documentElement.classList.add('dark');
        } else {
          document.documentElement.classList.remove('dark');
        }
      });
    }
  }

  toggleTheme() {
    this.theme.update(current => (current === 'light' ? 'dark' : 'light'));
  }
}
