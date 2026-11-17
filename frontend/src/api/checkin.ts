import { apiClient } from './client';
import { CheckInResponse, EventAttendanceStats } from '../types';

const BASE_URL = import.meta.env.VITE_API_URL || '';

export const checkInApi = {
  checkInTicket: async (eventId: string, ticketCode: string): Promise<CheckInResponse> => {
    return apiClient<CheckInResponse>(`/api/events/${eventId}/check-in`, {
      method: 'POST',
      body: JSON.stringify({ ticketCode }),
    });
  },

  getAttendanceStats: async (eventId: string): Promise<EventAttendanceStats> => {
    return apiClient<EventAttendanceStats>(`/api/events/${eventId}/stats`);
  },

  downloadAttendeesCsv: async (eventId: string, eventTitle: string): Promise<void> => {
    const token = localStorage.getItem('token');
    const headers = new Headers();
    if (token) {
      headers.set('Authorization', `Bearer ${token}`);
    }

    const response = await fetch(`${BASE_URL}/api/events/${eventId}/attendees.csv`, {
      headers,
    });

    if (!response.ok) {
      throw new Error('Failed to download CSV export');
    }

    const blob = await response.blob();
    const url = window.URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    const sanitizedTitle = eventTitle.toLowerCase().replace(/[^a-z0-9]/g, '-');
    a.download = `attendees-${sanitizedTitle}-${eventId.slice(0, 8)}.csv`;
    document.body.appendChild(a);
    a.click();
    window.URL.revokeObjectURL(url);
    document.body.removeChild(a);
  },
};
