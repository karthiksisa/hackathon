import { Component, ChangeDetectionStrategy, output, inject, computed } from '@angular/core';
import { LeadService } from '../../services/lead.service';
import { Lead, LeadStatus } from '../../models/lead.model';
import { UserService } from '../../services/user.service';
import { AuthService } from '../../services/auth.service';

@Component({
  selector: 'app-lead-list',
  templateUrl: './lead-list.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LeadListComponent {
  private leadService = inject(LeadService);
  private userService = inject(UserService);
  private authService = inject(AuthService);

  readonly leads = this.leadService.leads;
  private readonly allUsers = this.userService.users;

  readonly canCreateLead = computed(() => {
    const role = this.authService.currentUser()?.role;
    return role === 'Super Admin' || role === 'Regional Lead';
  });

  readonly stats = computed(() => {
    const leads = this.leads();
    const newLeads = leads.filter(l => l.status === 'New').length;
    const qualifiedLeads = leads.filter(l => l.status === 'Qualified').length;
    const totalLeads = leads.length;
    return { newLeads, qualifiedLeads, totalLeads };
  });

  readonly viewLead = output<Lead>();
  readonly addLead = output<void>();

  getOwnerName(ownerId: number): string {
    return this.allUsers().find(u => u.id === ownerId)?.name ?? 'Unknown';
  }

  getStatusClass(status: LeadStatus): string {
    switch (status) {
      case 'New': return 'bg-blue-100 text-blue-800 dark:bg-blue-900/50 dark:text-blue-300';
      case 'Contacted': return 'bg-purple-100 text-purple-800 dark:bg-purple-900/50 dark:text-purple-300';
      case 'Qualified': return 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/50 dark:text-yellow-300';
      case 'Disqualified': return 'bg-red-100 text-red-800 dark:bg-red-900/50 dark:text-red-300';
      case 'Nurture': return 'bg-indigo-100 text-indigo-800 dark:bg-indigo-900/50 dark:text-indigo-300';
      case 'Converted': return 'bg-green-100 text-green-800 dark:bg-green-900/50 dark:text-green-300';
      default: return 'bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-200';
    }
  }
}