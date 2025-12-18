import { Component, ChangeDetectionStrategy, input, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { StalledDeal, Scope } from '../../../models/dashboard.model';

@Component({
  selector: 'app-stalled-deals-table',
  templateUrl: './stalled-deals-table.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule],
})
export class StalledDealsTableComponent {
  deals = input.required<StalledDeal[] | null>();
  scope = input.required<Scope | null>();

  readonly title = computed(() => {
    const userScope = this.scope();
    switch (userScope?.role) {
      case 'Super Admin': return 'Top Stalled Deals (Company-wide)';
      case 'Regional Lead': return `Top Stalled Deals (${userScope.regionName})`;
      case 'Sales Rep': return 'My Stalled Deals';
      default: return 'Stalled Deals';
    }
  });
}