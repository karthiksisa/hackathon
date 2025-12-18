export type TaskType = 'Call' | 'Email' | 'Follow-up' | 'Meeting' | 'Other';
export type TaskStatus = 'Pending' | 'Completed';
export type RelatedEntityType = 'Lead' | 'Account' | 'Opportunity';

export interface Task {
  id: number;
  subject: string;
  dueDate: string;
  type: TaskType;
  status: TaskStatus;
  assignedToId?: number;
  relatedToType: RelatedEntityType;
  relatedToId: number;
  relatedToName: string; // Denormalized for display
}
