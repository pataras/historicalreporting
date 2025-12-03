export interface Report {
  id: string;
  name: string;
  description?: string;
  query: string;
  status: ReportStatus;
  createdAt: string;
  updatedAt?: string;
  createdBy?: string;
  updatedBy?: string;
}

export enum ReportStatus {
  Draft = 'Draft',
  Active = 'Active',
  Archived = 'Archived',
}

export interface HealthCheck {
  status: string;
  timestamp: string;
  version: string;
}

export interface ApiError {
  message: string;
  status: number;
}
