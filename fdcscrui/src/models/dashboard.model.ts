import { UserRole } from './user.model';

export interface DashboardResponse {
  scope: Scope;
  filtersApplied: FiltersApplied;
  kpis: Kpis;
  funnel: Funnel;
  forecastByRep: ForecastByRep[];
  forecastPivot: ForecastPivot;
  stalledDeals: StalledDeal[];
}

export interface Scope {
  role: UserRole;
  regionId?: number;
  regionName?: string;
  userId: number;
  userName: string;
}

export interface FiltersApplied {
  dateFrom: string;
  dateTo: string;
  pipelineType: 'Open' | 'Won' | 'All';
  stalledDaysThreshold: number;
}

export interface Kpis {
  revenueWon: number;
  pipelineOpen: number;
  openDealsCount: number;
  winRate: number;
  avgSalesCycleDays: number;
  stalledDealsCount: number;
}

export interface Funnel {
  stages: FunnelStage[];
}

export interface FunnelStage {
  stageKey: string;
  stageLabel: string;
  count: number;
  amount: number;
}

export interface ForecastByRep {
  repId: number;
  repName: string;
  inProgressValue: number;
  stalledValue: number;
  openDealsCount: number;
}

export interface ForecastPivot {
  mode: 'RegionStage' | 'StageSummary';
  rows: PivotRow[];
  columnOrder: string[];
}

export interface PivotRow {
  rowKey: string;
  rowLabel: string;
  values: { [key: string]: number };
  counts: { [key: string]: number };
}

export interface StalledDeal {
  opportunityId: number;
  opportunityName: string;
  accountName: string;
  stageKey: string;
  stageLabel: string;
  value: number;
  daysInStage: number;
  lastActivityDate: string;
  ownerName: string;
}