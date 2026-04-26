export enum UserRole {
  Customer = 0,
  Operator = 1,
  Admin = 2
}

export interface AuthResponse {
  token: string;
  fullName: string;
  email: string;
  role: UserRole;
  id: string;
  isApproved?: boolean;
  status?: string;
  rejectionReason?: string;
}

export interface RegisterRequest {
  fullName: string;
  email: string;
  phone: string;
  password: string;
  role: UserRole;
  companyName?: string;
  address?: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}
