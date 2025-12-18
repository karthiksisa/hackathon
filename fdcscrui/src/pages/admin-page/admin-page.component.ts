import { Component, ChangeDetectionStrategy, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { UserManagementComponent } from '../../components/user-management/user-management.component';
import { RegionManagementComponent } from '../../components/region-management/region-management.component';
import { AuditLogComponent } from '../../components/admin/audit-log/audit-log.component';

@Component({
  selector: 'app-admin-page',
  templateUrl: './admin-page.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, UserManagementComponent, RegionManagementComponent, AuditLogComponent],
})
export class AdminPageComponent {
  readonly activeTab = signal<'users' | 'regions' | 'audit'>('users');

  setTab(tab: 'users' | 'regions' | 'audit') {
    this.activeTab.set(tab);
  }
}
