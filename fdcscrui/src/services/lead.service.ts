import { Injectable, signal, computed, inject, effect } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Lead } from '../models/lead.model';
import { AuthService } from './auth.service';
import { UserService } from './user.service';
import { API_BASE_URL } from '../config';
import { tap, switchMap, map } from 'rxjs/operators';
import { of } from 'rxjs';

interface LeadDTO {
  id: number;
  name: string;
  company: string;
  email: string;
  phone: string;
  status: any;
  ownerId: number;
  ownerName?: string;
  regionId?: number;
  regionName?: string;
  createdDate: string;
  source: string;
  notes?: string;
}

@Injectable({ providedIn: 'root' })
export class LeadService {
  private http = inject(HttpClient);
  private authService = inject(AuthService);
  private userService = inject(UserService);

  private leadsState = signal<LeadDTO[]>([]);

  // Computed signal to transform DTOs into the frontend Lead model
  private allLeads = computed(() => {
    const dtos = this.leadsState();
    const users = this.userService.users();

    const mappedLeads = dtos.map((dto): Lead => {
      // Handle potential PascalCase keys from backend
      const d = dto as any;

      const mappedOwnerName = d.ownerName || d.OwnerName;
      const mappedRegionName = d.regionName || d.RegionName;

      // START FIX: Derive regionId from owner if missing in DTO
      let regionId = d.regionId || d.RegionId;
      if (!regionId) {
        const ownerId = d.ownerId || d.OwnerId;
        const owner = users.find(u => u.id === ownerId);
        if (owner) {
          regionId = owner.regionId;
        }
      }
      // END FIX

      const lead = {
        ...dto,
        ownerName: mappedOwnerName,
        regionName: mappedRegionName,
        // Ensure standard fields are picked up if they differ in casing
        ownerId: d.ownerId || d.OwnerId,
        regionId: regionId,
      } as Lead;
      return lead;
    });

    // Filtering removed as per request to 'show all leads regional lead like admin'
    return mappedLeads;
  });

  constructor() {
    effect(() => {
      const user = this.authService.currentUser();
      if (user) {
        this.loadLeads();
      } else {
        this.leadsState.set([]);
      }
    });
  }

  private loadLeads() {
    const user = this.authService.currentUser();
    // Use switchMap to handle potential async region fetching
    const userObservable = of(user);

    userObservable.pipe(
      switchMap(u => {
        if (!u) return of([]);

        if (u.role === 'Sales Rep') {
          const params = `?ownerId=${u.id}`;
          return this.http.get<LeadDTO[]>(`${API_BASE_URL}/Leads${params}`);
        } else {
          // Super Admin, Regional Lead, or others - fetch all
          // Regional Lead filtering happens in 'allLeads' computed property to leverage regionId polyfill
          return this.http.get<LeadDTO[]>(`${API_BASE_URL}/Leads`);
        }
      })
    ).subscribe({
      next: (leads) => {
        this.leadsState.set(leads);
      },
      error: (err) => console.error('Failed to load leads:', err),
    });
  }

  leads = computed(() => {
    // Simply return the processed leads; rely on backend filtering
    return this.allLeads();
  });

  saveLead(lead: Partial<Lead>) {
    // Helper to cast Lead (partial or full) to LeadDTO for state update
    // In a real app we might fetch the fresh DTO, but here we merge.
    if (lead.id) {
      return this.http.put<Lead>(`${API_BASE_URL}/Leads/${lead.id}`, lead).pipe(
        tap(updatedLead => {
          this.leadsState.update(leads =>
            leads.map(l => {
              if (l.id === lead.id) {
                // Merge existing DTO (l) with updates.
                // We assume updatedLead from backend might be partial or full, but we prioritize our local known updates + server response
                return { ...l, ...updatedLead, ...lead } as LeadDTO;
              }
              return l;
            })
          );
        })
      );
    } else {
      return this.http.post<Lead>(`${API_BASE_URL}/Leads`, lead).pipe(
        tap(newLead => {
          // newLead is Lead, map to LeadDTO (compatible mostly)
          this.leadsState.update(leads => [...leads, newLead as unknown as LeadDTO]);
        })
      );
    }
  }

  deleteLead(id: number) {
    return this.http.delete(`${API_BASE_URL}/Leads/${id}`).pipe(
      tap(() => {
        this.leadsState.update(leads => leads.filter(l => l.id !== id));
      })
    );
  }

  convertLead(payload: { leadId: number; accountName: string; regionId: number; salesRepId: number; }) {
    return this.http.post<void>(`${API_BASE_URL}/Leads/${payload.leadId}/convert`, payload);
  }
}
