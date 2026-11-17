import React from 'react';
import { EventStatus, RegistrationStatus } from '../../types';

interface BadgeProps {
  children?: React.ReactNode;
  variant?: 'slate' | 'indigo' | 'emerald' | 'amber' | 'rose' | 'sky';
  size?: 'sm' | 'md';
  className?: string;
}

export const Badge: React.FC<BadgeProps> = ({
  children,
  variant = 'slate',
  size = 'md',
  className = '',
}) => {
  const variantStyles = {
    slate: 'bg-slate-100 text-slate-700 border-slate-200',
    indigo: 'bg-indigo-50 text-indigo-700 border-indigo-200',
    emerald: 'bg-emerald-50 text-emerald-700 border-emerald-200',
    amber: 'bg-amber-50 text-amber-700 border-amber-200',
    rose: 'bg-rose-50 text-rose-700 border-rose-200',
    sky: 'bg-sky-50 text-sky-700 border-sky-200',
  };

  const sizeStyles = {
    sm: 'text-xs px-2 py-0.5 rounded-md font-medium',
    md: 'text-xs px-2.5 py-1 rounded-lg font-medium',
  };

  return (
    <span
      className={`inline-flex items-center gap-1 border ${sizeStyles[size]} ${variantStyles[variant]} ${className}`}
    >
      {children}
    </span>
  );
};

export const EventStatusBadge: React.FC<{ status: EventStatus }> = ({ status }) => {
  switch (status) {
    case 'Published':
      return <Badge variant="emerald">Published</Badge>;
    case 'Draft':
      return <Badge variant="slate">Draft</Badge>;
    case 'Cancelled':
      return <Badge variant="rose">Cancelled</Badge>;
    case 'Completed':
      return <Badge variant="sky">Completed</Badge>;
    default:
      return <Badge>{status}</Badge>;
  }
};

export const RegistrationStatusBadge: React.FC<{
  status: RegistrationStatus;
  waitlistPosition?: number | null;
}> = ({ status, waitlistPosition }) => {
  switch (status) {
    case 'Confirmed':
      return <Badge variant="emerald">Confirmed Ticket</Badge>;
    case 'Waitlisted':
      return (
        <Badge variant="amber">
          Waitlisted {waitlistPosition != null ? `#${waitlistPosition}` : ''}
        </Badge>
      );
    case 'CheckedIn':
      return <Badge variant="sky">Checked In</Badge>;
    case 'Cancelled':
      return <Badge variant="rose">Cancelled</Badge>;
    default:
      return <Badge>{status}</Badge>;
  }
};

export const SpotsLeftBadge: React.FC<{
  spotsLeft: number;
  isSoldOut: boolean;
  waitlistCount?: number;
}> = ({ spotsLeft, isSoldOut, waitlistCount = 0 }) => {
  if (isSoldOut || spotsLeft === 0) {
    return (
      <Badge variant="amber" className="font-semibold">
        Full – Join Waitlist {waitlistCount > 0 ? `(${waitlistCount} queued)` : ''}
      </Badge>
    );
  }

  if (spotsLeft <= 5) {
    return (
      <Badge variant="rose" className="font-semibold">
        Only {spotsLeft} spot{spotsLeft === 1 ? '' : 's'} left!
      </Badge>
    );
  }

  return (
    <Badge variant="emerald" className="font-semibold">
      {spotsLeft} spots available
    </Badge>
  );
};
