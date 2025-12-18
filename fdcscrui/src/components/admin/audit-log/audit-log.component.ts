import { Component, ChangeDetectionStrategy, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { AuditLogService } from '../../../services/audit-log.service';

@Component({
  selector: 'app-audit-log',
  templateUrl: './audit-log.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule],
  standalone: true,
})
export class AuditLogComponent {
  private auditLogService = inject(AuditLogService);
  readonly logs = this.auditLogService.logs;
}
