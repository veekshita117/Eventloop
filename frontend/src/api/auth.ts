import { apiClient } from './client';
import { AuthResponse, User } from '../types';

export const authApi = {
  login: async (email: string, password: string): Promise<AuthResponse> => {
    return apiClient<AuthResponse>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify({ email, password }),
    });
  },

  register: async (name: string, email: string, password: string): Promise<AuthResponse> => {
    return apiClient<AuthResponse>('/api/auth/register', {
      method: 'POST',
      body: JSON.stringify({ name, email, password }),
    });
  },

  getCurrentUser: async (): Promise<User> => {
    return apiClient<User>('/api/auth/me', {
      method: 'GET',
    });
  },

  promoteOrganizer: async (userId: string): Promise<void> => {
    return apiClient<void>(`/api/users/${userId}/promote-organizer`, {
      method: 'POST',
    });
  },
};
