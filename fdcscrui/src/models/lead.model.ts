export type LeadStatus = 'New' | 'Contacted' | 'Qualified' | 'Disqualified' | 'Nurture' | 'Converted';

export interface Lead {
  id: number;
  name: string;
  company: string;
  email: string;
  phone: string;
  status: LeadStatus;
  ownerId: number;
  ownerName?: string;
  regionId?: number;
  regionName?: string;
  createdDate: string;
  source: string;
  notes?: string;
}