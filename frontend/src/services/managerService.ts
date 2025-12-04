import { api } from './api';
import type { Manager } from '../types';

export const managerService = {
  getByOrganisation: (organisationId: string) =>
    api.get<Manager[]>(`/managers/organisation/${organisationId}`),

  getById: (managerId: string) =>
    api.get<Manager>(`/managers/${managerId}`),
};
