import { Component, ChangeDetectionStrategy, input, output, signal, effect, inject } from '@angular/core';
import { Document, DocumentType, DocumentStatus, RelatedDocEntityType } from '../../models/document.model';
import { DocumentService } from '../../services/document.service';
import { AccountService } from '../../services/account.service';
import { OpportunityService } from '../../services/opportunity.service';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-document-upload',
  templateUrl: './document-upload.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [FormsModule, CommonModule]
})
export class DocumentUploadComponent {
  document = input<Document | null>();
  backToList = output<void>();

  private documentService = inject(DocumentService);
  readonly accountService = inject(AccountService);
  readonly opportunityService = inject(OpportunityService);

  readonly isNewDocument = signal(false);
  readonly editableDocument = signal<Partial<Document>>({});
  readonly selectedFile = signal<File | null>(null);
  
  readonly docTypes: DocumentType[] = ['Proposal', 'SOW'];
  readonly docStatuses: DocumentStatus[] = ['Draft', 'Sent', 'Signed', 'Archived'];
  readonly relatedEntityTypes: RelatedDocEntityType[] = ['Account', 'Opportunity'];

  constructor() {
    effect(() => {
      const currentDoc = this.document();
      const defaults = this.documentService.newDocDefaults();

      if (currentDoc) {
        this.isNewDocument.set(false);
        this.editableDocument.set({ ...currentDoc });
        this.selectedFile.set(null);
      } else {
        this.isNewDocument.set(true);
        const firstAccount = this.accountService.accounts()[0];
        this.editableDocument.set({
          name: '',
          type: 'Proposal',
          status: 'Draft',
          uploadedBy: 'Current User', // Placeholder
          relatedToType: defaults?.relatedToType ?? 'Account',
          relatedToId: defaults?.relatedToId ?? firstAccount?.id,
          relatedToName: defaults?.relatedToName ?? firstAccount?.name,
          fileSize: 0,
        });
        this.selectedFile.set(null);

        if (defaults) {
          this.documentService.newDocDefaults.set(null);
        }
      }
    });
  }

  updateField(field: keyof Document, value: any) {
    this.editableDocument.update(doc => ({ ...doc, [field]: value }));
  }

  onSave() {
    this.documentService.saveDocument(this.editableDocument(), this.selectedFile());
    this.backToList.emit();
  }

  onDelete() {
    const docToDelete = this.editableDocument();
    if (docToDelete && docToDelete.id) {
        if(confirm(`Are you sure you want to delete this document?`)) {
            this.documentService.deleteDocument(docToDelete.id);
            this.backToList.emit();
        }
    }
  }
  
  onEntityTypeChange(type: RelatedDocEntityType) {
    this.editableDocument.update(doc => ({
      ...doc,
      relatedToType: type,
      relatedToId: undefined, // Reset selection
      relatedToName: '',
    }));
  }

  onEntityChange(id: number) {
    const type = this.editableDocument().relatedToType;
    let entity;
    if (type === 'Account') {
      entity = this.accountService.accounts().find(e => e.id === +id);
    } else {
      entity = this.opportunityService.opportunities().find(e => e.id === +id);
    }

    this.editableDocument.update(doc => ({
      ...doc,
      relatedToId: entity?.id,
      relatedToName: entity?.name,
    }));
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
        const file = input.files[0];
        this.selectedFile.set(file);
        this.editableDocument.update(doc => ({
            ...doc,
            name: file.name,
            fileSize: file.size,
        }));
    }
  }

  formatFileSize(bytes?: number): string {
    if (!bytes) return '-';
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(2)) + ' ' + sizes[i];
  }
}
