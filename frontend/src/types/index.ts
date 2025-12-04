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

export const ReportStatus = {
  Draft: 'Draft',
  Active: 'Active',
  Archived: 'Archived',
} as const;

export type ReportStatus = typeof ReportStatus[keyof typeof ReportStatus];

export interface HealthCheck {
  status: string;
  timestamp: string;
  version: string;
}

export interface ApiError {
  message: string;
  status: number;
}

export interface MonthlyUserStatus {
  year: number;
  month: number;
  validCount: number;
  invalidCount: number;
  totalCount: number;
}

export interface MonthlyUserStatusReportResult {
  organisationId: string;
  organisationName: string;
  monthlyData: MonthlyUserStatus[];
}

export interface Organisation {
  id: string;
  name: string;
}
