export type UserRole = 'Super Admin' | 'Regional Lead' | 'Sales Rep';

export interface User {
  id: number;
  name: string;
  email: string;
  role: UserRole;
  // A sales rep belongs to one region
  // A regional lead can manage multiple regions
  regionId?: number;
  regionIds?: number[];
  addressLine1?: string;
  addressLine2?: string;
  city?: string;
  state?: string;
  zipCode?: string;
  panNumber?: string;
  mobileNumber?: string;
  password?: string;
}