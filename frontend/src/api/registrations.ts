import { apiClient } from './client';
import { PagedResult, Registration, RegistrationStatus, TicketDetails } from '../types';

export interface RegistrationFilters {
  status?: RegistrationStatus;
  searchTerm?: string;
  page?: number;
  pageSize?: number;
}

export const registrationsApi = {
  registerForEvent: async (eventId: string): Promise<Registration> => {
    return apiClient<Registration>(`/api/events/${eventId}/register`, {
      method: 'POST',
    });
  },

  cancelRegistration: async (eventId: string): Promise<Registration> => {
    return apiClient<Registration>(`/api/events/${eventId}/register`, {
      method: 'DELETE',
    });
  },

  getMyRegistrations: async (): Promise<Registration[]> => {
    return apiClient<Registration[]>('/api/me/registrations');
  },

  getEventRegistrations: async (
    eventId: string,
    filters: RegistrationFilters = {}
  ): Promise<PagedResult<Registration>> => {
    const searchParams = new URLSearchParams();
    if (filters.status) searchParams.append('status', filters.status);
    if (filters.searchTerm) searchParams.append('searchTerm', filters.searchTerm);
    if (filters.page) searchParams.append('page', String(filters.page));
    if (filters.pageSize) searchParams.append('pageSize', String(filters.pageSize));

    const qs = searchParams.toString();
    return apiClient<PagedResult<Registration>>(
      `/api/events/${eventId}/registrations${qs ? `?${qs}` : ''}`
    );
  },

  getTicketDetails: async (registrationId: string): Promise<TicketDetails> => {
    return apiClient<TicketDetails>(`/api/registrations/${registrationId}/ticket`);
  },
};
