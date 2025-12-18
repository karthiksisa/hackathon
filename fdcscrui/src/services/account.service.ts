import { Injectable, signal, computed, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Account } from '../models/account.model';
import { AuthService } from './auth.service';
import { API_BASE_URL } from '../config';
import { UserService } from './user.service';
import { RegionService } from './region.service';

// The DTO from the backend API, which may differ from the frontend model.
interface AccountDTO {
  id: number;
  name: string;
  regionId: number;
  salesRepId?: number;
  industry: string;
  status: 'Active' | 'Prospect' | 'Inactive' | 'Pending Approval';
  createdDate: string;
  salesRepName?: string;
  regionName?: string;
}

@Injectable({ providedIn: 'root' })
export class AccountService {
  private http = inject(HttpClient);
  private authService = inject(AuthService);
  private userService = inject(UserService);
  private regionService = inject(RegionService);

  // Private signal to hold the raw DTO data from the backend
  private accountsState = signal<AccountDTO[]>([]);

  // Computed signal to transform DTOs into the frontend Account model
  private allAccounts = computed(() => {
    const dtos = this.accountsState();
    const users = this.userService.users();
    const regions = this.regionService.regions();

    // ALLOWED: Map even if users/regions are not fully loaded yet.
    // if (users.length === 0 || regions.length === 0) {
    //   return [];
    // }

    return dtos.map((dto): Account => {
      const d = dto as any;
      const salesRepNameRaw = d.salesRepName || d.SalesRepName;
      const salesRepId = d.salesRepId || d.SalesRepId;
      const ownerName = salesRepNameRaw ?? users.find(u => u.id === salesRepId)?.name ?? 'Unassigned';

      const regionNameRaw = d.regionName || d.RegionName;
      const regionId = d.regionId || d.RegionId;
      const regionName = regionNameRaw ?? regions.find(r => r.id === regionId)?.name ?? 'N/A';

      return { ...dto, owner: ownerName, region: regionName };
    });
  });

  constructor() {
    this.loadAccounts();
  }

  private loadAccounts() {
    this.http.get<AccountDTO[]>(`${API_BASE_URL}/Accounts`).subscribe(accounts => {
      this.accountsState.set(accounts);
    });
  }

  // Public computed signal filtered by the current user's role and permissions
  accounts = computed(() => {
    const currentUser = this.authService.currentUser();
    if (!currentUser) return [];

    switch (currentUser.role) {
      case 'Super Admin':
        return this.allAccounts();
      case 'Regional Lead':
        return this.allAccounts().filter(a => currentUser.regionIds?.includes(a.regionId));
      case 'Sales Rep':
        return this.allAccounts().filter(a => a.salesRepId === currentUser.id);
      default:
        return [];
    }
  });

  saveAccount(account: Partial<Account>) {
    if (account.id) {
      const updatePayload = {
        name: account.name,
        status: account.status,
        industry: account.industry,
        salesRepId: account.salesRepId,
      };
      this.http.put<void>(`${API_BASE_URL}/Accounts/${account.id}`, updatePayload).subscribe(() => {
        this.accountsState.update(accounts =>
          accounts.map(a => a.id === account.id ? { ...a, ...updatePayload } as AccountDTO : a)
        );
      });
    } else {
      // API does not accept status on creation. Backend defaults to 'Pending Approval' for Sales Reps.
      const createPayload = {
        name: account.name,
        regionId: account.regionId,
        salesRepId: account.salesRepId,
        industry: account.industry,
      };
      this.http.post<AccountDTO>(`${API_BASE_URL}/Accounts`, createPayload).subscribe(newAccount => {
        this.accountsState.update(accounts => [...accounts, newAccount]);
      });
    }
  }

  deleteAccount(id: number) {
    this.http.delete<void>(`${API_BASE_URL}/Accounts/${id}`).subscribe(() => {
      this.accountsState.update(accounts => accounts.filter(a => a.id !== id));
    });
  }

  approveAccount(id: number) {
    this.http.post<void>(`${API_BASE_URL}/Accounts/${id}/approve`, {}).subscribe(() => {
      this.accountsState.update(accounts =>
        accounts.map(a => a.id === id ? { ...a, status: 'Active' } : a)
      );
    });
  }

  rejectAccount(id: number) {
    const payload = { accountId: id, reason: 'Rejected from CRM UI' };
    this.http.post<void>(`${API_BASE_URL}/Accounts/${id}/reject`, payload).subscribe(() => {
      // On successful rejection, the backend deletes the account
      this.accountsState.update(accounts => accounts.filter(a => a.id !== id));
    });
  }
}
