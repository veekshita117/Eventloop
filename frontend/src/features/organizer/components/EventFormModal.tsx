import React, { useState, useEffect } from 'react';
import { CreateEventRequest, EventItem, UpdateEventRequest } from '../../../types';
import { Modal } from '../../../components/common/Modal';
import { Input } from '../../../components/common/Input';
import { Button } from '../../../components/common/Button';

interface EventFormModalProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (data: CreateEventRequest | UpdateEventRequest) => Promise<void>;
  eventToEdit?: EventItem | null;
  isLoading?: boolean;
}

export const EventFormModal: React.FC<EventFormModalProps> = ({
  isOpen,
  onClose,
  onSubmit,
  eventToEdit,
  isLoading = false,
}) => {
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [venue, setVenue] = useState('');
  const [startTime, setStartTime] = useState('');
  const [endTime, setEndTime] = useState('');
  const [capacity, setCapacity] = useState<number>(50);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (eventToEdit) {
      setTitle(eventToEdit.title);
      setDescription(eventToEdit.description);
      setVenue(eventToEdit.venue);
      // Format to datetime-local string (YYYY-MM-DDTHH:mm)
      setStartTime(new Date(eventToEdit.startTime).toISOString().slice(0, 16));
      setEndTime(new Date(eventToEdit.endTime).toISOString().slice(0, 16));
      setCapacity(eventToEdit.capacity);
    } else {
      // Default new event values
      setTitle('');
      setDescription('');
      setVenue('');
      const tomorrow = new Date();
      tomorrow.setDate(tomorrow.getDate() + 1);
      tomorrow.setHours(10, 0, 0, 0);
      setStartTime(tomorrow.toISOString().slice(0, 16));

      const tomorrowEnd = new Date(tomorrow);
      tomorrowEnd.setHours(14, 0, 0, 0);
      setEndTime(tomorrowEnd.toISOString().slice(0, 16));

      setCapacity(50);
    }
    setError(null);
  }, [eventToEdit, isOpen]);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);

    if (!title.trim() || !venue.trim() || !startTime || !endTime) {
      setError('Please fill in all required fields.');
      return;
    }

    if (new Date(endTime) <= new Date(startTime)) {
      setError('End time must be after start time.');
      return;
    }

    if (capacity <= 0) {
      setError('Capacity must be greater than 0.');
      return;
    }

    try {
      await onSubmit({
        title: title.trim(),
        description: description.trim(),
        venue: venue.trim(),
        startTime: new Date(startTime).toISOString(),
        endTime: new Date(endTime).toISOString(),
        capacity: Number(capacity),
      });
      onClose();
    } catch (err: any) {
      setError(err.message || 'Failed to save event.');
    }
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={eventToEdit ? 'Edit Event' : 'Create New Event'}
      maxWidth="lg"
    >
      <form onSubmit={handleSubmit} className="space-y-4">
        {error && (
          <div className="p-3 text-xs text-rose-700 bg-rose-50 border border-rose-200 rounded-xl font-medium">
            {error}
          </div>
        )}

        <Input
          label="Event Title *"
          placeholder="e.g. Annual Tech Symposium 2026"
          value={title}
          onChange={(e) => setTitle(e.target.value)}
          required
        />

        <div className="space-y-1">
          <label className="block text-sm font-medium text-slate-700">Description</label>
          <textarea
            className="w-full rounded-xl border border-slate-300 p-3 text-sm focus:outline-none focus:ring-2 focus:ring-brand-100 focus:border-brand-500 placeholder:text-slate-400"
            rows={3}
            placeholder="Share what attendees should expect, agenda, speakers, etc."
            value={description}
            onChange={(e) => setDescription(e.target.value)}
          />
        </div>

        <Input
          label="Venue / Location *"
          placeholder="e.g. Main Auditorium, Room 102"
          value={venue}
          onChange={(e) => setVenue(e.target.value)}
          required
        />

        <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
          <Input
            label="Start Date & Time *"
            type="datetime-local"
            value={startTime}
            onChange={(e) => setStartTime(e.target.value)}
            required
          />

          <Input
            label="End Date & Time *"
            type="datetime-local"
            value={endTime}
            onChange={(e) => setEndTime(e.target.value)}
            required
          />
        </div>

        <Input
          label="Capacity (Max Attendees) *"
          type="number"
          min={1}
          value={capacity}
          onChange={(e) => setCapacity(Number(e.target.value))}
          required
          helperText={eventToEdit ? "Cannot be reduced below current confirmed registrations." : undefined}
        />

        <div className="flex justify-end gap-3 pt-4 border-t border-slate-100">
          <Button variant="outline" type="button" onClick={onClose} disabled={isLoading}>
            Cancel
          </Button>
          <Button variant="primary" type="submit" isLoading={isLoading}>
            {eventToEdit ? 'Save Changes' : 'Create Event (Draft)'}
          </Button>
        </div>
      </form>
    </Modal>
  );
};
