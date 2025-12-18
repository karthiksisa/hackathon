import { Component, ChangeDetectionStrategy, signal } from '@angular/core';
import { Lead } from '../../models/lead.model';
import { LeadListComponent } from '../../components/lead-list/lead-list.component';
import { LeadDetailComponent } from '../../components/lead-detail/lead-detail.component';
import { LeadConvertComponent } from '../../components/lead-convert/lead-convert.component';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-leads-page',
  templateUrl: './leads-page.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, LeadListComponent, LeadDetailComponent, LeadConvertComponent],
})
export class LeadsPageComponent {
  readonly view = signal<'list' | 'detail' | 'convert'>('list');
  readonly selectedLead = signal<Lead | null>(null);

  onViewLead(lead: Lead) {
    this.selectedLead.set(lead);
    this.view.set('detail');
  }

  onAddLead() {
    this.selectedLead.set(null);
    this.view.set('detail');
  }
  
  onStartConvert(lead: Lead) {
    this.selectedLead.set(lead);
    this.view.set('convert');
  }

  onBackToList() {
    this.selectedLead.set(null);
    this.view.set('list');
  }
}