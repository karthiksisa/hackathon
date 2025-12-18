import { Component, ChangeDetectionStrategy, input, output, signal, effect, inject, computed } from '@angular/core';
import { Account } from '../../models/account.model';
import { Contact } from '../../models/contact.model';
import { Opportunity, OpportunityStage } from '../../models/opportunity.model';
import { Task, TaskType } from '../../models/task.model';
import { Document, DocumentType, DocumentStatus } from '../../models/document.model';

import { AccountService } from '../../services/account.service';
import { ContactService } from '../../services/contact.service';
import { OpportunityService } from '../../services/opportunity.service';
import { TaskService } from '../../services/task.service';
import { DocumentService } from '../../services/document.service';
import { AuthService } from '../../services/auth.service';
import { UserService } from '../../services/user.service';
import { RegionService } from '../../services/region.service';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-account-detail',
  templateUrl: './account-detail.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, CommonModule]
})
export class AccountDetailComponent {
  account = input.required<Account | null>();
  backToList = output<void>();

  private accountService = inject(AccountService);
  private contactService = inject(ContactService);
  private opportunityService = inject(OpportunityService);
  private taskService = inject(TaskService);
  private documentService = inject(DocumentService);
  private authService = inject(AuthService);
  private userService = inject(UserService);
  private regionService = inject(RegionService);

  readonly currentUser = this.authService.currentUser;
  readonly allRegions = this.regionService.regions;
  readonly opportunityStages: OpportunityStage[] = ['New', 'Qualified', 'Proposal', 'Negotiation', 'Closed Won', 'Closed Lost'];
  readonly taskTypes: TaskType[] = ['Call', 'Email', 'Follow-up', 'Meeting', 'Other'];
  readonly docTypes: DocumentType[] = ['Proposal', 'SOW'];
  readonly docStatuses: DocumentStatus[] = ['Draft', 'Sent', 'Signed', 'Archived'];

  readonly isNewAccount = signal(false);
  readonly editableAccount = signal<Partial<Account>>({});
  readonly statuses: Account['status'][] = ['Active', 'Prospect', 'Inactive', 'Pending Approval'];

  readonly activeTab = signal<'overview' | 'contacts' | 'opportunities' | 'activities' | 'documents'>('overview');

  // State for inline forms
  readonly isAddingContact = signal(false);
  readonly newContact = signal<Partial<Contact>>({});
  readonly editingOpportunity = signal<Partial<Opportunity> | null>(null);
  readonly editingTask = signal<Partial<Task> | null>(null);
  readonly editingDocument = signal<Partial<Document> | null>(null);
  readonly selectedFile = signal<File | null>(null);

  readonly canEditDetails = computed(() => {
    const user = this.currentUser();
    const role = user?.role;
    return role === 'Super Admin' || role === 'Regional Lead';
  });

  readonly isStatusLocked = computed(() => {
    const user = this.currentUser();
    const account = this.editableAccount();
    if (user?.role === 'Super Admin') return false;
    if (account.status === 'Pending Approval') return true;
    if (user?.role === 'Sales Rep') return true;
    return false;
  });

  readonly isPendingApproval = computed(() => this.editableAccount().status === 'Pending Approval');

  readonly canApproveOrReject = computed(() => {
    if (!this.isPendingApproval()) return false;
    const role = this.currentUser()?.role;
    return role === 'Super Admin' || role === 'Regional Lead';
  });

  readonly salesReps = computed(() => this.userService.users().filter(u => u.role === 'Sales Rep'));

  readonly possibleOppOwners = computed(() => {
    const user = this.currentUser();
    const allUsers = this.userService.users();
    if (!user) return [];

    const salesReps = allUsers.filter(u => u.role === 'Sales Rep');
    if (user.role === 'Super Admin') {
      return salesReps;
    }
    if (user.role === 'Regional Lead') {
      return salesReps.filter(rep => rep.regionId && user.regionIds?.includes(rep.regionId));
    }
    return [];
  });

  readonly accountContacts = computed(() => this.contactService.contacts().filter(c => c.accountId === this.account()?.id));
  readonly accountOpportunities = computed(() => this.opportunityService.opportunities().filter(o => o.accountId === this.account()?.id));
  readonly accountTasks = computed(() => this.taskService.tasks().filter(t => t.relatedToType === 'Account' && t.relatedToId === this.account()?.id));
  readonly accountDocuments = computed(() => this.documentService.documents().filter(d => d.relatedToType === 'Account' && d.relatedToId === this.account()?.id));

