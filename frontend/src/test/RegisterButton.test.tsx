import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/react';
import { RegisterButton } from '../features/events/components/RegisterButton';
import { EventItem, Registration } from '../types';

const mockEventWithSpots: EventItem = {
  id: 'ev-1',
  organizerId: 'org-1',
  organizerName: 'Robotics Club',
  title: 'Robotics Showcase',
  description: 'Annual showcase of autonomous robots.',
  venue: 'Engineering Hall',
  startTime: '2026-12-01T10:00:00Z',
  endTime: '2026-12-01T12:00:00Z',
  capacity: 100,
  status: 'Published',
  confirmedCount: 50,
  waitlistCount: 0,
  spotsLeft: 50,
  isSoldOut: false,
  createdAt: '2026-11-01T10:00:00Z',
};

const mockFullEvent: EventItem = {
  ...mockEventWithSpots,
  id: 'ev-2',
  confirmedCount: 100,
  spotsLeft: 0,
  isSoldOut: true,
  waitlistCount: 5,
};

describe('RegisterButton Component', () => {
  it('renders "Sign In to Register" when unauthenticated and triggers login callback', () => {
    const handleLogin = vi.fn();
    render(
      <RegisterButton
        event={mockEventWithSpots}
        isAuthenticated={false}
        onRegister={vi.fn()}
        onCancel={vi.fn()}
        onNavigateLogin={handleLogin}
      />
    );

    const button = screen.getByRole('button', { name: /sign in to register/i });
    expect(button).toBeInTheDocument();

    fireEvent.click(button);
    expect(handleLogin).toHaveBeenCalledTimes(1);
  });

  it('renders "Register for Event" when authenticated and spots are available', () => {
    const handleRegister = vi.fn();
    render(
      <RegisterButton
        event={mockEventWithSpots}
        isAuthenticated={true}
        onRegister={handleRegister}
        onCancel={vi.fn()}
        onNavigateLogin={vi.fn()}
      />
    );

    const button = screen.getByRole('button', { name: /register for event/i });
    expect(button).toBeInTheDocument();

    fireEvent.click(button);
    expect(handleRegister).toHaveBeenCalledTimes(1);
  });

  it('renders "Join Waitlist" when authenticated and event is full', () => {
    const handleRegister = vi.fn();
    render(
      <RegisterButton
        event={mockFullEvent}
        isAuthenticated={true}
        onRegister={handleRegister}
        onCancel={vi.fn()}
        onNavigateLogin={vi.fn()}
      />
    );

    const button = screen.getByRole('button', { name: /join waitlist/i });
    expect(button).toBeInTheDocument();

    fireEvent.click(button);
    expect(handleRegister).toHaveBeenCalledTimes(1);
  });

  it('renders confirmed state and "Cancel Registration" button when user is confirmed', () => {
    const handleCancel = vi.fn();
    const confirmedRegistration: Registration = {
      id: 'reg-1',
      eventId: mockEventWithSpots.id,
      eventTitle: mockEventWithSpots.title,
      eventStartTime: mockEventWithSpots.startTime,
      eventEndTime: mockEventWithSpots.endTime,
      eventVenue: mockEventWithSpots.venue,
      userId: 'usr-1',
      userName: 'Alice',
      userEmail: 'alice@college.edu',
      status: 'Confirmed',
      ticketCode: '11111111-1111-1111-1111-111111111111',
      registeredAt: '2026-11-15T10:00:00Z',
    };

    render(
      <RegisterButton
        event={mockEventWithSpots}
        registration={confirmedRegistration}
        isAuthenticated={true}
        onRegister={vi.fn()}
        onCancel={handleCancel}
        onNavigateLogin={vi.fn()}
      />
    );

    expect(screen.getByText(/you have a confirmed ticket!/i)).toBeInTheDocument();
    const cancelButton = screen.getByRole('button', { name: /cancel registration/i });
    expect(cancelButton).toBeInTheDocument();

    fireEvent.click(cancelButton);
    expect(handleCancel).toHaveBeenCalledTimes(1);
  });

  it('renders waitlist position and "Leave Waitlist" button when user is waitlisted', () => {
    const handleCancel = vi.fn();
    const waitlistedRegistration: Registration = {
      id: 'reg-2',
      eventId: mockFullEvent.id,
      eventTitle: mockFullEvent.title,
      eventStartTime: mockFullEvent.startTime,
      eventEndTime: mockFullEvent.endTime,
      eventVenue: mockFullEvent.venue,
      userId: 'usr-2',
      userName: 'Bob',
      userEmail: 'bob@college.edu',
      status: 'Waitlisted',
      waitlistPosition: 3,
      ticketCode: '22222222-2222-2222-2222-222222222222',
      registeredAt: '2026-11-16T10:00:00Z',
    };

    render(
      <RegisterButton
        event={mockFullEvent}
        registration={waitlistedRegistration}
        isAuthenticated={true}
        onRegister={vi.fn()}
        onCancel={handleCancel}
        onNavigateLogin={vi.fn()}
      />
    );

    expect(screen.getByText(/waitlisted – position #3/i)).toBeInTheDocument();
    const leaveButton = screen.getByRole('button', { name: /leave waitlist/i });
    expect(leaveButton).toBeInTheDocument();

    fireEvent.click(leaveButton);
    expect(handleCancel).toHaveBeenCalledTimes(1);
  });
});
