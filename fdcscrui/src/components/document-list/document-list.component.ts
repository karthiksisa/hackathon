import { Component, ChangeDetectionStrategy, output, inject, computed } from '@angular/core';
import { DocumentService } from '../../services/document.service';
import { Document } from '../../models/document.model';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-document-list',
  templateUrl: './document-list.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [CommonModule],
})
export class DocumentListComponent {
  private documentService = inject(DocumentService);

  readonly documents = this.documentService.documents;

  readonly viewDocument = output<Document>();
  readonly addDocument = output<void>();

  getStatusClass(status: Document['status']): string {
    switch (status) {
      case 'Draft': return 'bg-gray-200 text-gray-800 dark:bg-gray-700 dark:text-gray-200';
      case 'Sent': return 'bg-blue-100 text-blue-800 dark:bg-blue-900/50 dark:text-blue-300';
      case 'Signed': return 'bg-green-100 text-green-800 dark:bg-green-900/50 dark:text-green-300';
      case 'Archived': return 'bg-indigo-100 text-indigo-800 dark:bg-indigo-900/50 dark:text-indigo-300';
      default: return 'bg-gray-100 text-gray-800 dark:bg-gray-700 dark:text-gray-200';
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

  onDownload(doc: Document) {
    if (doc.id) {
      this.documentService.downloadDocument(doc.id, doc.name);
    }
  }
}