export type OpportunityStage = 'Prospecting' | 'New' | 'Qualified' | 'Proposal' | 'Negotiation' | 'Closed Won' | 'Closed Lost';

export interface Opportunity {
  id: number;
  name: string;
  accountId: number;
  accountName: string; // Denormalized for easier display
  stage: OpportunityStage;
  amount: number;
  closeDate: string;
  ownerId: number;
  ownerName?: string;
  regionId?: number;
  regionName?: string;
  accountOwnerName?: string;
}
