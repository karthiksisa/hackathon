import { Component, ChangeDetectionStrategy, input, output, signal, effect, inject, computed } from '@angular/core';
import { Task, TaskType, RelatedEntityType } from '../../models/task.model';
import { TaskService } from '../../services/task.service';
import { LeadService } from '../../services/lead.service';
import { AccountService } from '../../services/account.service';
import { OpportunityService } from '../../services/opportunity.service';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-task-detail',
  templateUrl: './task-detail.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, CommonModule]
})
export class TaskDetailComponent {
  task = input<Task | null>();
  backToList = output<void>();

  private taskService = inject(TaskService);
  readonly leadService = inject(LeadService);
  readonly accountService = inject(AccountService);
  readonly opportunityService = inject(OpportunityService);

  readonly isNewTask = signal(false);
  readonly editableTask = signal<Partial<Task>>({});

  readonly taskTypes: TaskType[] = ['Call', 'Email', 'Follow-up', 'Meeting', 'Other'];
  readonly relatedEntityTypes: RelatedEntityType[] = ['Lead', 'Account', 'Opportunity'];
  readonly currentUser = inject(TaskService)['authService'].currentUser; // Access authService via TaskService dependency or inject directly

  readonly canEdit = computed(() => {
    const user = this.currentUser();
    const task = this.editableTask();
    if (!user) return false;
    if (user.role === 'Super Admin' || user.role === 'Regional Lead') return true;
    if (this.isNewTask()) return true; // Can always edit a new task being created
    return task.assignedToId === user.id;
  });

  readonly canDelete = computed(() => {
    const user = this.currentUser();
    const task = this.editableTask();
    if (!user) return false;
    if (user.role === 'Super Admin' || user.role === 'Regional Lead') return true;
    return !this.isNewTask() && task.assignedToId === user.id;
  });

  constructor() {
    effect(() => {
      const currentTask = this.task();
      const defaults = this.taskService.newTaskDefaults();

      if (currentTask) {
        this.isNewTask.set(false);
        this.editableTask.set({ ...currentTask });
      } else {
        this.isNewTask.set(true);
        const firstLead = this.leadService.leads()[0];
        this.editableTask.set({
          subject: '',
          dueDate: new Date().toISOString().split('T')[0],
          type: 'Call',
          relatedToType: defaults?.relatedToType ?? 'Lead',
          relatedToId: defaults?.relatedToId ?? firstLead?.id,
          relatedToName: defaults?.relatedToName ?? firstLead?.name,
        });

        if (defaults) {
          this.taskService.newTaskDefaults.set(null);
        }
      }
    });
  }

  updateField(field: keyof Task, value: any) {
    this.editableTask.update(task => ({ ...task, [field]: value }));
  }

  onSave() {
    this.taskService.saveTask(this.editableTask());
    this.backToList.emit();
  }

  onDelete() {
    const taskToDelete = this.editableTask();
    if (taskToDelete && taskToDelete.id) {
      if (confirm(`Are you sure you want to delete this task?`)) {
        this.taskService.deleteTask(taskToDelete.id);
        this.backToList.emit();
      }
    }
  }

  onEntityTypeChange(type: RelatedEntityType) {
    this.editableTask.update(task => ({
      ...task,
      relatedToType: type,
      relatedToId: undefined, // Reset selection
      relatedToName: '',
    }));
  }

  onEntityChange(id: number) {
    const type = this.editableTask().relatedToType;
    let entity;
    if (type === 'Lead') {
      entity = this.leadService.leads().find(e => e.id === +id);
    } else if (type === 'Account') {
      entity = this.accountService.accounts().find(e => e.id === +id);
    } else {
      entity = this.opportunityService.opportunities().find(e => e.id === +id);
    }

    this.editableTask.update(task => ({
      ...task,
      relatedToId: entity?.id,
      relatedToName: entity?.name,
    }));
  }
}
