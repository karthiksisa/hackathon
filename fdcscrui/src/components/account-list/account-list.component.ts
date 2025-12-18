import { Component, ChangeDetectionStrategy, output, inject, computed } from '@angular/core';
import { AccountService } from '../../services/account.service';
import { Account } from '../../models/account.model';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-account-list',
  templateUrl: './account-list.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AccountListComponent {
  private accountService = inject(AccountService);
  private authService = inject(AuthService);

  readonly accounts = this.accountService.accounts;
  readonly currentUser = this.authService.currentUser;

  readonly canCreateAccount = computed(() => {
    const role = this.currentUser()?.role;
    return role === 'Super Admin' || role === 'Regional Lead';
  });

  readonly canRequestAccount = computed(() => {
    return this.currentUser()?.role === 'Sales Rep';
  });
  
  readonly showApprovalQueue = computed(() => {
    const role = this.currentUser()?.role;
    return role === 'Super Admin' || role === 'Regional Lead';
  });

  readonly pendingAccounts = computed(() => {
    if (!this.showApprovalQueue()) return [];
    // The `this.accounts()` signal is already correctly filtered for the manager's scope.
    return this.accounts().filter(a => a.status === 'Pending Approval');
  });

  readonly stats = computed(() => {
    const accounts = this.accounts();
    const active = accounts.filter(a => a.status === 'Active').length;
    const prospect = accounts.filter(a => a.status === 'Prospect').length;
    const total = accounts.length;
    return { active, prospect, total };
  });

  readonly viewAccount = output<Account>();
  readonly addAccount = output<void>();

  getStatusClass(status: Account['status']): string {
    switch (status) {
      case 'Active': return 'bg-green-100 text-green-800 dark:bg-green-900/50 dark:text-green-300';
      case 'Prospect': return 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/50 dark:text-yellow-300';
      case 'Inactive': return 'bg-gray-200 text-gray-800 dark:bg-gray-700 dark:text-gray-200';
      case 'Pending Approval': return 'bg-orange-100 text-orange-800 dark:bg-orange-900/50 dark:text-orange-300';
      default: return 'bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-200';
    }
  }
}