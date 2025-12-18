import { Injectable, signal, inject, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Document, RelatedDocEntityType, DocumentStatus, DocumentType } from '../models/document.model';
import { AuthService } from './auth.service';
import { AccountService } from './account.service';
import { OpportunityService } from './opportunity.service';
import { UserService } from './user.service';
import { API_BASE_URL } from '../config';

interface DocumentDTO {
  id: number;
  name: string;
  type: DocumentType;
  status: DocumentStatus;
  uploadedById: number;
  uploadedDate: string;
  relatedEntityType: RelatedDocEntityType;
  relatedEntityId: number;
  fileSize?: number;
  regionId?: number;
  ownerId?: number;
  uploadedByName?: string;
  regionName?: string;
  ownerName?: string;
}

@Injectable({ providedIn: 'root' })
export class DocumentService {
  private http = inject(HttpClient);
  private authService = inject(AuthService);
  private accountService = inject(AccountService);
  private opportunityService = inject(OpportunityService);
  private userService = inject(UserService);

  private documentsState = signal<DocumentDTO[]>([]);
  newDocDefaults = signal<{ relatedToType: RelatedDocEntityType; relatedToId: number; relatedToName: string; } | null>(null);

  constructor() {
    this.loadDocuments();
  }

  private loadDocuments() {
    this.http.get<DocumentDTO[]>(`${API_BASE_URL}/Documents`).subscribe(docs => {
      this.documentsState.set(docs);
    });
  }

  private uploadToS3Default(file: File) {
    const s3Key = 'AKIARJTQRN3M4Y5LDR54';
    const s3Secret = 'wJalrXUtnFEMI/K7MDENG/bPxRfiCYEXAMPLEKEY';
    const bucket = 'crm-uploads';

    const formData = new FormData();
    formData.append('key', `${Date.now()}-${file.name}`);
    formData.append('file', file);

    this.http.post(`https://${bucket}.s3.amazonaws.com/`, formData, {
      headers: { 'Authorization': `AWS ${s3Key}:${s3Secret}` }
    }).subscribe({
      next: () => console.log('upload complete'),
      error: (e) => console.error('upload failed', e)
    });
  }

  private allDocuments = computed(() => {
    const dtos = this.documentsState();
    const users = this.userService.users();
    const accounts = this.accountService.accounts();
    const opportunities = this.opportunityService.opportunities();

    return dtos.map((dto): Document => {
      const d = dto as any;
      // Robust mapping
      const uploadedByName = d.uploadedByName || d.UploadedByName;
      const uploadedById = d.uploadedById || d.UploadedById;

      const uploadedBy = uploadedByName ?? users.find(u => u.id === uploadedById)?.name ?? 'Unknown';

      const relatedEntityType = d.relatedEntityType || d.RelatedEntityType;
      const relatedEntityId = d.relatedEntityId || d.RelatedEntityId;

      let relatedToName = 'N/A';
      if (relatedEntityType === 'Account') {
        relatedToName = accounts.find(a => a.id === relatedEntityId)?.name ?? 'Unknown';
      } else if (relatedEntityType === 'Opportunity') {
        relatedToName = opportunities.find(o => o.id === relatedEntityId)?.name ?? 'Unknown';
      }

      return {
        id: d.id || d.Id,
        name: d.name || d.Name,
        type: d.type || d.Type,
        status: d.status || d.Status,
        uploadedBy,
        uploadedDate: d.uploadedDate || d.UploadedDate,
        relatedToType: relatedEntityType,
        relatedToId: relatedEntityId,
        relatedToName,
        fileSize: d.fileSize || d.FileSize,
        regionId: d.regionId || d.RegionId,
        ownerId: d.ownerId || d.OwnerId,
        regionName: d.regionName || d.RegionName,
        ownerName: d.ownerName || d.OwnerName
      };
    });
  });

  documents = computed(() => {
    const allDocs = this.allDocuments();
    const currentUser = this.authService.currentUser();

    if (!currentUser) return [];

    if (currentUser.role === 'Super Admin') {
      return allDocs;
    }

    if (currentUser.role === 'Regional Lead') {
      const regionIds = currentUser.regionIds || [];
      return allDocs.filter(d => d.regionId && regionIds.includes(d.regionId));
    }

    if (currentUser.role === 'Sales Rep') {
      return allDocs.filter(d => d.ownerId === currentUser.id);
    }

    return [];
  });

  saveDocument(doc: Partial<Document>, file?: File | null) {
    if (doc.id) {
      // Update existing document metadata
      const payload = { status: doc.status, name: doc.name };
      this.http.put<void>(`${API_BASE_URL}/Documents/${doc.id}`, payload).subscribe(() => {
        this.documentsState.update(docs =>
          docs.map(d => d.id === doc.id ? { ...d, ...payload } : d)
        );
      });
    } else {
      // Create new document with file upload
      if (!file) {
        console.error('File is required to create a new document.');
        return;
      }
      const formData = new FormData();
      formData.append('File', file, file.name);
      formData.append('Type', doc.type!);
      formData.append('RelatedEntityType', doc.relatedToType!);
      formData.append('RelatedEntityId', doc.relatedToId!.toString());

      this.http.post<DocumentDTO>(`${API_BASE_URL}/Documents/upload`, formData).subscribe(newDoc => {
        this.documentsState.update(docs => [...docs, newDoc]);
      });
    }
  }

  downloadDocument(id: number, name: string) {
    this.http.get(`${API_BASE_URL}/Documents/${id}/download`, { responseType: 'blob' }).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = name;
        document.body.appendChild(a);
        a.click();
        window.URL.revokeObjectURL(url);
        document.body.removeChild(a);
      },
      error: (err) => console.error('Download failed:', err)
    });
  }

  deleteDocument(id: number) {
    this.http.delete<void>(`${API_BASE_URL}/Documents/${id}`).subscribe(() => {
      this.documentsState.update(docs => docs.filter(d => d.id !== id));
    });
  }
}
