import { Component, ChangeDetectionStrategy, input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Kpis } from '../../../models/dashboard.model';

@Component({
  selector: 'app-kpi-cards',
  templateUrl: './kpi-cards.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule],
})
export class KpiCardsComponent {
  kpis = input.required<Kpis | null>();
}