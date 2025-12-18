import { Component, ChangeDetectionStrategy, output, inject, computed } from '@angular/core';
import { TaskService } from '../../services/task.service';
import { Task } from '../../models/task.model';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-task-list',
  templateUrl: './task-list.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule],
})
export class TaskListComponent {
  private taskService = inject(TaskService);

  readonly tasks = this.taskService.tasks;
  
  readonly stats = computed(() => {
    const tasks = this.tasks();
    const pending = tasks.filter(t => t.status === 'Pending').length;
    const overdue = tasks.filter(t => t.status === 'Pending' && new Date(t.dueDate) < new Date()).length;
    const total = tasks.length;
    return { pending, overdue, total };
  });

  readonly viewTask = output<Task>();
  readonly addTask = output<void>();

  completeTask(id: number) {
    this.taskService.completeTask(id);
  }

  getStatusClass(status: Task['status']): string {
    switch (status) {
      case 'Pending': return 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/50 dark:text-yellow-300';
      case 'Completed': return 'bg-green-100 text-green-800 dark:bg-green-900/50 dark:text-green-300';
      default: return 'bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-200';
    }
  }

  isOverdue(dueDate: string, status: Task['status']): boolean {
    return status === 'Pending' && new Date(dueDate) < new Date();
  }
}