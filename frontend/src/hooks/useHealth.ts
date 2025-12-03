import { useQuery } from '@tanstack/react-query';
import { healthService } from '../services/reportService';

export function useHealth() {
  return useQuery({
    queryKey: ['health'],
    queryFn: healthService.check,
    refetchInterval: 30000, // Refresh every 30 seconds
    retry: 1,
  });
}
