import { useQuery } from '@tanstack/react-query';
import { managerService } from '../services/managerService';

export const managerKeys = {
  byOrganisation: (organisationId: string) => ['managers', 'organisation', organisationId] as const,
};

export function useManagersByOrganisation(organisationId: string | null) {
  return useQuery({
    queryKey: managerKeys.byOrganisation(organisationId ?? ''),
    queryFn: () => managerService.getByOrganisation(organisationId!),
    enabled: !!organisationId,
  });
}
