import { useQuery } from '@tanstack/react-query';
import { userStatusReportService } from '../services/userStatusReportService';

export const userStatusReportKeys = {
  monthly: (organisationId: string) => ['userStatusReport', 'monthly', organisationId] as const,
  monthlyByManager: (organisationId: string, managerId: string) =>
    ['userStatusReport', 'monthly', organisationId, 'manager', managerId] as const,
};

export function useMonthlyUserStatusReport(organisationId: string | null) {
  return useQuery({
    queryKey: userStatusReportKeys.monthly(organisationId ?? ''),
    queryFn: () => userStatusReportService.getMonthlyByOrganisation(organisationId!),
    enabled: !!organisationId,
  });
}

export function useMonthlyUserStatusReportByManager(
  organisationId: string | null,
  managerId: string | null
) {
  return useQuery({
    queryKey: userStatusReportKeys.monthlyByManager(organisationId ?? '', managerId ?? ''),
    queryFn: () => userStatusReportService.getMonthlyByManager(organisationId!, managerId!),
    enabled: !!organisationId && !!managerId,
  });
}
