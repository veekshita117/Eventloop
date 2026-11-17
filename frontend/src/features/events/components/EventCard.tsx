import React from 'react';
import { Link } from 'react-router-dom';
import { Calendar, MapPin, Users, ArrowRight } from 'lucide-react';
import { EventItem } from '../../../types';
import { SpotsLeftBadge } from '../../../components/common/Badge';

interface EventCardProps {
  event: EventItem;
}

export const EventCard: React.FC<EventCardProps> = ({ event }) => {
  const startDate = new Date(event.startTime);
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
    <div className="bg-white rounded-2xl border border-slate-200/80 shadow-sm hover:shadow-md transition-all duration-200 flex flex-col overflow-hidden group">
      <div className="p-6 flex-1 flex flex-col">
        <div className="flex items-start justify-between gap-3 mb-3">
          <SpotsLeftBadge
            spotsLeft={event.spotsLeft}
            isSoldOut={event.isSoldOut}
            waitlistCount={event.waitlistCount}
          />
          <span className="text-xs font-medium text-slate-400 capitalize">
            By {event.organizerName}
          </span>
        </div>

        <Link to={`/events/${event.id}`} className="group-hover:text-brand-600 transition-colors">
          <h3 className="text-lg font-bold text-slate-900 tracking-tight line-clamp-1 mb-2">
            {event.title}
          </h3>
        </Link>

        <p className="text-sm text-slate-500 line-clamp-2 mb-4 flex-1">
          {event.description}
        </p>

        <div className="space-y-1.5 pt-4 border-t border-slate-100 text-xs text-slate-600">
          <div className="flex items-center gap-2">
            <Calendar className="w-3.5 h-3.5 text-slate-400 flex-shrink-0" />
            <span>{formattedDate} at {formattedTime}</span>
          </div>
          <div className="flex items-center gap-2">
            <MapPin className="w-3.5 h-3.5 text-slate-400 flex-shrink-0" />
            <span className="truncate">{event.venue}</span>
          </div>
          <div className="flex items-center gap-2">
            <Users className="w-3.5 h-3.5 text-slate-400 flex-shrink-0" />
            <span>{event.confirmedCount} / {event.capacity} registered</span>
          </div>
        </div>
      </div>

      <div className="px-6 py-3 bg-slate-50 border-t border-slate-100 flex items-center justify-between text-xs font-semibold text-brand-600 group-hover:bg-brand-50/50 transition-colors">
        <span>View Details</span>
        <ArrowRight className="w-3.5 h-3.5 transform group-hover:translate-x-1 transition-transform" />
      </div>
    </div>
  );
};
