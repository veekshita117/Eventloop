import React, { useState } from 'react';
import { useParams, Link, useSearchParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import {
  ArrowLeft,
  Users,
  Camera,
  BarChart3,
  Calendar,
  MapPin,
  AlertCircle,
} from 'lucide-react';
import { eventsApi } from '../../api/events';
import { EventStatusBadge, SpotsLeftBadge } from '../../components/common/Badge';
import { AttendeesTab } from './tabs/AttendeesTab';
import { CheckInTab } from './tabs/CheckInTab';
import { StatsTab } from './tabs/StatsTab';

type TabType = 'attendees' | 'check-in' | 'stats';

export const EventManagePage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const [searchParams, setSearchParams] = useSearchParams();
  const initialTab = (searchParams.get('tab') as TabType) || 'attendees';
  const [activeTab, setActiveTab] = useState<TabType>(initialTab);

  const { data: event, isLoading, error } = useQuery({
    queryKey: ['event', id],
    queryFn: () => eventsApi.getEventById(id!),
    enabled: !!id,
  });

  const handleTabChange = (tab: TabType) => {
    setActiveTab(tab);
    setSearchParams({ tab });
  };

  if (isLoading) {
    return (
      <div className="max-w-7xl mx-auto px-4 py-16 text-center">
        <div className="animate-spin rounded-full h-10 w-10 border-b-2 border-brand-600 mx-auto"></div>
        <p className="mt-4 text-sm text-slate-500">Loading event management portal...</p>
      </div>
    );
  }

  if (error || !event) {
    return (
      <div className="max-w-xl mx-auto px-4 py-16 text-center space-y-4">
        <AlertCircle className="w-12 h-12 text-rose-500 mx-auto" />
        <h2 className="text-xl font-bold text-slate-900">Event Not Found</h2>
        <p className="text-sm text-slate-600">The event could not be found or you do not have permission.</p>
        <Link
          to="/organizer/dashboard"
          className="inline-flex items-center gap-2 text-sm font-semibold text-brand-600"
        >
          <ArrowLeft className="w-4 h-4" />
          Back to Organizer Dashboard
        </Link>
      </div>
    );
  }

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 md:py-12 space-y-8">
      {/* Back button */}
      <div>
        <Link
          to="/organizer/dashboard"
          className="inline-flex items-center gap-1.5 text-xs font-semibold text-slate-500 hover:text-slate-900 transition-colors"
        >
          <ArrowLeft className="w-4 h-4" />
          Back to My Events Dashboard
        </Link>
      </div>

      {/* Event Header Banner */}
      <div className="bg-white p-6 sm:p-8 rounded-3xl border border-slate-200/80 shadow-sm space-y-4">
        <div className="flex flex-wrap items-center justify-between gap-3">
          <div className="flex items-center gap-2">
            <EventStatusBadge status={event.status} />
            <SpotsLeftBadge
              spotsLeft={event.spotsLeft}
              isSoldOut={event.isSoldOut}
              waitlistCount={event.waitlistCount}
            />
          </div>
          <span className="text-xs text-slate-400">
            Created on {new Date(event.createdAt).toLocaleDateString()}
          </span>
        </div>

        <h1 className="text-2xl sm:text-3xl font-black text-slate-900 tracking-tight">
          {event.title}
        </h1>

        <div className="flex flex-wrap items-center gap-6 text-xs text-slate-600 pt-2 border-t border-slate-100">
          <div className="flex items-center gap-2">
            <Calendar className="w-4 h-4 text-slate-400" />
            <span>
              {new Date(event.startTime).toLocaleDateString(undefined, {
                weekday: 'short',
                month: 'short',
                day: 'numeric',
                hour: 'numeric',
                minute: '2-digit',
              })}
            </span>
          </div>

          <div className="flex items-center gap-2">
            <MapPin className="w-4 h-4 text-slate-400" />
            <span>{event.venue}</span>
          </div>

          <div className="flex items-center gap-2">
            <Users className="w-4 h-4 text-slate-400" />
            <span>
              {event.confirmedCount} / {event.capacity} Registered ({event.spotsLeft} remaining)
            </span>
          </div>
        </div>
      </div>

      {/* Tabs Navigation */}
      <div className="border-b border-slate-200 flex items-center space-x-2">
        <button
          onClick={() => handleTabChange('attendees')}
          className={`flex items-center gap-2 px-4 py-3 text-sm font-semibold border-b-2 transition-all ${
            activeTab === 'attendees'
              ? 'border-brand-600 text-brand-600 bg-brand-50/30'
              : 'border-transparent text-slate-500 hover:text-slate-800'
          }`}
        >
          <Users className="w-4 h-4" />
          Attendees Roster
        </button>

        <button
          onClick={() => handleTabChange('check-in')}
          className={`flex items-center gap-2 px-4 py-3 text-sm font-semibold border-b-2 transition-all ${
            activeTab === 'check-in'
              ? 'border-brand-600 text-brand-600 bg-brand-50/30'
              : 'border-transparent text-slate-500 hover:text-slate-800'
          }`}
        >
          <Camera className="w-4 h-4" />
          Door Check-In
        </button>

        <button
          onClick={() => handleTabChange('stats')}
          className={`flex items-center gap-2 px-4 py-3 text-sm font-semibold border-b-2 transition-all ${
            activeTab === 'stats'
              ? 'border-brand-600 text-brand-600 bg-brand-50/30'
              : 'border-transparent text-slate-500 hover:text-slate-800'
          }`}
        >
          <BarChart3 className="w-4 h-4" />
          Attendance Analytics
        </button>
      </div>

      {/* Active Tab Content */}
      <div>
        {activeTab === 'attendees' && <AttendeesTab event={event} />}
        {activeTab === 'check-in' && <CheckInTab event={event} />}
        {activeTab === 'stats' && <StatsTab event={event} />}
      </div>
    </div>
  );
};
