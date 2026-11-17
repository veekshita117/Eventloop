export type UserRole = 'Student' | 'Organizer' | 'Admin';

export type EventStatus = 'Draft' | 'Published' | 'Cancelled' | 'Completed';

export type RegistrationStatus = 'Confirmed' | 'Waitlisted' | 'Cancelled' | 'CheckedIn';

export interface User {
  id: string;
  name: string;
  email: string;
  role: UserRole;
  createdAt: string;
}

export interface AuthResponse {
  token: string;
  user: User;
}

export interface EventItem {
  id: string;
  organizerId: string;
  organizerName: string;
  title: string;
  description: string;
  venue: string;
  startTime: string;
  endTime: string;
  capacity: number;
  status: EventStatus;
  confirmedCount: number;
  waitlistCount: number;
  spotsLeft: number;
  isSoldOut: boolean;
  createdAt: string;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasPreviousPage: boolean;
  hasNextPage: boolean;
}

export interface EventQueryParameters {
  searchTerm?: string;
  fromDate?: string;
  toDate?: string;
  hasSpots?: boolean;
  sortBy?: string;
  sortDescending?: boolean;
  page?: number;
  pageSize?: number;
}

export interface CreateEventRequest {
  title: string;
  description: string;
  venue: string;
  startTime: string;
  endTime: string;
  capacity: number;
}

export interface UpdateEventRequest {
  title: string;
  description: string;
  venue: string;
  startTime: string;
  endTime: string;
  capacity: number;
}

export interface Registration {
  id: string;
  eventId: string;
  eventTitle: string;
  eventStartTime: string;
  eventEndTime: string;
  eventVenue: string;
  userId: string;
  userName: string;
  userEmail: string;
  status: RegistrationStatus;
  waitlistPosition?: number | null;
  ticketCode: string;
  registeredAt: string;
  checkedInAt?: string | null;
}

export interface TicketDetails {
  registrationId: string;
  ticketCode: string;
  eventId: string;
  eventTitle: string;
  eventVenue: string;
  eventStartTime: string;
  eventEndTime: string;
  attendeeName: string;
  attendeeEmail: string;
  status: RegistrationStatus;
  registeredAt: string;
  checkedInAt?: string | null;
}

export interface CheckInResponse {
  success: boolean;
  message: string;
  registrationId: string;
  ticketCode: string;
  attendeeName: string;
  attendeeEmail: string;
  eventTitle: string;
  checkedInAt: string;
  checkedInBy: string;
}

export interface CheckInIntervalStat {
  intervalStart: string;
  count: number;
}

export interface DailyRegistrationStat {
  date: string;
  count: number;
}

export interface EventAttendanceStats {
  eventId: string;
  eventTitle: string;
  capacity: number;
  confirmedCount: number;
  checkedInCount: number;
  waitlistCount: number;
  cancelledCount: number;
  noShowRate: number;
  checkInsPer15MinInterval: CheckInIntervalStat[];
  registrationsPerDay: DailyRegistrationStat[];
}

export interface ApiProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  errors?: Record<string, string[]>;
}
