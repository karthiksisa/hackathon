import { Injectable, signal, inject, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Opportunity, OpportunityStage } from '../models/opportunity.model';
import { AuthService } from './auth.service';
import { AccountService } from './account.service';
import { API_BASE_URL } from '../config';

interface OpportunityDTO {
  id: number;
  name: string;
  accountId: number;
  accountName: string;
  stage: OpportunityStage;
  amount: number;
  closeDate: string;
  ownerId: number;
  ownerName?: string;
  regionId?: number;
  regionName?: string;
  accountOwnerName?: string;
}

@Injectable({ providedIn: 'root' })
export class OpportunityService {
  private http = inject(HttpClient);
  private authService = inject(AuthService);
  private accountService = inject(AccountService);

  private opportunitiesState = signal<Opportunity[]>([]);
  private allOpportunities = this.opportunitiesState.asReadonly();

  newOpportunityDefaults = signal<{ accountId: number; accountName: string; } | null>(null);

  constructor() {
    this.loadOpportunities();
  }

  private loadOpportunities() {
    this.http.get<OpportunityDTO[]>(`${API_BASE_URL}/opportunities`).subscribe({
      next: (dtos) => {
        const opportunities: Opportunity[] = dtos.map(dto => {
          const d = dto as any;
          return {
            ...dto,
            // Robust mapping for PascalCase/camelCase
            ownerName: d.ownerName || d.OwnerName,
            accountName: d.accountName || d.AccountName,
            regionName: d.regionName || d.RegionName,
            accountOwnerName: d.accountOwnerName || d.AccountOwnerName,
            regionId: d.regionId || d.RegionId,
            ownerId: d.ownerId || d.OwnerId
          } as Opportunity;
        });
        this.opportunitiesState.set(opportunities);
      },
      error: (err) => console.error('Failed to load opportunities:', err),
    });
  }

  opportunities = computed(() => {
    const user = this.authService.currentUser();
    if (!user) return [];

    const allOpps = this.allOpportunities();

    switch (user.role) {
      case 'Super Admin':
        return allOpps;
      case 'Regional Lead': {
        const regionIds = user.regionIds || [];
        return allOpps.filter(o => o.regionId && regionIds.includes(o.regionId));
      }
      case 'Sales Rep': {
        // Sales Reps see opportunities they own OR opportunities in accounts they own (fallback logic)
        // Using visible accounts from AccountService ensures consistency
        const visibleAccounts = this.accountService.accounts();
        const visibleAccountIds = new Set(visibleAccounts.map(a => a.id));
        return allOpps.filter(o => o.ownerId === user.id || visibleAccountIds.has(o.accountId));
      }
      default:
        return [];
    }
  });

  saveOpportunity(opportunity: Partial<Opportunity>) {
    if (opportunity.id) {
      // UpdateOpportunityRequest
      const payload = {
        name: opportunity.name,
        stage: opportunity.stage,
        amount: opportunity.amount,
        closeDate: opportunity.closeDate,
        ownerId: opportunity.ownerId // FIXED: Include ownerId in update
      };
      this.http.put<void>(`${API_BASE_URL}/Opportunities/${opportunity.id}`, payload).subscribe(() => {
        this.opportunitiesState.update(opportunities =>
          opportunities.map(o => o.id === opportunity.id ? { ...o, ...opportunity } as Opportunity : o)
        );
      });
    } else {
      // CreateOpportunityRequest
      const payload = {
        name: opportunity.name,
        accountId: opportunity.accountId,
        amount: opportunity.amount,
        closeDate: opportunity.closeDate,
        ownerId: opportunity.ownerId
      };
      this.http.post<Opportunity>(`${API_BASE_URL}/Opportunities`, payload).subscribe(newOpp => {
        this.opportunitiesState.update(opportunities => [...opportunities, newOpp]);
      });
    }
  }

  moveOpportunity(id: number, newStage: string) {
    const payload = { opportunityId: id, newStage };
    console.log('moveOpportunity called:', payload);

    // OPTIMISTIC UPDATE: Update local state immediately
    const previousState = this.opportunitiesState();
    this.opportunitiesState.update(opportunities =>
      opportunities.map(o => o.id === id ? { ...o, stage: newStage as any } : o)
    );

    const revert = (err: any) => {
      console.error('Backend rejected move, reverting:', err);
      this.opportunitiesState.set(previousState);
      alert(`Failed to move opportunity: ${err.error?.message || err.message || 'Unknown error'}`);
    };

    if (newStage === 'Closed Won') {
      this.http.post<void>(`${API_BASE_URL}/Opportunities/${id}/win`, { opportunityId: id }).subscribe({
        next: () => console.log('Backend accepted WIN'),
        error: revert
      });
    } else if (newStage === 'Closed Lost') {
      const lostPayload = { opportunityId: id, reason: 'Moved to Closed Lost in Kanban' };
      this.http.post<void>(`${API_BASE_URL}/Opportunities/${id}/lose`, lostPayload).subscribe({
        next: () => console.log('Backend accepted LOSE'),
        error: revert
      });
    } else {
      this.http.post<void>(`${API_BASE_URL}/Opportunities/${id}/move-stage`, payload).subscribe({
        next: () => console.log('Backend accepted move'),
        error: revert
      });
    }
  }

  deleteOpportunity(id: number) {
    this.http.delete(`${API_BASE_URL}/Opportunities/${id}`).subscribe(() => {
      this.opportunitiesState.update(opportunities => opportunities.filter(o => o.id !== id));
    });
  }

  markAsWon(opportunityId: number) {
    this.http.post<void>(`${API_BASE_URL}/Opportunities/${opportunityId}/win`, { opportunityId }).subscribe(() => {
      this.opportunitiesState.update(opportunities =>
        opportunities.map(o => o.id === opportunityId ? { ...o, stage: 'Closed Won', wonAt: new Date().toISOString() } : o)
      );
    });
  }

  markAsLost(opportunityId: number) {
    // LoseOpportunityRequest - UI doesn't provide reason, sending default.
    const payload = { opportunityId, reason: 'Marked lost from generic UI action' };
    this.http.post<void>(`${API_BASE_URL}/Opportunities/${opportunityId}/lose`, payload).subscribe(() => {
      this.opportunitiesState.update(opportunities =>
        opportunities.map(o => o.id === opportunityId ? { ...o, stage: 'Closed Lost', lostAt: new Date().toISOString() } : o)
      );
    });
  }
}
