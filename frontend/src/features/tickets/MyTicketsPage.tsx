import React, { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { Ticket, Calendar, MapPin, QrCode } from 'lucide-react';
import { registrationsApi } from '../../api/registrations';
import { RegistrationStatusBadge } from '../../components/common/Badge';
import { Button } from '../../components/common/Button';
import { EmptyState } from '../../components/common/EmptyState';
import { Skeleton } from '../../components/common/Skeleton';
import { TicketModal } from './TicketModal';
import { Registration } from '../../types';

export const MyTicketsPage: React.FC = () => {
  const [selectedRegId, setSelectedRegId] = useState<string | null>(null);
  const [statusFilter, setStatusFilter] = useState<'All' | 'Confirmed' | 'Waitlisted' | 'CheckedIn'>('All');

  const { data: registrations, isLoading, error } = useQuery({
    queryKey: ['myRegistrations'],
    queryFn: () => registrationsApi.getMyRegistrations(),
  });

  const filteredRegistrations = registrations?.filter((r) => {
    if (statusFilter === 'All') return true;
    return r.status === statusFilter;
  });

  return (
    <div className="max-w-5xl mx-auto px-4 sm:px-6 lg:px-8 py-8 md:py-12 space-y-8">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl sm:text-3xl font-extrabold tracking-tight text-slate-900">
            My Event Tickets
          </h1>
          <p className="text-sm text-slate-500 mt-1">
            Access your admission QR passes, waitlist positions, and check-in history.
          </p>
        </div>

        {/* Status filter tabs */}
        <div className="flex items-center gap-1.5 p-1 bg-slate-100 rounded-xl">
          {(['All', 'Confirmed', 'Waitlisted', 'CheckedIn'] as const).map((tab) => (
            <button
              key={tab}
              onClick={() => setStatusFilter(tab)}
              className={`px-3 py-1.5 rounded-lg text-xs font-semibold transition-all ${
                statusFilter === tab
                  ? 'bg-white text-slate-900 shadow-sm'
                  : 'text-slate-600 hover:text-slate-900'
              }`}
            >
              {tab === 'CheckedIn' ? 'Checked In' : tab}
            </button>
          ))}
        </div>
      </div>

      {/* Tickets List */}
      {isLoading ? (
        <div className="space-y-4">
          {Array.from({ length: 3 }).map((_, i) => (
            <Skeleton key={i} className="h-28 w-full rounded-2xl" />
          ))}
        </div>
      ) : error ? (
        <div className="p-8 rounded-2xl bg-rose-50 border border-rose-200 text-center">
          <p className="text-sm text-rose-700">Failed to load tickets: {(error as Error).message}</p>
        </div>
      ) : filteredRegistrations?.length === 0 ? (
        <EmptyState
          icon={Ticket}
          title="No registrations found"
          description={
            statusFilter === 'All'
              ? "You haven't registered for any events yet. Check out upcoming campus events to get started!"
              : `You don't have any tickets with status "${statusFilter}".`
          }
          actionText="Browse Events"
          onAction={() => (window.location.href = '/events')}
        />
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {filteredRegistrations?.map((reg: Registration) => {
            const startDate = new Date(reg.eventStartTime);
            const formattedDate = startDate.toLocaleDateString(undefined, {
              weekday: 'short',
              month: 'short',
              day: 'numeric',
            });
            const formattedTime = startDate.toLocaleTimeString(undefined, {
              hour: 'numeric',
              minute: '2-digit',
            });

            return (
              <div
                key={reg.id}
                className="bg-white rounded-2xl border border-slate-200/80 shadow-sm hover:shadow-md transition-all duration-200 overflow-hidden flex flex-col justify-between"
              >
                <div className="p-5 space-y-3">
                  <div className="flex items-start justify-between gap-3">
                    <RegistrationStatusBadge
                      status={reg.status}
                      waitlistPosition={reg.waitlistPosition}
                    />
                    <span className="text-[11px] text-slate-400">
                      Reg {new Date(reg.registeredAt).toLocaleDateString()}
                    </span>
                  </div>

                  <Link
                    to={`/events/${reg.eventId}`}
                    className="block text-base font-bold text-slate-900 hover:text-brand-600 transition-colors line-clamp-1"
                  >
                    {reg.eventTitle}
                  </Link>

                  <div className="space-y-1 text-xs text-slate-600">
                    <div className="flex items-center gap-2">
                      <Calendar className="w-3.5 h-3.5 text-slate-400 flex-shrink-0" />
                      <span>{formattedDate} at {formattedTime}</span>
                    </div>
                    <div className="flex items-center gap-2">
                      <MapPin className="w-3.5 h-3.5 text-slate-400 flex-shrink-0" />
                      <span className="truncate">{reg.eventVenue}</span>
                    </div>
                  </div>
                </div>

                <div className="px-5 py-3 bg-slate-50 border-t border-slate-100 flex items-center justify-between">
                  <span className="font-mono text-[11px] text-slate-400">
                    ID: {reg.ticketCode.slice(0, 8)}...
                  </span>
                  <Button
                    variant="primary"
                    size="sm"
                    onClick={() => setSelectedRegId(reg.id)}
                    icon={<QrCode className="w-3.5 h-3.5" />}
                  >
                    View QR Pass
                  </Button>
                </div>
              </div>
            );
          })}
        </div>
      )}

      {/* Ticket QR Modal */}
      <TicketModal
        registrationId={selectedRegId}
        onClose={() => setSelectedRegId(null)}
      />
    </div>
  );
};
