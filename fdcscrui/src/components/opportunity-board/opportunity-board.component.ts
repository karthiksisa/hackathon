import { Component, ChangeDetectionStrategy, output, inject, computed } from '@angular/core';
import { OpportunityService } from '../../services/opportunity.service';
import { Opportunity, OpportunityStage } from '../../models/opportunity.model';
import { UserService } from '../../services/user.service';
import { CommonModule } from '@angular/common';
import { DragDropModule, CdkDragDrop, moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';

@Component({
  selector: 'app-opportunity-board',
  templateUrl: './opportunity-board.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, DragDropModule],
})
export class OpportunityBoardComponent {
  private opportunityService = inject(OpportunityService);
  private userService = inject(UserService);

  readonly stages: OpportunityStage[] = ['Prospecting', 'New', 'Qualified', 'Proposal', 'Negotiation', 'Closed Won', 'Closed Lost'];
  private readonly allUsers = this.userService.users;

  readonly opportunitiesByStage = computed(() => {
    const opportunities = this.opportunityService.opportunities();
    const grouped: { [key in OpportunityStage]?: Opportunity[] } = {};
    for (const stage of this.stages) {
      grouped[stage] = opportunities.filter(o => o.stage === stage);
    }
    return grouped;
  });

  readonly viewOpportunity = output<Opportunity>();
  readonly addOpportunity = output<void>();

  getOwnerName(ownerId: number): string {
    return this.allUsers().find(u => u.id === ownerId)?.name ?? 'Unknown';
  }

  getStageClass(stage: OpportunityStage): { border: string, bg: string } {
    switch (stage) {
      case 'Prospecting': return { border: 'border-t-teal-500', bg: 'bg-teal-50 dark:bg-gray-800/50' };
      case 'New': return { border: 'border-t-blue-500', bg: 'bg-blue-50 dark:bg-gray-800/50' };
      case 'Qualified': return { border: 'border-t-yellow-500', bg: 'bg-yellow-50 dark:bg-gray-800/50' };
      case 'Proposal': return { border: 'border-t-indigo-500', bg: 'bg-indigo-50 dark:bg-gray-800/50' };
      case 'Negotiation': return { border: 'border-t-purple-500', bg: 'bg-purple-50 dark:bg-gray-800/50' };
      case 'Closed Won': return { border: 'border-t-green-500', bg: 'bg-green-50 dark:bg-gray-800/50' };
      case 'Closed Lost': return { border: 'border-t-red-500', bg: 'bg-red-50 dark:bg-gray-800/50' };
      default: return { border: 'border-t-gray-500', bg: 'bg-gray-50 dark:bg-gray-800/50' };
    }
  }

  drop(event: CdkDragDrop<Opportunity[] | undefined>) {
    if (event.previousContainer === event.container) {
      // Reordering within the same column
      return;
    } else {
      // Moving to a new stage
      console.log('Drop event:', event);
      const item = event.item.data as Opportunity;
      const newStage = event.container.id as OpportunityStage;
      console.log(`Moving opportunity ${item.id} to ${newStage}`);
      this.opportunityService.moveOpportunity(item.id, newStage);
    }
  }

  entered(event: any) {
    console.log('Entered list:', event.container.id);
  }
}