import React, { useState } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Calendar,
  MapPin,
  Users,
  ArrowLeft,
  Ticket,
  AlertCircle,
  HelpCircle,
} from 'lucide-react';
import { eventsApi } from '../../api/events';
import { registrationsApi } from '../../api/registrations';
import { useAuth } from '../../context/AuthContext';
import { useToast } from '../../context/ToastContext';
import { SpotsLeftBadge } from '../../components/common/Badge';
import { Button } from '../../components/common/Button';
import { Modal } from '../../components/common/Modal';
import { RegisterButton } from './components/RegisterButton';

export const EventDetailsPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { isAuthenticated } = useAuth();
  const { success, error } = useToast();

  const [confirmCancelOpen, setConfirmCancelOpen] = useState(false);

  // Fetch event details
  const {
    data: event,
    isLoading: eventLoading,
    error: eventError,
  } = useQuery({
    queryKey: ['event', id],
    queryFn: () => eventsApi.getEventById(id!),
    enabled: !!id,
  });

  // Fetch user's registrations to check current status for this event
  const { data: myRegistrations, isLoading: regLoading } = useQuery({
    queryKey: ['myRegistrations'],
    queryFn: () => registrationsApi.getMyRegistrations(),
    enabled: isAuthenticated,
  });

  const currentRegistration = myRegistrations?.find((r) => r.eventId === id);

  // Register mutation
  const registerMutation = useMutation({
    mutationFn: () => registrationsApi.registerForEvent(id!),
    onSuccess: (data) => {
      queryClient.invalidateQueries({ queryKey: ['event', id] });
      queryClient.invalidateQueries({ queryKey: ['myRegistrations'] });
      queryClient.invalidateQueries({ queryKey: ['events'] });

      if (data.status === 'Confirmed') {
        success('You are registered! Your ticket QR code is ready in My Tickets.');
      } else if (data.status === 'Waitlisted') {
        success(`Event is full. You joined the waitlist at Position #${data.waitlistPosition}!`);
      }
    },
    onError: (err: any) => {
      error(err.message || 'Registration failed.');
    },
  });

  // Cancel mutation
  const cancelMutation = useMutation({
    mutationFn: () => registrationsApi.cancelRegistration(id!),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['event', id] });
      queryClient.invalidateQueries({ queryKey: ['myRegistrations'] });
      queryClient.invalidateQueries({ queryKey: ['events'] });
      setConfirmCancelOpen(false);
      success('Registration cancelled successfully.');
    },
    onError: (err: any) => {
      error(err.message || 'Failed to cancel registration.');
    },
  });

  if (eventLoading || (isAuthenticated && regLoading)) {
    return (
      <div className="max-w-4xl mx-auto px-4 py-16 text-center">
        <div className="animate-spin rounded-full h-10 w-10 border-b-2 border-brand-600 mx-auto"></div>
        <p className="mt-4 text-sm text-slate-500">Loading event details...</p>
      </div>
    );
  }

  if (eventError || !event) {
    return (
      <div className="max-w-xl mx-auto px-4 py-16 text-center space-y-4">
        <AlertCircle className="w-12 h-12 text-rose-500 mx-auto" />
        <h2 className="text-xl font-bold text-slate-900">Event Not Found</h2>
        <p className="text-sm text-slate-600">
          The event you requested could not be found or is no longer available.
        </p>
        <Link to="/events">
          <Button variant="outline" icon={<ArrowLeft className="w-4 h-4" />}>
            Back to Events
          </Button>
        </Link>
      </div>
    );
  }

  const startDate = new Date(event.startTime);
  const endDate = new Date(event.endTime);

  const formattedDate = startDate.toLocaleDateString(undefined, {
    weekday: 'long',
    month: 'long',
    day: 'numeric',
    year: 'numeric',
  });

  const formattedStartTime = startDate.toLocaleTimeString(undefined, {
    hour: 'numeric',
    minute: '2-digit',
  });

  const formattedEndTime = endDate.toLocaleTimeString(undefined, {
    hour: 'numeric',
    minute: '2-digit',
  });

  return (
    <div className="max-w-4xl mx-auto px-4 sm:px-6 lg:px-8 py-8 md:py-12 space-y-8">
      {/* Top back link */}
      <div>
        <Link
          to="/events"
          className="inline-flex items-center gap-1.5 text-xs font-semibold text-slate-500 hover:text-slate-900 transition-colors"
        >
          <ArrowLeft className="w-4 h-4" />
          Back to Events Feed
        </Link>
      </div>

      {/* Main Event Card */}
      <div className="bg-white rounded-3xl border border-slate-200/80 shadow-sm overflow-hidden">
        {/* Header section */}
        <div className="p-6 sm:p-8 border-b border-slate-100 space-y-4">
          <div className="flex flex-wrap items-center justify-between gap-3">
            <SpotsLeftBadge
              spotsLeft={event.spotsLeft}
              isSoldOut={event.isSoldOut}
              waitlistCount={event.waitlistCount}
            />
            <span className="text-sm text-slate-500">
              Organized by <strong className="text-slate-800">{event.organizerName}</strong>
            </span>
          </div>

          <h1 className="text-2xl sm:text-3xl font-extrabold text-slate-900 tracking-tight">
            {event.title}
          </h1>

          {/* Quick info row */}
          <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 pt-4 border-t border-slate-100">
            <div className="flex items-start gap-3">
              <div className="p-2.5 rounded-xl bg-indigo-50 text-brand-600">
                <Calendar className="w-5 h-5" />
              </div>
              <div>
                <span className="block text-xs font-medium text-slate-400">Date & Time</span>
                <span className="text-sm font-semibold text-slate-800 block">{formattedDate}</span>
                <span className="text-xs text-slate-500">
                  {formattedStartTime} &ndash; {formattedEndTime}
                </span>
              </div>
            </div>

            <div className="flex items-start gap-3">
              <div className="p-2.5 rounded-xl bg-indigo-50 text-brand-600">
                <MapPin className="w-5 h-5" />
              </div>
              <div>
                <span className="block text-xs font-medium text-slate-400">Location</span>
                <span className="text-sm font-semibold text-slate-800">{event.venue}</span>
              </div>
            </div>

            <div className="flex items-start gap-3">
              <div className="p-2.5 rounded-xl bg-indigo-50 text-brand-600">
                <Users className="w-5 h-5" />
              </div>
              <div>
                <span className="block text-xs font-medium text-slate-400">Capacity & Spots</span>
                <span className="text-sm font-semibold text-slate-800">
                  {event.confirmedCount} / {event.capacity} Confirmed
                </span>
                <span className="text-xs text-slate-500 block">
                  {event.waitlistCount > 0 ? `${event.waitlistCount} on waitlist` : 'No waitlist queue'}
                </span>
              </div>
            </div>
          </div>
        </div>

        {/* Content body and registration action */}
        <div className="p-6 sm:p-8 space-y-8">
          <div>
            <h2 className="text-base font-bold text-slate-900 mb-3">About this Event</h2>
            <div className="prose prose-slate max-w-none text-sm text-slate-600 leading-relaxed whitespace-pre-wrap">
              {event.description}
            </div>
          </div>

          {/* Registration Section */}
          <div className="pt-6 border-t border-slate-100">
            <div className="max-w-md mx-auto space-y-4">
              <RegisterButton
                event={event}
                registration={currentRegistration}
                isAuthenticated={isAuthenticated}
                isLoading={registerMutation.isPending || cancelMutation.isPending}
                onRegister={() => registerMutation.mutate()}
                onCancel={() => setConfirmCancelOpen(true)}
                onNavigateLogin={() => navigate(`/login?returnUrl=/events/${id}`)}
              />

              {currentRegistration?.status === 'Confirmed' && (
                <div className="text-center">
                  <Link
                    to="/my-tickets"
                    className="inline-flex items-center gap-1.5 text-xs font-semibold text-brand-600 hover:text-brand-700"
                  >
                    <Ticket className="w-3.5 h-3.5" />
                    Open Ticket QR Code in My Tickets
                  </Link>
                </div>
              )}

              {/* Waitlist explanation box */}
              {event.spotsLeft === 0 && (!currentRegistration || currentRegistration.status === 'Cancelled') && (
                <div className="p-3.5 rounded-xl bg-slate-50 border border-slate-200 text-xs text-slate-600 flex items-start gap-2.5">
                  <HelpCircle className="w-4 h-4 text-slate-400 flex-shrink-0 mt-0.5" />
                  <span>
                    <strong>How waitlist works:</strong> If you join, you will be queued in first-come, first-served order. When any confirmed attendee cancels, Eventloop automatically promotes the first student on the waitlist!
                  </span>
                </div>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* Cancellation Confirmation Modal */}
      <Modal
        isOpen={confirmCancelOpen}
        onClose={() => setConfirmCancelOpen(false)}
        title="Cancel Registration"
        maxWidth="sm"
      >
        <div className="space-y-4">
          <p className="text-sm text-slate-600">
            Are you sure you want to cancel your registration for <strong>{event.title}</strong>?
          </p>
          {currentRegistration?.status === 'Confirmed' && (
            <p className="text-xs text-amber-700 bg-amber-50 p-3 rounded-xl border border-amber-200">
              Your confirmed spot will be immediately offered to the next waitlisted student.
            </p>
          )}
          <div className="flex justify-end gap-3 pt-4 border-t border-slate-100">
            <Button
              variant="outline"
              size="sm"
              onClick={() => setConfirmCancelOpen(false)}
            >
              Keep Registration
            </Button>
            <Button
              variant="danger"
              size="sm"
              isLoading={cancelMutation.isPending}
              onClick={() => cancelMutation.mutate()}
            >
              Yes, Cancel It
            </Button>
          </div>
        </div>
      </Modal>
    </div>
  );
};
