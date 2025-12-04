import { api } from './api';
import type { MonthlyUserStatusReportResult } from '../types';

export const userStatusReportService = {
  getMonthlyByOrganisation: (organisationId: string) =>
    api.get<MonthlyUserStatusReportResult>(`/userstatusreports/monthly/${organisationId}`),

  getMonthlyByManager: (organisationId: string, managerId: string) =>
    api.get<MonthlyUserStatusReportResult>(`/userstatusreports/monthly/${organisationId}/manager/${managerId}`),
};
