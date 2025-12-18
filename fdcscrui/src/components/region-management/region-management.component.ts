import { Component, ChangeDetectionStrategy, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RegionService } from '../../services/region.service';
import { Region } from '../../models/region.model';

@Component({
  selector: 'app-region-management',
  templateUrl: './region-management.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule],
})
export class RegionManagementComponent {
  private regionService = inject(RegionService);

  readonly regions = this.regionService.regions;

  constructor() {
    this.regionService.loadRegions();
  }

  readonly isEditing = signal(false);
  readonly editableRegion = signal<Partial<Region>>({});

  startNew() {
    this.isEditing.set(true);
    this.editableRegion.set({ name: '' });
  }

  startEdit(region: Region) {
    this.isEditing.set(true);
    this.editableRegion.set({ ...region });
  }

  cancelEdit() {
    this.isEditing.set(false);
    this.editableRegion.set({});
  }

  saveRegion() {
    this.regionService.saveRegion(this.editableRegion());
    this.cancelEdit();
  }

  deleteRegion(id: number) {
    if (confirm('Are you sure you want to delete this region?')) {
      this.regionService.deleteRegion(id);
    }
  }
}
