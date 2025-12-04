import { api } from './api';
import type { NlpQueryRequest, NlpQueryResponse, NlpQueryHistoryItem } from '../types';

export const nlpQueryService = {
  processQuery: (request: NlpQueryRequest): Promise<NlpQueryResponse> => {
    return api.post<NlpQueryResponse>('/NlpQuery', request);
  },

  getHistory: (limit: number = 20): Promise<NlpQueryHistoryItem[]> => {
    return api.get<NlpQueryHistoryItem[]>(`/NlpQuery/history?limit=${limit}`);
  },

  getSuggestions: (): Promise<string[]> => {
    return api.get<string[]>('/NlpQuery/suggestions');
  },

  exportToCsv: async (request: NlpQueryRequest): Promise<Blob> => {
    const response = await fetch('/api/NlpQuery/export/csv', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
      },
      body: JSON.stringify(request),
    });

    if (!response.ok) {
      const error = await response.text();
      throw new Error(error || `HTTP error! status: ${response.status}`);
    }

    return response.blob();
  },
};
