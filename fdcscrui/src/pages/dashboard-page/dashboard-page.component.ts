import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { DashboardService } from '../../services/dashboard.service';
import { KpiCardsComponent } from '../../components/dashboard/kpi-cards/kpi-cards.component';
import { FunnelChartComponent } from '../../components/dashboard/funnel-chart/funnel-chart.component';
import { ForecastByRepChartComponent } from '../../components/dashboard/forecast-by-rep-chart/forecast-by-rep-chart.component';
import { ForecastSummaryTableComponent } from '../../components/dashboard/forecast-summary-table/forecast-summary-table.component';
import { StalledDealsTableComponent } from '../../components/dashboard/stalled-deals-table/stalled-deals-table.component';

@Component({
  selector: 'app-dashboard-page',
  templateUrl: './dashboard-page.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule,
    KpiCardsComponent,
    FunnelChartComponent,
    ForecastByRepChartComponent,
    ForecastSummaryTableComponent,
    StalledDealsTableComponent,
  ],
})
export class DashboardPageComponent {
  private dashboardService = inject(DashboardService);
  readonly stats = this.dashboardService.stats;
}