import { apiClient } from './client';
import { CreateEventRequest, EventItem, EventQueryParameters, PagedResult, UpdateEventRequest } from '../types';

export const eventsApi = {
  getPublishedEvents: async (params: EventQueryParameters = {}): Promise<PagedResult<EventItem>> => {
    const searchParams = new URLSearchParams();
    if (params.searchTerm) searchParams.append('searchTerm', params.searchTerm);
    if (params.fromDate) searchParams.append('fromDate', params.fromDate);
    if (params.toDate) searchParams.append('toDate', params.toDate);
    if (params.hasSpots !== undefined) searchParams.append('hasSpots', String(params.hasSpots));
    if (params.sortBy) searchParams.append('sortBy', params.sortBy);
    if (params.sortDescending !== undefined) searchParams.append('sortDescending', String(params.sortDescending));
    if (params.page) searchParams.append('page', String(params.page));
    if (params.pageSize) searchParams.append('pageSize', String(params.pageSize));

    const qs = searchParams.toString();
    return apiClient<PagedResult<EventItem>>(`/api/events${qs ? `?${qs}` : ''}`);
  },

  getEventById: async (id: string): Promise<EventItem> => {
    return apiClient<EventItem>(`/api/events/${id}`);
  },

  getMyEvents: async (): Promise<EventItem[]> => {
    return apiClient<EventItem[]>('/api/events/mine');
  },

  createEvent: async (data: CreateEventRequest): Promise<EventItem> => {
    return apiClient<EventItem>('/api/events', {
      method: 'POST',
      body: JSON.stringify(data),
    });
  },

  updateEvent: async (id: string, data: UpdateEventRequest): Promise<EventItem> => {
    return apiClient<EventItem>(`/api/events/${id}`, {
      method: 'PUT',
      body: JSON.stringify(data),
    });
  },

  deleteEvent: async (id: string): Promise<void> => {
    return apiClient<void>(`/api/events/${id}`, {
      method: 'DELETE',
    });
  },

  publishEvent: async (id: string): Promise<EventItem> => {
    return apiClient<EventItem>(`/api/events/${id}/publish`, {
      method: 'POST',
    });
  },

  cancelEvent: async (id: string): Promise<EventItem> => {
    return apiClient<EventItem>(`/api/events/${id}/cancel`, {
      method: 'POST',
    });
  },
};
