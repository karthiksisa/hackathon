import { Component, ChangeDetectionStrategy, signal } from '@angular/core';
import { Account } from '../../models/account.model';
import { AccountListComponent } from '../../components/account-list/account-list.component';
import { AccountDetailComponent } from '../../components/account-detail/account-detail.component';

@Component({
  selector: 'app-accounts-page',
  templateUrl: './accounts-page.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [AccountListComponent, AccountDetailComponent],
})
export class AccountsPageComponent {
  readonly view = signal<'list' | 'detail'>('list');
  readonly selectedAccount = signal<Account | null>(null);

  onViewAccount(account: Account) {
    this.selectedAccount.set(account);
    this.view.set('detail');
  }

  onAddAccount() {
    this.selectedAccount.set(null);
    this.view.set('detail');
  }

  onBackToList() {
    this.selectedAccount.set(null);
    this.view.set('list');
  }
}
