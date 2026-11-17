import { describe, it, expect } from 'vitest';
import { render, screen } from '@testing-library/react';
import { CheckInResultPanel, CheckInScanResult } from '../features/organizer/components/CheckInResultPanel';

describe('CheckInResultPanel Component', () => {
  it('renders "Ready to Scan" idle state when result is null', () => {
    render(<CheckInResultPanel result={null} />);

    expect(screen.getByText(/ready to scan/i)).toBeInTheDocument();
    expect(
      screen.getByText(/point the camera at an attendee's qr ticket pass/i)
    ).toBeInTheDocument();
  });

  it('renders green verified admission banner on success with attendee name', () => {
    const successResult: CheckInScanResult = {
      success: true,
      message: 'Check-in successful.',
      attendeeName: 'Alice Johnson',
      attendeeEmail: 'alice@college.edu',
      checkedInAt: '2026-11-17T12:00:00Z',
      ticketCode: '12345678-1234-1234-1234-1234567890ab',
    };

    render(<CheckInResultPanel result={successResult} />);

    expect(screen.getByTestId('checkin-success-panel')).toBeInTheDocument();
    expect(screen.getByText(/verified admission/i)).toBeInTheDocument();
    expect(screen.getByText(/checked in: alice johnson/i)).toBeInTheDocument();
    expect(screen.getByText('alice@college.edu')).toBeInTheDocument();
  });

  it('renders red duplicate check-in banner when ticket was already checked in', () => {
    const duplicateResult: CheckInScanResult = {
      success: false,
      message: 'Ticket was already checked in at 2026-11-17 11:30:00 UTC by scanner org-1.',
      ticketCode: '12345678-1234-1234-1234-1234567890ab',
      isDuplicate: true,
    };

    render(<CheckInResultPanel result={duplicateResult} />);

    expect(screen.getByTestId('checkin-error-panel')).toBeInTheDocument();
    expect(screen.getByText(/duplicate check-in/i)).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: /already checked in/i })).toBeInTheDocument();
    expect(
      screen.getByText(/ticket was already checked in at 2026-11-17 11:30:00 utc/i)
    ).toBeInTheDocument();
  });

  it('renders red admission denied banner for invalid or waitlisted ticket', () => {
    const errorResult: CheckInScanResult = {
      success: false,
      message: 'This user is on the waitlist (Position #2) and does not have a confirmed ticket.',
      ticketCode: '87654321-4321-4321-4321-ba0987654321',
      isDuplicate: false,
    };

    render(<CheckInResultPanel result={errorResult} />);

    expect(screen.getByTestId('checkin-error-panel')).toBeInTheDocument();
    expect(screen.getByText(/admission denied/i)).toBeInTheDocument();
    expect(screen.getByText(/invalid ticket/i)).toBeInTheDocument();
    expect(screen.getByText(/this user is on the waitlist \(position #2\)/i)).toBeInTheDocument();
  });
});
