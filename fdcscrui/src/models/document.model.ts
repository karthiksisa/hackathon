export type DocumentType = 'Proposal' | 'SOW';
export type DocumentStatus = 'Draft' | 'Sent' | 'Signed' | 'Archived';
export type RelatedDocEntityType = 'Account' | 'Opportunity';

export interface Document {
  id: number;
  name: string;
  type: DocumentType;
  status: DocumentStatus;
  uploadedBy: string;
  uploadedDate: string;
  relatedToType: RelatedDocEntityType;
  relatedToId: number;
  relatedToName: string; // Denormalized for display
  fileSize?: number; // size in bytes
  regionId?: number;
  regionName?: string;
  ownerId?: number;
  ownerName?: string;
}