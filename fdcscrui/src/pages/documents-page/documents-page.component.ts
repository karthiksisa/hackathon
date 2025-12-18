import { Component, ChangeDetectionStrategy, signal } from '@angular/core';
import { Document } from '../../models/document.model';
import { DocumentListComponent } from '../../components/document-list/document-list.component';
import { DocumentUploadComponent } from '../../components/document-upload/document-upload.component';

@Component({
  selector: 'app-documents-page',
  templateUrl: './documents-page.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DocumentListComponent, DocumentUploadComponent],
})
export class DocumentsPageComponent {
  readonly view = signal<'list' | 'upload'>('list');
  readonly selectedDocument = signal<Document | null>(null);

  onViewDocument(doc: Document) {
    this.selectedDocument.set(doc);
    this.view.set('upload');
  }

  onAddDocument() {
    this.selectedDocument.set(null);
    this.view.set('upload');
  }

  onBackToList() {
    this.selectedDocument.set(null);
    this.view.set('list');
  }
}
