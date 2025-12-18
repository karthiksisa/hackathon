import { Injectable, inject, signal } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { DashboardResponse } from '../models/dashboard.model';
import { API_BASE_URL } from '../config';
import { AuthService } from './auth.service';
import { catchError, shareReplay, switchMap, tap, map, startWith, filter } from 'rxjs/operators';
import { toSignal, toObservable } from '@angular/core/rxjs-interop';
import { of, Subject, combineLatest } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class DashboardService {
  private http = inject(HttpClient);

  // Triggers for refreshing data
  private refreshTrigger$ = new Subject<void>();

  // Current filters (could be expanded to a signal/state)
  private filters = signal({
    dateFrom: '',
    dateTo: '',
    pipelineType: 'All'
  });

  private authService = inject(AuthService);

  // Resource that fetches data whenever trigger fires or USER changes
  // using toSignal to convert Observable to Signal for the template
  readonly stats = toSignal(
    combineLatest([
      this.refreshTrigger$.pipe(startWith(void 0)),
      toObservable(this.authService.currentUser).pipe(filter(u => !!u)) // Only fetch if user logged in
    ]).pipe(
      // When either updates, fetch data
      switchMap(() => {
        let params = new HttpParams();
        const f = this.filters();
        if (f.dateFrom) params = params.set('dateFrom', f.dateFrom);
        if (f.dateTo) params = params.set('dateTo', f.dateTo);
        if (f.pipelineType) params = params.set('pipelineType', f.pipelineType);

        return this.http.get<DashboardResponse>(`${API_BASE_URL}/Dashboard`, { params }).pipe(
          tap(data => console.log('Dashboard stats loaded:', data)),
          map(data => {
            // Inject filters
            const currentFilters = this.filters();
            // @ts-ignore - injecting local state into response
            data.filtersApplied = {
              dateFrom: currentFilters.dateFrom || 'Start',
              dateTo: currentFilters.dateTo || 'End',
              pipelineType: currentFilters.pipelineType as 'All' | 'Open' | 'Won',
              stalledDaysThreshold: 30
            };

            // Adapt payload: ForecastByRep
            if (data.forecastByRep) {
              data.forecastByRep = data.forecastByRep.map((item: any) => ({
                ...item,
                repName: item.repName || item.label,
                repId: item.repId || 0
              }));
            }

            // Adapt payload: Funnel (API returns array, UI expects { stages: [] })
            if (Array.isArray(data.funnel)) {
              // @ts-ignore
              data.funnel = { stages: data.funnel };
            }

            // Adapt payload: ForecastPivot
            if (data.forecastPivot) {
              const mode = data.forecastPivot.mode;
              const rawRows: any[] = data.forecastPivot.rows || [];

              if (mode === 'RegionStage') {
                // Collect all unique stage names for columnOrder from 'columns' keys
                const stageSet = new Set<string>();
                rawRows.forEach(r => {
                  if (r.columns) {
                    Object.keys(r.columns).forEach(k => stageSet.add(k));
                  }
                });
                // Define a standard order or sort
                const standardStages = ['Prospecting', 'Qualification', 'Proposal', 'Negotiation', 'Closed Won'];
                data.forecastPivot.columnOrder = Array.from(stageSet).sort((a, b) => {
                  const ia = standardStages.indexOf(a);
                  const ib = standardStages.indexOf(b);
                  if (ia !== -1 && ib !== -1) return ia - ib;
                  if (ia !== -1) return -1;
                  if (ib !== -1) return 1;
                  return a.localeCompare(b);
                });

                // Map columns to values
                data.forecastPivot.rows = rawRows.map(r => ({
                  rowKey: r.rowLabel,
                  rowLabel: r.rowLabel,
                  values: r.columns || {},
                  counts: {}
                }));

              } else if (mode === 'StageSummary') {
                // Map columns TotalAmount/Count to values['total'] / counts['total']
                data.forecastPivot.rows = rawRows.map(r => ({
                  rowKey: r.rowLabel, // e.g. "Negotiation"
                  rowLabel: r.rowLabel,
                  values: { total: r.columns?.TotalAmount || 0 },
                  counts: { total: r.columns?.Count || 0 }
                }));
                data.forecastPivot.columnOrder = [];
              }
            }

            return data;
          }),
          catchError(err => {
            console.error('Dashboard stats fetch failed', err);
            // Return default empty structure to avoid stuck loading state
            // Use current filters in error state too
            const f = this.filters();
            return of({
              scope: { role: 'Error', userName: 'Error', userId: 0 },
              kpis: { revenueWon: 0, pipelineOpen: 0, openDealsCount: 0, winRate: 0, avgSalesCycleDays: 0, stalledDealsCount: 0 },
              funnel: { stages: [] },
              forecastByRep: [],
              forecastPivot: { mode: 'StageSummary', rows: [], columnOrder: [] },
              stalledDeals: [],
              filtersApplied: {
                dateFrom: f.dateFrom,
                dateTo: f.dateTo,
                pipelineType: f.pipelineType,
                stalledDaysThreshold: 30
              }
            } as unknown as DashboardResponse);
          })
        );
      })
      // shareReplay(1) // Removed intentionally to force refresh on new user
    ),
    { initialValue: null }
  );

  constructor() {
    // Initial load
    setTimeout(() => this.refreshTrigger$.next(), 0);
  }

  refresh() {
    this.refreshTrigger$.next();
  }
}