export interface Account {
  id: number;
  name: string;
  region: string;
  regionId: number;
  owner: string; // Kept for display, but salesRepId is the source of truth
  salesRepId?: number;
  industry: string;
  status: 'Active' | 'Prospect' | 'Inactive' | 'Pending Approval';
  createdDate: string;
}