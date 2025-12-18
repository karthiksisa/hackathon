import { Component, ChangeDetectionStrategy, input, output, signal, effect, inject, computed } from '@angular/core';
import { Opportunity, OpportunityStage } from '../../models/opportunity.model';
import { OpportunityService } from '../../services/opportunity.service';
import { AccountService } from '../../services/account.service';
import { TaskService } from '../../services/task.service';
import { DocumentService } from '../../services/document.service';
import { AuthService } from '../../services/auth.service';
import { UserService } from '../../services/user.service';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';


@Component({
  selector: 'app-opportunity-detail',
  templateUrl: './opportunity-detail.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, CommonModule]
})
export class OpportunityDetailComponent {
  opportunity = input<Opportunity | null>();
  backToBoard = output<void>();

  private opportunityService = inject(OpportunityService);
  private accountService = inject(AccountService);
  private taskService = inject(TaskService);
  private documentService = inject(DocumentService);
  private authService = inject(AuthService);
  private userService = inject(UserService);

  readonly visibleAccounts = this.accountService.accounts;
  readonly isNewOpportunity = signal(false);
  readonly currentUser = this.authService.currentUser;

  readonly editableOpportunity = signal<Partial<Opportunity>>({});

  readonly stages: OpportunityStage[] = ['Prospecting', 'New', 'Qualified', 'Proposal', 'Negotiation', 'Closed Won', 'Closed Lost'];

  readonly canEditOwner = computed(() => {
    const role = this.currentUser()?.role;
    return role === 'Super Admin' || role === 'Regional Lead';
  });

  readonly isClosed = computed(() => {
    const stage = this.editableOpportunity()?.stage;
    return stage === 'Closed Won' || stage === 'Closed Lost';
  });

  readonly possibleOwners = computed(() => {
    const user = this.currentUser();
    const allUsers = this.userService.users();
    if (!user) return [];

    const salesReps = allUsers.filter(u => u.role === 'Sales Rep');
    if (user.role === 'Super Admin') {
      return salesReps;
    }
    if (user.role === 'Regional Lead') {
      return salesReps.filter(rep => rep.regionId && user.regionIds?.includes(rep.regionId));
    }
    return [];
  });

  readonly opportunityTasks = computed(() => {
    const currentOpp = this.opportunity();
    if (!currentOpp) return [];
    return this.taskService.tasks().filter(t => t.relatedToType === 'Opportunity' && t.relatedToId === currentOpp.id);
  });

  readonly opportunityDocuments = computed(() => {
    const currentOpp = this.opportunity();
    if (!currentOpp) return [];
    return this.documentService.documents().filter(d => d.relatedToType === 'Opportunity' && d.relatedToId === currentOpp.id);
  });

  constructor() {
    effect(() => {
      const currentOpportunity = this.opportunity();
      const user = this.currentUser();
      const defaults = this.opportunityService.newOpportunityDefaults();

      if (currentOpportunity) {
        this.isNewOpportunity.set(false);
        this.editableOpportunity.set({ ...currentOpportunity });
      } else {
        this.isNewOpportunity.set(true);
        const account = defaults ? { id: defaults.accountId, name: defaults.accountName } : this.visibleAccounts()[0];

        this.editableOpportunity.set({
          name: '',
          accountId: account?.id,
          accountName: account?.name,
          stage: 'New',
          amount: 0,
          closeDate: new Date().toISOString().split('T')[0],
          ownerId: user?.id,
        });

        // Clear defaults after use
        if (defaults) {
          this.opportunityService.newOpportunityDefaults.set(null);
        }
      }
    });
  }

  onSave() {
    this.opportunityService.saveOpportunity(this.editableOpportunity());
    this.backToBoard.emit();
  }

  onDelete() {
    const oppToDelete = this.editableOpportunity();
    if (oppToDelete && oppToDelete.id) {
      if (confirm(`Are you sure you want to delete opportunity: ${oppToDelete.name}?`)) {
        this.opportunityService.deleteOpportunity(oppToDelete.id);
        this.backToBoard.emit();
      }
    }
  }

  onMarkWon() {
    const opp = this.editableOpportunity();
    if (opp && opp.id) {
      this.opportunityService.markAsWon(opp.id);
      this.backToBoard.emit();
    }
  }

  onMarkLost() {
    const opp = this.editableOpportunity();
    if (opp && opp.id) {
      this.opportunityService.markAsLost(opp.id);
      this.backToBoard.emit();
    }
  }

  updateField(field: keyof Opportunity, value: any) {
    this.editableOpportunity.update(opp => ({ ...opp, [field]: value }));
  }

  onAccountChange(accountId: number) {
    const selectedAccount = this.visibleAccounts().find(a => a.id === +accountId);
    this.editableOpportunity.update(opp => ({
      ...opp,
      accountId: selectedAccount?.id,
      accountName: selectedAccount?.name
    }));
  }

  getOwnerName(ownerId?: number): string {
    if (!ownerId) return 'N/A';
    return this.userService.users().find(u => u.id === ownerId)?.name ?? 'Unknown';
  }
}
