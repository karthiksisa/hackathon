import { Component, ChangeDetectionStrategy, signal } from '@angular/core';
import { Task } from '../../models/task.model';
import { TaskListComponent } from '../../components/task-list/task-list.component';
import { TaskDetailComponent } from '../../components/task-detail/task-detail.component';

@Component({
  selector: 'app-tasks-page',
  templateUrl: './tasks-page.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TaskListComponent, TaskDetailComponent],
})
export class TasksPageComponent {
  readonly view = signal<'list' | 'detail'>('list');
  readonly selectedTask = signal<Task | null>(null);

  onViewTask(task: Task) {
    this.selectedTask.set(task);
    this.view.set('detail');
  }

  onAddTask() {
    this.selectedTask.set(null);
    this.view.set('detail');
  }

  onBackToList() {
    this.selectedTask.set(null);
    this.view.set('list');
  }
}
