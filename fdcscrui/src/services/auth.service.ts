import { Injectable, signal, inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { User } from '../models/user.model';
import { API_BASE_URL } from '../config';
// FIX: Add 'map' operator from rxjs/operators to fix pipeable operator errors.
import { tap, catchError, map, switchMap } from 'rxjs/operators';
import { of, lastValueFrom } from 'rxjs';

const TOKEN_KEY = 'crm_auth_token';

interface LoginResponse {
  token: string;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private http = inject(HttpClient);
  private platformId = inject(PLATFORM_ID);

  readonly currentUser = signal<User | null>(null);

  constructor() {
    this.tryLoginFromToken();
  }

  private tryLoginFromToken() {
    if (isPlatformBrowser(this.platformId)) {
      const token = localStorage.getItem(TOKEN_KEY);
      if (token) {
        // In a real app, you'd send this token to a 'validate' or 'me' endpoint
        // For now, we'll fetch the current user based on a presumed valid token
        this.http.get<User>(`${API_BASE_URL}/Auth/current`).pipe(
          switchMap(basicUser => {
            if (!basicUser) return of(null);
            // Fetch full user details using the ID from basicUser
            return this.http.get<User>(`${API_BASE_URL}/Users/${basicUser.id}`).pipe(
              catchError(() => of(basicUser)) // Fallback to basicUser if detailed fetch fails
            );
          }),
          catchError(() => {
            this.clearToken();
            return of(null);
          })
        ).subscribe(user => {
          if (user) {
            // Robust mapping for potential PascalCase from backend
            const u = user as any;
            const mappedUser: User = {
              ...user,
              regionIds: u.regionIds || u.RegionIds,
              regionId: u.regionId || u.RegionId,
              role: u.role || u.Role,
              name: u.name || u.Name,
              email: u.email || u.Email
            };
            this.currentUser.set(mappedUser);
          }
        });
      }
    }
  }

  async login(email: string, password?: string): Promise<boolean> {
    const login$ = this.http.post<LoginResponse>(`${API_BASE_URL}/Auth/login`, { email, password }).pipe(
      tap(response => {
        if (response.token) {
          this.setToken(response.token);
        }
      }),
      // Switch map to get current user immediately after token is set
      switchMap(response => {
        if (response.token) {
          return this.http.get<User>(`${API_BASE_URL}/Auth/current`).pipe(
            switchMap(basicUser => {
              if (!basicUser) throw new Error('Failed to get current user');
              return this.http.get<User>(`${API_BASE_URL}/Users/${basicUser.id}`).pipe(
                catchError(() => of(basicUser))
              );
            }),
            tap(user => {
              const u = user as any;
              const mappedUser: User = {
                ...user,
                regionIds: u.regionIds || u.RegionIds,
                regionId: u.regionId || u.RegionId,
                role: u.role || u.Role,
                name: u.name || u.Name,
                email: u.email || u.Email
              };
              this.currentUser.set(mappedUser);
            }),
            map(() => true),
            catchError(() => of(false))
          );
        }
        return of(false);
      }),
      catchError(() => of(false))
    );
    return lastValueFrom<boolean>(login$);
  }

  logout() {
    this.http.post<void>(`${API_BASE_URL}/Auth/logout`, {}).subscribe(() => {
      this.currentUser.set(null);
      this.clearToken();
    });
  }



  async changePassword(currentPassword: string, newPassword: string): Promise<{ success: boolean; message: string; }> {
    const user = this.currentUser();
    if (!user || !user.id) {
      return { success: false, message: 'User not authenticated.' };
    }

    const change$ = this.http.post<void>(`${API_BASE_URL}/Auth/change-password`, {
      userId: user.id,
      currentPassword,
      newPassword
    }).pipe(
      map(() => ({ success: true, message: 'Password updated successfully!' })),
      catchError(err => of({ success: false, message: err.error?.message || 'Failed to change password.' }))
    );
    return lastValueFrom(change$);
  }

  private setToken(token: string) {
    if (isPlatformBrowser(this.platformId)) {
      localStorage.setItem(TOKEN_KEY, token);
    }
  }

  private clearToken() {
    if (isPlatformBrowser(this.platformId)) {
      localStorage.removeItem(TOKEN_KEY);
    }
  }
}
