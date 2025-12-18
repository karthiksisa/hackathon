import { Injectable, signal, inject, Injector } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { User } from '../models/user.model';
import { AuthService } from './auth.service';
import { API_BASE_URL } from '../config';
import { tap } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class UserService {
  private http = inject(HttpClient);
  private injector = inject(Injector);
  private usersState = signal<User[]>([]);

  users = this.usersState.asReadonly();

  constructor() {
    this.loadUsers();
  }

  loadUsers() {
    this.http.get<User[]>(`${API_BASE_URL}/Users`).subscribe(users => {
      const mappedUsers = users.map(u => {
        const d = u as any;
        return {
          ...u,
          regionId: d.regionId || d.RegionId,
          regionIds: d.regionIds || d.RegionIds,
          role: d.role || d.Role,
          name: d.name || d.Name,
          email: d.email || d.Email
        } as User;
      });
      this.usersState.set(mappedUsers);
      this.usersState.set(mappedUsers);
    });
  }

  getUserRegions(userId: number) {
    return this.http.get<number[]>(`${API_BASE_URL}/Users/${userId}/regions`);
  }

  saveUser(user: Partial<User>) {
    const authService = this.injector.get(AuthService);
    const currentUser = authService.currentUser();
    if (!currentUser) return;

    if (user.id) {
      // UpdateUserRequest
      const payload = {
        name: user.name,
        mobileNumber: user.mobileNumber,
        city: user.city,
        state: user.state,
        regionId: user.regionId,
        role: user.role
      };
      this.http.put<void>(`${API_BASE_URL}/Users/${user.id}`, payload).subscribe(() => {
        let updatedUser: User | null = null;
        this.usersState.update(users =>
          users.map(u => {
            if (u.id === user.id) {
              updatedUser = { ...u, ...user } as User;
              return updatedUser;
            }
            return u;
          })
        );

        if (currentUser.id === user.id && updatedUser) {
          authService.currentUser.set(updatedUser);
        }

        // Handle regionIds for Regional Lead
        if (user.role === 'Regional Lead' && user.regionIds) {
          this.http.post<void>(`${API_BASE_URL}/Users/${user.id}/regions`, { userId: user.id, regionIds: user.regionIds }).subscribe();
        }
      });
    } else {
      // CreateUserRequest
      const payload = {
        name: user.name,
        email: user.email,
        password: user.password || 'Password123!',
        role: user.role,
        regionId: user.regionId,
        mobileNumber: user.mobileNumber
      };
      this.http.post<User>(`${API_BASE_URL}/Users`, payload).pipe(
        tap(newUser => {
          this.usersState.update(users => [...users, newUser]);

          // Handle regionIds for Regional Lead
          if (newUser.role === 'Regional Lead' && user.regionIds) {
            this.http.post<void>(`${API_BASE_URL}/Users/${newUser.id}/regions`, { userId: newUser.id, regionIds: user.regionIds }).subscribe();
          }
        })
      ).subscribe();
    }
  }

  deleteUser(id: number) {
    this.http.delete<void>(`${API_BASE_URL}/Users/${id}`).subscribe(() => {
      this.usersState.update(users => users.filter(u => u.id !== id));
    });
  }
}
