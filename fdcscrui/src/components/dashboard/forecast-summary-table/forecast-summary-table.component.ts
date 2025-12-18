import { Component, ChangeDetectionStrategy, input, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ForecastPivot } from '../../../models/dashboard.model';

@Component({
  selector: 'app-forecast-summary-table',
  templateUrl: './forecast-summary-table.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule],
})
export class ForecastSummaryTableComponent {
  pivotData = input.required<ForecastPivot | null>();
  
  readonly title = computed(() => {
    const mode = this.pivotData()?.mode;
    if (mode === 'RegionStage') return 'Pipeline by Region & Stage';
    if (mode === 'StageSummary') return 'My Pipeline by Stage';
    return 'Forecast Summary';
  });

  readonly columnLabels = computed(() => {
     const data = this.pivotData();
     if (!data) return [];
     return data.columnOrder.map(key => key.charAt(0).toUpperCase() + key.slice(1));
  });
}