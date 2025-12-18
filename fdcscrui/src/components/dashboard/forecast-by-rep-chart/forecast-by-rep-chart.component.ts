import { Component, ChangeDetectionStrategy, input, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ForecastByRep, Scope } from '../../../models/dashboard.model';

@Component({
  selector: 'app-forecast-by-rep-chart',
  templateUrl: './forecast-by-rep-chart.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule],
})
export class ForecastByRepChartComponent {
  forecast = input.required<ForecastByRep[] | null>();
  scope = input.required<Scope | null>();

  readonly chartData = computed(() => {
    const data = this.forecast();
    if (!data || data.length === 0) {
      return { bars: [], maxAmount: 0 };
    }
    const maxAmount = Math.max(...data.map(rep => rep.inProgressValue + rep.stalledValue));
    
    const bars = data.map(rep => {
      const total = rep.inProgressValue + rep.stalledValue;
      return {
        ...rep,
        total,
        inProgressPercent: total > 0 ? (rep.inProgressValue / total) * 100 : 0,
        stalledPercent: total > 0 ? (rep.stalledValue / total) * 100 : 0,
        heightPercent: maxAmount > 0 ? (total / maxAmount) * 100 : 0,
      };
    });

    return { bars, maxAmount };
  });

  readonly title = computed(() => {
    const userScope = this.scope();
    switch(userScope?.role) {
      case 'Super Admin': return 'Forecast by Rep (All Regions)';
      case 'Regional Lead': return `Forecast by Rep (${userScope.regionName})`;
      case 'Sales Rep': return 'My Forecast';
      default: return 'Forecast';
    }
  });
}