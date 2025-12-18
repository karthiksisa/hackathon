import { Component, ChangeDetectionStrategy, input, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Funnel } from '../../../models/dashboard.model';

@Component({
  selector: 'app-funnel-chart',
  templateUrl: './funnel-chart.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule],
})
export class FunnelChartComponent {
  funnel = input.required<Funnel | null>();

  readonly processedFunnel = computed(() => {
    const funnelData = this.funnel();
    if (!funnelData || funnelData.stages.length === 0) {
      return { stages: [], maxValue: 0 };
    }
    const maxValue = Math.max(...funnelData.stages.map(s => s.amount));
    const stages = funnelData.stages.map(stage => ({
      ...stage,
      percentage: maxValue > 0 ? (stage.amount / maxValue) * 100 : 0,
    }));
    return { stages, maxValue };
  });
}