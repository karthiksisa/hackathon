import { Component, ChangeDetectionStrategy, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { UserService } from '../../services/user.service';
import { RegionService } from '../../services/region.service';
import { User, UserRole } from '../../models/user.model';

@Component({
  selector: 'app-user-management',
  templateUrl: './user-management.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule],
})
export class UserManagementComponent {
  private userService = inject(UserService);
  private regionService = inject(RegionService);

  constructor() {
    this.userService.loadUsers();
  }

  readonly users = this.userService.users;
  readonly regions = this.regionService.regions;

  readonly isEditing = signal(false);
  readonly editableUser = signal<Partial<User>>({});

  readonly roles: UserRole[] = ['Super Admin', 'Regional Lead', 'Sales Rep'];

  startNew() {
    this.isEditing.set(true);
    this.editableUser.set({
      name: '',
      email: '',
      password: '',
      role: 'Sales Rep',
      regionId: this.regions()[0]?.id,
    });
  }

  startEdit(user: User) {
    this.isEditing.set(true);
    this.editableUser.set({ ...user });
  }

  cancelEdit() {
    this.isEditing.set(false);
    this.editableUser.set({});
  }

  saveUser() {
    const user = this.editableUser();
    if (user.role !== 'Regional Lead') {
      user.regionIds = undefined;
    }
    if (user.role !== 'Sales Rep') {
      user.regionId = undefined;
    }
    this.userService.saveUser(user);
    this.cancelEdit();
  }

  deleteUser(id: number) {
    if (confirm('Are you sure you want to delete this user?')) {
      this.userService.deleteUser(id);
    }
  }

  getRegionName(user: User): string {
    if (user.role === 'Regional Lead') {
      return user.regionIds?.map(id => this.regions().find(r => r.id === id)?.name).filter(Boolean).join(', ') || 'N/A';
    }
    if (user.role === 'Sales Rep') {
      return this.regions().find(r => r.id === user.regionId)?.name ?? 'N/A';
    }
    return 'N/A';
  }

  onRegionCheckboxChange(regionId: number, event: Event) {
    const isChecked = (event.target as HTMLInputElement).checked;
    this.editableUser.update(user => {
      const currentRegionIds = user.regionIds || [];
      if (isChecked) {
        return { ...user, regionIds: [...currentRegionIds, regionId] };
      } else {
        return { ...user, regionIds: currentRegionIds.filter(id => id !== regionId) };
      }
    });
  }

  isRegionSelected(regionId: number): boolean {
    return this.editableUser().regionIds?.includes(regionId) ?? false;
  }
}
