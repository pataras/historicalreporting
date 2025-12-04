import { useQuery } from '@tanstack/react-query';
import { organisationService } from '../services/organisationService';

export const organisationKeys = {
  all: ['organisations'] as const,
};

export function useOrganisations() {
  return useQuery({
    queryKey: organisationKeys.all,
    queryFn: organisationService.getAll,
  });
}
