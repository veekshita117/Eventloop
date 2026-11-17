import { ApiProblemDetails } from '../types';

const BASE_URL = import.meta.env.VITE_API_URL || '';

export class ApiError extends Error {
  status: number;
  problemDetails?: ApiProblemDetails;

  constructor(status: number, message: string, problemDetails?: ApiProblemDetails) {
    super(message);
    this.name = 'ApiError';
    this.status = status;
    this.problemDetails = problemDetails;
  }
}

export async function apiClient<T>(
  endpoint: string,
  options: RequestInit = {}
): Promise<T> {
  const token = localStorage.getItem('token');
  const headers = new Headers(options.headers || {});

  if (!headers.has('Content-Type') && !(options.body instanceof FormData)) {
    headers.set('Content-Type', 'application/json');
  }

  if (token) {
    headers.set('Authorization', `Bearer ${token}`);
  }

  const url = endpoint.startsWith('http') ? endpoint : `${BASE_URL}${endpoint}`;

  const response = await fetch(url, {
    ...options,
    headers,
  });

  if (response.status === 401) {
    localStorage.removeItem('token');
    localStorage.removeItem('user');
    if (!window.location.pathname.startsWith('/login') && !window.location.pathname.startsWith('/register')) {
      window.location.href = '/login?expired=true';
    }
    throw new ApiError(401, 'Session expired. Please log in again.');
  }

  if (!response.ok) {
    let errorMessage = `Request failed with status ${response.status}`;
    let problemDetails: ApiProblemDetails | undefined;

    try {
      const data = await response.json();
      problemDetails = data as ApiProblemDetails;
      if (problemDetails.detail) {
        errorMessage = problemDetails.detail;
      } else if (problemDetails.title) {
        errorMessage = problemDetails.title;
      } else if (problemDetails.errors) {
        const errorList = Object.values(problemDetails.errors).flat();
        if (errorList.length > 0) {
          errorMessage = errorList.join(', ');
        }
      }
    } catch {
      // Body wasn't JSON
    }

    throw new ApiError(response.status, errorMessage, problemDetails);
  }

  if (response.status === 204) {
    return {} as T;
  }

  const contentType = response.headers.get('content-type');
  if (contentType && contentType.includes('application/json')) {
    return (await response.json()) as T;
  }

  return (await response.text()) as unknown as T;
}
