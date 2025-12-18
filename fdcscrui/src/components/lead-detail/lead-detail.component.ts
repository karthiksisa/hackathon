import { Component, ChangeDetectionStrategy, input, output, signal, effect, inject, computed } from '@angular/core';
import { Lead, LeadStatus } from '../../models/lead.model';
import { LeadService } from '../../services/lead.service';
import { TaskService } from '../../services/task.service';
import { AuthService } from '../../services/auth.service';
import { UserService } from '../../services/user.service';
import { RegionService } from '../../services/region.service';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-lead-detail',
  templateUrl: './lead-detail.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, CommonModule]
})
export class LeadDetailComponent {
  lead = input<Lead | null>();
  backToList = output<void>();
  startConvert = output<Lead>();

  private leadService = inject(LeadService);
  private taskService = inject(TaskService);
  private authService = inject(AuthService);
  private userService = inject(UserService);
  private regionService = inject(RegionService);

  readonly isNewLead = signal(false);
  readonly currentUser = this.authService.currentUser;
  readonly regions = this.regionService.regions;

  readonly editableLead = signal<Partial<Lead>>({});

  readonly statuses: LeadStatus[] = ['New', 'Contacted', 'Qualified', 'Disqualified', 'Nurture'];

  readonly canDelete = computed(() => {
    return false; // Hidden for all users as per request
  });

  readonly canConvert = computed(() => {
    return this.currentUser()?.role === 'Super Admin';
  });

  readonly isConverted = computed(() => {
    return this.editableLead().status === 'Converted';
  });

  readonly canEditOwner = computed(() => {
    const role = this.currentUser()?.role;
    return (role === 'Super Admin' || role === 'Regional Lead') && !this.isConverted();
  });

  readonly possibleOwners = computed(() => {
    const user = this.currentUser();
    const allUsers = this.userService.users();
    const currentLeadRegionId = this.editableLead().regionId;

    if (!user) return [];

    const salesReps = allUsers.filter(u => u.role === 'Sales Rep');

    // If a region is selected in the form, filter reps by that region
    if (currentLeadRegionId) {
      return salesReps.filter(rep => rep.regionId === currentLeadRegionId);
    }

    // Default fallbacks if no region selected yet (though UI should enforce it)
    if (user.role === 'Super Admin') {
      return salesReps;
    }
    if (user.role === 'Regional Lead') {
      return salesReps.filter(rep => rep.regionId === user.regionId);
    }

    return [];
  });

  readonly leadTasks = computed(() => {
    const currentLead = this.lead();
    if (!currentLead) return [];
    return this.taskService.tasks().filter(t => t.relatedToType === 'Lead' && t.relatedToId === currentLead.id);
  });

  constructor() {
    effect(() => {
      const currentLead = this.lead();
      const user = this.currentUser();
      if (currentLead) {
        this.isNewLead.set(false);
        this.editableLead.set({ ...currentLead });
      } else {
        this.isNewLead.set(true);
        this.editableLead.set({
          name: '',
          company: '',
          email: '',
          phone: '',
          status: 'New',
          ownerId: undefined, // Explicitly undefined to force selection
          regionId: user?.regionId, // Default to user's region
          source: 'Manual Entry'
        });
      }
    });
  }

  onSave() {
    this.leadService.saveLead(this.editableLead()).subscribe(() => {
      this.backToList.emit();
    });
  }

  onDelete() {
    const leadToDelete = this.editableLead();
    if (leadToDelete && leadToDelete.id) {
      if (confirm(`Are you sure you want to delete lead: ${leadToDelete.name}?`)) {
        this.leadService.deleteLead(leadToDelete.id).subscribe({
          next: () => {
            this.backToList.emit();
          },
          error: (err) => {
            alert('Failed to delete lead. Please try again.');
            console.error('Delete lead failed', err);
          }
        });
      }
    }
  }

  updateField(field: keyof Lead, value: string | number) {
    this.editableLead.update(lead => ({ ...lead, [field]: value }));
  }

  getTaskTypeIcon(type: string): string {
    switch (type) {
      case 'Call': return 'M16 3H7a2 2 0 00-2 2v14a2 2 0 002 2h9a2 2 0 002-2V5a2 2 0 00-2-2zM12 21a3 3 0 110-6 3 3 0 010 6z';
      case 'Email': return 'M3 8l7.89 5.26a2 2 0 002.22 0L21 8M5 19h14a2 2 0 002-2V7a2 2 0 00-2-2H5a2 2 0 00-2 2v10a2 2 0 002 2z';
      case 'Meeting': return 'M12 12a2 2 0 100-4 2 2 0 000 4z M17 12a5 5 0 11-10 0 5 5 0 0110 0z';
      default: return 'M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2m-3 7h3m-3 4h3m-6-4h.01M9 16h.01';
    }
  }
}