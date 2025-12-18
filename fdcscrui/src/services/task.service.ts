import { Injectable, signal, inject, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Task, RelatedEntityType, TaskType, TaskStatus } from '../models/task.model';
import { AuthService } from './auth.service';
import { AccountService } from './account.service';
import { OpportunityService } from './opportunity.service';
import { LeadService } from './lead.service';
import { API_BASE_URL } from '../config';

// From swagger.json
interface CrmTaskDTO {
  id: number;
  subject: string;
  dueDate: string;
  type: TaskType;
  status: TaskStatus;
  relatedEntityType: RelatedEntityType;
  relatedEntityId: number;
  assignedToId?: number;
  completedAt?: string;
  notes?: string;
}

@Injectable({ providedIn: 'root' })
export class TaskService {
  private http = inject(HttpClient);
  private authService = inject(AuthService);
  private accountService = inject(AccountService);
  private opportunityService = inject(OpportunityService);
  private leadService = inject(LeadService);

  private tasksState = signal<CrmTaskDTO[]>([]);
  newTaskDefaults = signal<{ relatedToType: RelatedEntityType; relatedToId: number; relatedToName: string; } | null>(null);

  constructor() {
    this.loadTasks();
  }

  private loadTasks() {
    this.http.get<CrmTaskDTO[]>(`${API_BASE_URL}/Tasks`).subscribe(tasks => {
      this.tasksState.set(tasks);
    });
  }

  private allTasks = computed(() => {
    const dtos = this.tasksState();
    const leads = this.leadService.leads();
    const accounts = this.accountService.accounts();
    const opportunities = this.opportunityService.opportunities();

    return dtos.map((dto): Task => {
      let relatedToName = 'N/A';
      switch (dto.relatedEntityType) {
        case 'Lead':
          relatedToName = leads.find(e => e.id === dto.relatedEntityId)?.name ?? 'Unknown Lead';
          break;
        case 'Account':
          relatedToName = accounts.find(e => e.id === dto.relatedEntityId)?.name ?? 'Unknown Account';
          break;
        case 'Opportunity':
          relatedToName = opportunities.find(e => e.id === dto.relatedEntityId)?.name ?? 'Unknown Opportunity';
          break;
      }
      // FIX: Manually construct the Task object to match the interface, mapping DTO properties correctly.
      return {
        id: dto.id,
        subject: dto.subject,
        dueDate: dto.dueDate,
        type: dto.type,
        status: dto.status,
        relatedToType: dto.relatedEntityType,
        relatedToId: dto.relatedEntityId,
        relatedToName,
        assignedToId: dto.assignedToId,
      };
    });
  });

  tasks = computed(() => {
    const user = this.authService.currentUser();
    if (!user) return [];
    // Filtering logic can be enhanced based on roles later
    return this.allTasks().filter(task => {
      if (task.relatedToType === 'Lead') {
        return this.leadService.leads().some(l => l.id === task.relatedToId);
      } else if (task.relatedToType === 'Account') {
        return this.accountService.accounts().some(a => a.id === task.relatedToId);
      } else if (task.relatedToType === 'Opportunity') {
        return this.opportunityService.opportunities().some(o => o.id === task.relatedToId);
      }
      return true; // Fallback for unrelated or other types if any
    });
  });

  saveTask(task: Partial<Task>) {
    if (task.id) {
      // UpdateTaskRequest
      const payload = {
        subject: task.subject,
        status: task.status,
        dueDate: task.dueDate,
        assignedToId: task.assignedToId,
        notes: (task as any).notes // Assuming notes matches model/dto if we add it
      };
      this.http.put<void>(`${API_BASE_URL}/Tasks/${task.id}`, payload).subscribe(() => {
        this.tasksState.update(tasks =>
          tasks.map(t => t.id === task.id ? { ...t, ...task } as CrmTaskDTO : t)
        );
      });
    } else {
      // CreateTaskRequest
      const payload = {
        subject: task.subject,
        dueDate: task.dueDate,
        type: task.type,
        relatedEntityType: task.relatedToType,
        relatedEntityId: task.relatedToId,
        assignedToId: task.assignedToId,
        notes: (task as any).notes
      };
      this.http.post<CrmTaskDTO>(`${API_BASE_URL}/Tasks`, payload).subscribe(newTask => {
        this.tasksState.update(tasks => [...tasks, newTask]);
      });
    }
  }

  completeTask(id: number) {
    this.http.post<void>(`${API_BASE_URL}/Tasks/${id}/complete`, {}).subscribe(() => {
      this.tasksState.update(tasks =>
        tasks.map(t => t.id === id ? { ...t, status: 'Completed' } : t)
      );
    });
  }

  deleteTask(id: number) {
    this.http.delete<void>(`${API_BASE_URL}/Tasks/${id}`).subscribe(() => {
      this.tasksState.update(tasks => tasks.filter(t => t.id !== id));
    });
  }
}
