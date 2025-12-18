import { Component, ChangeDetectionStrategy, input, output, signal, inject, effect, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Lead } from '../../models/lead.model';
import { LeadService } from '../../services/lead.service';
import { RegionService } from '../../services/region.service';
import { UserService } from '../../services/user.service';

@Component({
  selector: 'app-lead-convert',
  templateUrl: './lead-convert.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule, FormsModule],
})
export class LeadConvertComponent {
  lead = input.required<Lead>();
  backToList = output<void>();
  
  private leadService = inject(LeadService);
  private regionService = inject(RegionService);
  private userService = inject(UserService);

  readonly regions = this.regionService.regions;
  readonly salesReps = computed(() => this.userService.users().filter(u => u.role === 'Sales Rep'));
  
  readonly conversionData = signal({
    accountName: '',
    regionId: 0,
    salesRepId: 0
  });

  constructor() {
    effect(() => {
      const currentLead = this.lead();
      this.conversionData.set({
        accountName: currentLead.company,
        regionId: this.regions()[0]?.id,
        salesRepId: this.salesReps()[0]?.id,
      });
    });
  }

  onConvert() {
    const payload = {
      leadId: this.lead().id,
      ...this.conversionData(),
    };
    
    this.leadService.convertLead(payload).subscribe(() => {
      // After conversion, the lead is gone. The account/contact will appear
      // on their respective pages after a fresh data load, which happens on service init.
      // For now, just go back to the list. A more advanced implementation might
      // optimistically update the account/contact services.
      this.backToList.emit();
    });
  }
}
