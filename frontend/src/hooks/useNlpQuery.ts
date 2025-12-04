import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { nlpQueryService } from '../services/nlpQueryService';
import type { NlpQueryRequest, NlpQueryResponse, NlpQueryHistoryItem } from '../types';

export function useNlpQuery() {
  const queryClient = useQueryClient();

  const mutation = useMutation<NlpQueryResponse, Error, NlpQueryRequest>({
    mutationFn: nlpQueryService.processQuery,
    onSuccess: () => {
      // Invalidate history cache on successful query
      queryClient.invalidateQueries({ queryKey: ['nlpQueryHistory'] });
    },
  });

  return {
    processQuery: mutation.mutate,
    processQueryAsync: mutation.mutateAsync,
    isLoading: mutation.isPending,
    error: mutation.error,
    data: mutation.data,
    reset: mutation.reset,
  };
}

export function useNlpQueryHistory(limit: number = 20) {
  return useQuery<NlpQueryHistoryItem[], Error>({
    queryKey: ['nlpQueryHistory', limit],
    queryFn: () => nlpQueryService.getHistory(limit),
    staleTime: 1000 * 60, // 1 minute
  });
}

export function useNlpQuerySuggestions() {
  return useQuery<string[], Error>({
    queryKey: ['nlpQuerySuggestions'],
    queryFn: () => nlpQueryService.getSuggestions(),
    staleTime: 1000 * 60 * 5, // 5 minutes
  });
}

export function useExportToCsv() {
  return useMutation<Blob, Error, NlpQueryRequest>({
    mutationFn: nlpQueryService.exportToCsv,
    onSuccess: (blob) => {
      // Trigger download
      const url = window.URL.createObjectURL(blob);
      const link = document.createElement('a');
      link.href = url;
      link.download = `query-results-${new Date().toISOString().slice(0, 19).replace(/:/g, '-')}.csv`;
      document.body.appendChild(link);
      link.click();
      document.body.removeChild(link);
      window.URL.revokeObjectURL(url);
    },
  });
}
