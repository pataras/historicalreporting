import { api } from './api';
import type { Organisation } from '../types';

export const organisationService = {
  getAll: () => api.get<Organisation[]>('/organisations'),
};