  constructor() {
    effect(() => {
      const currentAccount = this.account();
      const user = this.currentUser();
      if (currentAccount) {
        this.isNewAccount.set(false);
        this.editableAccount.set({ ...currentAccount });
        this.activeTab.set('overview');
      } else {
        this.isNewAccount.set(true);
        const userRegion = this.allRegions().find(r => r.id === user?.regionId);
        this.editableAccount.set({
          name: '',
          region: userRegion?.name ?? '', regionId: userRegion?.id,
          owner: user?.name ?? 'Current User',
          salesRepId: user?.id,
          industry: '',
          status: user?.role === 'Sales Rep' ? 'Pending Approval' : 'Prospect',
        });
      }
    });
  }

  // Main Account Actions
  onSave() { this.accountService.saveAccount(this.editableAccount()); this.backToList.emit(); }
  onDelete() {
    const id = this.editableAccount().id;
    if (id && confirm(`Are you sure you want to delete account: ${this.editableAccount().name}?`)) {
      this.accountService.deleteAccount(id);
      this.backToList.emit();
    }
  }
  setTab(tab: 'overview' | 'contacts' | 'opportunities' | 'activities' | 'documents') { this.activeTab.set(tab); }

  onApprove() {
    const id = this.editableAccount().id;
    if (id) {
      this.accountService.approveAccount(id);
      this.backToList.emit();
    }
  }

  onReject() {
    const id = this.editableAccount().id;
    if (id && confirm(`Are you sure you want to reject and delete this account request for: ${this.editableAccount().name}?`)) {
      this.accountService.rejectAccount(id);
      this.backToList.emit();
    }
  }

  // Contact Actions
  startAddContact() { this.newContact.set({ accountId: this.account()!.id, name: '', email: '', phone: '', title: '' }); this.isAddingContact.set(true); }
  cancelAddContact() { this.isAddingContact.set(false); }
  saveNewContact() { this.contactService.saveContact(this.newContact()); this.isAddingContact.set(false); }

  // Opportunity Actions
  startAddOpportunity() {
    this.editingOpportunity.set({
      accountId: this.account()!.id,
      accountName: this.account()!.name,
      name: '', stage: 'New', amount: 0,
      closeDate: new Date().toISOString().split('T')[0],
      ownerId: this.currentUser()?.id,
    });
  }
  startEditOpportunity(opp: Opportunity) { this.editingOpportunity.set({ ...opp }); }
  cancelEditOpportunity() { this.editingOpportunity.set(null); }
  saveOpportunity() { this.opportunityService.saveOpportunity(this.editingOpportunity()!); this.editingOpportunity.set(null); }

  // Task Actions
  startAddTask() {
    this.editingTask.set({
      subject: '',
      type: 'Call',
      dueDate: new Date().toISOString().split('T')[0],
      relatedToType: 'Account',
      relatedToId: this.account()!.id,
      relatedToName: this.account()!.name,
    });
  }
  startEditTask(task: Task) { this.editingTask.set({ ...task }); }
  cancelEditTask() { this.editingTask.set(null); }
  saveTask() { this.taskService.saveTask(this.editingTask()!); this.editingTask.set(null); }

  // Document Actions
  startAddDocument() {
    this.editingDocument.set({
      name: '', type: 'Proposal', status: 'Draft',
      uploadedBy: this.currentUser()?.name,
      relatedToType: 'Account',
      relatedToId: this.account()!.id,
      relatedToName: this.account()!.name,
      fileSize: 0,
    });
    this.selectedFile.set(null);
  }
  startEditDocument(doc: Document) { this.editingDocument.set({ ...doc }); this.selectedFile.set(null); }
  cancelEditDocument() { this.editingDocument.set(null); this.selectedFile.set(null); }
  saveDocument() { this.documentService.saveDocument(this.editingDocument()!, this.selectedFile()); this.editingDocument.set(null); this.selectedFile.set(null); }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      const file = input.files[0];
      this.selectedFile.set(file);
      this.editingDocument.update(doc => ({
        ...doc,
        name: file.name,
        fileSize: file.size,
      }));
    }
  }

  // UI Helpers
  getOwnerName(ownerId: number): string { return this.userService.users().find(u => u.id === ownerId)?.name ?? 'Unknown'; }
  getStageClass(stage: string): string {
    return { 'New': 'bg-blue-100 text-blue-800', 'Qualified': 'bg-yellow-100 text-yellow-800', 'Proposal': 'bg-indigo-100 text-indigo-800', 'Negotiation': 'bg-purple-100 text-purple-800', 'Closed Won': 'bg-green-100 text-green-800', 'Closed Lost': 'bg-red-100 text-red-800' }[stage] || 'bg-gray-100 text-gray-800';
  }
  getTaskStatusClass(status: string): string {
    return { 'Completed': 'bg-green-100 text-green-800', 'Pending': 'bg-yellow-100 text-yellow-800' }[status] || 'bg-gray-100 text-gray-800';
  }
  formatFileSize(bytes?: number): string {
    if (!bytes) return '-';
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  }

  onDownload(doc: Document) {
    if (doc.id) {
      this.documentService.downloadDocument(doc.id, doc.name);
    }
  }
}