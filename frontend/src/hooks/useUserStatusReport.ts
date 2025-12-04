import { useQuery } from '@tanstack/react-query';
import { userStatusReportService } from '../services/userStatusReportService';

export const userStatusReportKeys = {
  monthly: (organisationId: string) => ['userStatusReport', 'monthly', organisationId] as const,
};

export function useMonthlyUserStatusReport(organisationId: string | null) {
  return useQuery({
    queryKey: userStatusReportKeys.monthly(organisationId ?? ''),
    queryFn: () => userStatusReportService.getMonthlyByOrganisation(organisationId!),
    enabled: !!organisationId,
  });
}
