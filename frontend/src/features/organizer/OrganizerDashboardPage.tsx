import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import {
  Plus,
  Calendar,
  Settings,
  Edit2,
  Send,
  XCircle,
  AlertCircle,
} from 'lucide-react';
import { eventsApi } from '../../api/events';
import { CreateEventRequest, EventItem, UpdateEventRequest } from '../../types';
import { EventStatusBadge, SpotsLeftBadge } from '../../components/common/Badge';
import { Button } from '../../components/common/Button';
import { EmptyState } from '../../components/common/EmptyState';
import { Skeleton } from '../../components/common/Skeleton';
import { EventFormModal } from './components/EventFormModal';
import { Modal } from '../../components/common/Modal';
import { useToast } from '../../context/ToastContext';

export const OrganizerDashboardPage: React.FC = () => {
  const queryClient = useQueryClient();
  const { success, error } = useToast();

  const [isCreateModalOpen, setIsCreateModalOpen] = useState(false);
  const [editingEvent, setEditingEvent] = useState<EventItem | null>(null);
  const [confirmCancelEvent, setConfirmCancelEvent] = useState<EventItem | null>(null);
  const [confirmPublishEvent, setConfirmPublishEvent] = useState<EventItem | null>(null);

  // Load organizer events
  const { data: events, isLoading, error: queryError } = useQuery({
    queryKey: ['myEvents'],
    queryFn: () => eventsApi.getMyEvents(),
  });

  // Create mutation
  const createMutation = useMutation({
    mutationFn: (data: CreateEventRequest) => eventsApi.createEvent(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['myEvents'] });
      queryClient.invalidateQueries({ queryKey: ['events'] });
      success('Event created in Draft mode. You can edit or publish it when ready.');
    },
    onError: (err: any) => {
      error(err.message || 'Failed to create event.');
    },
  });

  // Update mutation
  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateEventRequest }) =>
      eventsApi.updateEvent(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['myEvents'] });
      queryClient.invalidateQueries({ queryKey: ['events'] });
      success('Event updated successfully.');
    },
    onError: (err: any) => {
      error(err.message || 'Failed to update event.');
    },
  });

  // Publish mutation
  const publishMutation = useMutation({
    mutationFn: (id: string) => eventsApi.publishEvent(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['myEvents'] });
      queryClient.invalidateQueries({ queryKey: ['events'] });
      setConfirmPublishEvent(null);
      success('Event published! Students can now register.');
    },
    onError: (err: any) => {
      error(err.message || 'Failed to publish event.');
    },
  });

  // Cancel mutation
  const cancelMutation = useMutation({
    mutationFn: (id: string) => eventsApi.cancelEvent(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['myEvents'] });
      queryClient.invalidateQueries({ queryKey: ['events'] });
      setConfirmCancelEvent(null);
      success('Event has been cancelled.');
    },
    onError: (err: any) => {
      error(err.message || 'Failed to cancel event.');
    },
  });

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 md:py-12 space-y-8">
      {/* Header and Create Button */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between gap-4">
        <div>
          <h1 className="text-2xl sm:text-3xl font-extrabold tracking-tight text-slate-900">
            Organizer Portal
          </h1>
          <p className="text-sm text-slate-500 mt-1">
            Manage your club events, review registration rosters, scan tickets at the door, and monitor attendance metrics.
          </p>
        </div>

        <Button
          variant="primary"
          size="md"
          icon={<Plus className="w-4 h-4" />}
          onClick={() => setIsCreateModalOpen(true)}
        >
          Create New Event
        </Button>
      </div>

      {/* Events List */}
      {isLoading ? (
        <div className="space-y-4">
          {Array.from({ length: 3 }).map((_, i) => (
            <Skeleton key={i} className="h-32 w-full rounded-2xl" />
          ))}
        </div>
      ) : queryError ? (
        <div className="p-8 rounded-2xl bg-rose-50 border border-rose-200 text-center">
          <p className="text-sm text-rose-700">Failed to load events: {(queryError as Error).message}</p>
        </div>
      ) : events?.length === 0 ? (
        <EmptyState
          icon={Calendar}
          title="No events yet"
          description="You haven't created any events yet. Click below to host your first college event!"
          actionText="Create Event"
          onAction={() => setIsCreateModalOpen(true)}
        />
      ) : (
        <div className="space-y-4">
          {events?.map((ev) => {
            const startDate = new Date(ev.startTime);
            const isDraft = ev.status === 'Draft';
            const isPublished = ev.status === 'Published';

            return (
              <div
                key={ev.id}
                className="bg-white p-6 rounded-2xl border border-slate-200/80 shadow-sm hover:shadow-md transition-all flex flex-col lg:flex-row lg:items-center lg:justify-between gap-6"
              >
                <div className="space-y-2 flex-1">
                  <div className="flex flex-wrap items-center gap-2">
                    <EventStatusBadge status={ev.status} />
                    <SpotsLeftBadge
                      spotsLeft={ev.spotsLeft}
                      isSoldOut={ev.isSoldOut}
                      waitlistCount={ev.waitlistCount}
                    />
                    <span className="text-xs text-slate-400">
                      {startDate.toLocaleDateString(undefined, {
                        weekday: 'short',
                        month: 'short',
                        day: 'numeric',
                        year: 'numeric',
                      })}
                    </span>
                  </div>

                  <h3 className="text-lg font-bold text-slate-900 tracking-tight">
                    {ev.title}
                  </h3>

                  <p className="text-xs text-slate-500 line-clamp-1">
                    {ev.venue} &bull; {ev.confirmedCount} / {ev.capacity} Confirmed &bull;{' '}
                    {ev.waitlistCount} waitlisted
                  </p>
                </div>

                {/* Actions row */}
                <div className="flex flex-wrap items-center gap-2">
                  <Link to={`/organizer/events/${ev.id}`}>
                    <Button variant="outline" size="sm" icon={<Settings className="w-3.5 h-3.5" />}>
                      Manage (Roster & Scan)
                    </Button>
                  </Link>

                  <Button
                    variant="ghost"
                    size="sm"
                    icon={<Edit2 className="w-3.5 h-3.5" />}
                    onClick={() => setEditingEvent(ev)}
                  >
                    Edit
                  </Button>

                  {isDraft && (
                    <Button
                      variant="primary"
                      size="sm"
                      icon={<Send className="w-3.5 h-3.5" />}
                      onClick={() => setConfirmPublishEvent(ev)}
                    >
                      Publish
                    </Button>
                  )}

                  {isPublished && (
                    <Button
                      variant="ghost"
                      size="sm"
                      className="text-rose-600 hover:bg-rose-50"
                      icon={<XCircle className="w-3.5 h-3.5" />}
                      onClick={() => setConfirmCancelEvent(ev)}
                    >
                      Cancel Event
                    </Button>
                  )}
                </div>
              </div>
            );
          })}
        </div>
      )}

      {/* Create Event Modal */}
      <EventFormModal
        isOpen={isCreateModalOpen}
        onClose={() => setIsCreateModalOpen(false)}
        onSubmit={async (data) => {
          await createMutation.mutateAsync(data as CreateEventRequest);
        }}
        isLoading={createMutation.isPending}
      />

      {/* Edit Event Modal */}
      <EventFormModal
        isOpen={!!editingEvent}
        eventToEdit={editingEvent}
        onClose={() => setEditingEvent(null)}
        onSubmit={async (data) => {
          if (editingEvent) {
            await updateMutation.mutateAsync({ id: editingEvent.id, data: data as UpdateEventRequest });
          }
        }}
        isLoading={updateMutation.isPending}
      />

      {/* Confirm Publish Modal */}
      <Modal
        isOpen={!!confirmPublishEvent}
        onClose={() => setConfirmPublishEvent(null)}
        title="Publish Event"
        maxWidth="sm"
      >
        <div className="space-y-4">
          <p className="text-sm text-slate-600">
            Are you ready to publish <strong>{confirmPublishEvent?.title}</strong>? Once published, students will be able to see this event and register.
          </p>
          <div className="flex justify-end gap-3 pt-4 border-t border-slate-100">
            <Button
              variant="outline"
              size="sm"
              onClick={() => setConfirmPublishEvent(null)}
            >
              Cancel
            </Button>
            <Button
              variant="primary"
              size="sm"
              isLoading={publishMutation.isPending}
              onClick={() => confirmPublishEvent && publishMutation.mutate(confirmPublishEvent.id)}
            >
              Publish Now
            </Button>
          </div>
        </div>
      </Modal>

      {/* Confirm Cancel Modal */}
      <Modal
        isOpen={!!confirmCancelEvent}
        onClose={() => setConfirmCancelEvent(null)}
        title="Cancel Event"
        maxWidth="sm"
      >
        <div className="space-y-4">
          <div className="p-3 bg-rose-50 border border-rose-200 rounded-xl flex items-start gap-2.5 text-xs text-rose-800">
            <AlertCircle className="w-4 h-4 text-rose-600 flex-shrink-0 mt-0.5" />
            <span>
              Cancelling this event will mark it as cancelled for all attendees and prevent any new registrations or check-ins.
            </span>
          </div>
          <p className="text-sm text-slate-600">
            Are you sure you want to cancel <strong>{confirmCancelEvent?.title}</strong>?
          </p>
          <div className="flex justify-end gap-3 pt-4 border-t border-slate-100">
            <Button
              variant="outline"
              size="sm"
              onClick={() => setConfirmCancelEvent(null)}
            >
              Keep Event
            </Button>
            <Button
              variant="danger"
              size="sm"
              isLoading={cancelMutation.isPending}
              onClick={() => confirmCancelEvent && cancelMutation.mutate(confirmCancelEvent.id)}
            >
              Yes, Cancel Event
            </Button>
          </div>
        </div>
      </Modal>
    </div>
  );
};
