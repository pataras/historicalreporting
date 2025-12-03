import { api } from './api';
import type { Report, HealthCheck } from '../types';

export const reportService = {
  getAll: () => api.get<Report[]>('/reports'),

  getById: (id: string) => api.get<Report>(`/reports/${id}`),

  create: (report: Omit<Report, 'id' | 'createdAt'>) =>
    api.post<Report>('/reports', report),

  update: (id: string, report: Partial<Report>) =>
    api.put<Report>(`/reports/${id}`, report),

  delete: (id: string) => api.delete<void>(`/reports/${id}`),
};

export const healthService = {
  check: () => api.get<HealthCheck>('/health'),
};
