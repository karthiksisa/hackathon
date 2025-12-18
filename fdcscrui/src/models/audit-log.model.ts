export type AuditAction = 'Create' | 'Update' | 'Delete' | 'Convert' | 'Login' | 'Logout' | 'Complete' | 'Approve' | 'Reject';
export type EntityType = 'Lead' | 'Account' | 'Contact' | 'Opportunity' | 'Task' | 'Document' | 'User' | 'Region' | 'System';

export interface AuditLog {
  id: number;
  timestamp: string;
  userId: number;
  userName: string;
  action: AuditAction;
  entityType: EntityType;
  entityId?: number;
  entityName?: string;
  details: string;
}