import { Component, ChangeDetectionStrategy, signal } from '@angular/core';
import { Opportunity } from '../../models/opportunity.model';
import { OpportunityBoardComponent } from '../../components/opportunity-board/opportunity-board.component';
import { OpportunityDetailComponent } from '../../components/opportunity-detail/opportunity-detail.component';

@Component({
  selector: 'app-opportunities-page',
  templateUrl: './opportunities-page.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [OpportunityBoardComponent, OpportunityDetailComponent],
})
export class OpportunitiesPageComponent {
  readonly view = signal<'board' | 'detail'>('board');
  readonly selectedOpportunity = signal<Opportunity | null>(null);

  onViewOpportunity(opportunity: Opportunity) {
    this.selectedOpportunity.set(opportunity);
    this.view.set('detail');
  }

  onAddOpportunity() {
    this.selectedOpportunity.set(null);
    this.view.set('detail');
  }

  onBackToBoard() {
    this.selectedOpportunity.set(null);
    this.view.set('board');
  }
}
