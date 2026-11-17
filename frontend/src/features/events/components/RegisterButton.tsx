import React from 'react';
import { Ticket, Clock, CheckCircle2, AlertTriangle } from 'lucide-react';
import { Button } from '../../../components/common/Button';
import { EventItem, Registration } from '../../../types';

export interface RegisterButtonProps {
  event: EventItem;
  registration?: Registration | null;
  isAuthenticated: boolean;
  isLoading?: boolean;
  onRegister: () => void;
  onCancel: () => void;
  onNavigateLogin: () => void;
}

export const RegisterButton: React.FC<RegisterButtonProps> = ({
  event,
  registration,
  isAuthenticated,
  isLoading = false,
  onRegister,
  onCancel,
  onNavigateLogin,
}) => {
  if (!isAuthenticated) {
    const isWaitlist = event.spotsLeft === 0 || event.isSoldOut;
    return (
      <Button
        variant="primary"
        size="lg"
        className="w-full"
        onClick={onNavigateLogin}
        icon={isWaitlist ? <Clock className="w-5 h-5" /> : <Ticket className="w-5 h-5" />}
      >
        {isWaitlist ? 'Sign In to Join Waitlist' : 'Sign In to Register'}
      </Button>
    );
  }

  // Already registered states
  if (registration && registration.status !== 'Cancelled') {
    if (registration.status === 'CheckedIn') {
      return (
        <div className="p-4 rounded-xl bg-sky-50 border border-sky-200 flex items-center justify-between gap-3 text-sky-900">
          <div className="flex items-center gap-2">
            <CheckCircle2 className="w-5 h-5 text-sky-600 flex-shrink-0" />
            <span className="font-semibold text-sm">You are checked into this event!</span>
          </div>
          <span className="text-xs text-sky-700">Door verified</span>
        </div>
      );
    }

    if (registration.status === 'Confirmed') {
      return (
        <div className="space-y-3">
          <div className="p-4 rounded-xl bg-emerald-50 border border-emerald-200 flex items-center gap-3 text-emerald-900">
            <CheckCircle2 className="w-5 h-5 text-emerald-600 flex-shrink-0" />
            <div className="text-sm">
              <span className="font-bold block">You have a confirmed ticket!</span>
              <span className="text-xs text-emerald-700">Present your ticket QR code at the door.</span>
            </div>
          </div>
          <Button
            variant="outline"
            size="md"
            className="w-full text-rose-600 border-rose-200 hover:bg-rose-50"
            onClick={onCancel}
            isLoading={isLoading}
            icon={<AlertTriangle className="w-4 h-4" />}
          >
            Cancel Registration
          </Button>
        </div>
      );
    }

    if (registration.status === 'Waitlisted') {
      return (
        <div className="space-y-3">
          <div className="p-4 rounded-xl bg-amber-50 border border-amber-200 flex items-center gap-3 text-amber-900">
            <Clock className="w-5 h-5 text-amber-600 flex-shrink-0" />
            <div className="text-sm">
              <span className="font-bold block">
                Waitlisted – Position #{registration.waitlistPosition ?? 1}
              </span>
              <span className="text-xs text-amber-700">
                You will be automatically promoted if a confirmed attendee cancels!
              </span>
            </div>
          </div>
          <Button
            variant="outline"
            size="md"
            className="w-full text-rose-600 border-rose-200 hover:bg-rose-50"
            onClick={onCancel}
            isLoading={isLoading}
          >
            Leave Waitlist
          </Button>
        </div>
      );
    }
  }

  // Not registered yet
  const isWaitlist = event.spotsLeft === 0 || event.isSoldOut;

  return (
    <Button
      variant="primary"
      size="lg"
      className={`w-full ${isWaitlist ? 'bg-amber-600 hover:bg-amber-700 focus:ring-amber-500' : ''}`}
      onClick={onRegister}
      isLoading={isLoading}
      icon={isWaitlist ? <Clock className="w-5 h-5" /> : <Ticket className="w-5 h-5" />}
    >
      {isWaitlist ? 'Join Waitlist (Queue when full)' : 'Register for Event'}
    </Button>
  );
};
