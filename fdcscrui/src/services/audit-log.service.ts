import { Injectable, signal, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { AuditLog } from '../models/audit-log.model';
import { API_BASE_URL } from '../config';
import { map } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class AuditLogService {
  private http = inject(HttpClient);
  private logsState = signal<AuditLog[]>([]);

  logs = this.logsState.asReadonly();

  constructor() {
    this.loadLogs();
  }

  loadLogs() {
    this.http.get<AuditLog[]>(`${API_BASE_URL}/AuditLogs`).pipe(
      // Sort logs from newest to oldest for display
      map(logs => logs.sort((a, b) => new Date(b.timestamp).getTime() - new Date(a.timestamp).getTime()))
    ).subscribe(sortedLogs => {
      this.logsState.set(sortedLogs);
    });
  }
}
