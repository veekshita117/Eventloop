import React, { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Search, Calendar, Sparkles, AlertCircle } from 'lucide-react';
import { eventsApi } from '../../api/events';
import { EventCard } from './components/EventCard';
import { EventCardSkeleton } from '../../components/common/Skeleton';
import { EmptyState } from '../../components/common/EmptyState';
import { Input } from '../../components/common/Input';

export const EventsListPage: React.FC = () => {
  const [searchTerm, setSearchTerm] = useState('');
  const [fromDate, setFromDate] = useState('');
  const [toDate, setToDate] = useState('');
  const [hasSpots, setHasSpots] = useState(false);

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['events', { searchTerm, fromDate, toDate, hasSpots }],
    queryFn: () =>
      eventsApi.getPublishedEvents({
        searchTerm: searchTerm.trim() || undefined,
        fromDate: fromDate || undefined,
        toDate: toDate || undefined,
        hasSpots: hasSpots ? true : undefined,
        page: 1,
        pageSize: 50,
      }),
  });

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8 md:py-12 space-y-8">
      {/* Hero Header */}
      <div className="text-center max-w-3xl mx-auto space-y-3">
        <div className="inline-flex items-center gap-1.5 px-3 py-1 rounded-full bg-brand-50 border border-brand-100 text-brand-700 text-xs font-semibold">
          <Sparkles className="w-3.5 h-3.5" />
          Campus Life & Student Clubs
        </div>
        <h1 className="text-3xl sm:text-4xl font-extrabold tracking-tight text-slate-900">
          Discover & Join Campus Events
        </h1>
        <p className="text-base text-slate-600">
          Reserve your spot, get instant QR tickets, or join the automated waitlist when events fill up.
        </p>
      </div>

      {/* Filter and Search Bar */}
      <div className="bg-white p-4 sm:p-5 rounded-2xl border border-slate-200/80 shadow-sm space-y-4">
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
          <div className="relative">
            <Input
              placeholder="Search by event title..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="pl-9"
            />
            <Search className="w-4 h-4 text-slate-400 absolute left-3 top-2.5 pointer-events-none" />
          </div>

          <div>
            <Input
              type="date"
              placeholder="From Date"
              value={fromDate}
              onChange={(e) => setFromDate(e.target.value)}
            />
          </div>

          <div>
            <Input
              type="date"
              placeholder="To Date"
              value={toDate}
              onChange={(e) => setToDate(e.target.value)}
            />
          </div>

          <div className="flex items-center px-2">
            <label className="flex items-center gap-2 cursor-pointer select-none text-sm font-medium text-slate-700">
              <input
                type="checkbox"
                checked={hasSpots}
                onChange={(e) => setHasSpots(e.target.checked)}
                className="w-4 h-4 text-brand-600 border-slate-300 rounded focus:ring-brand-500"
              />
              <span>Available spots only</span>
            </label>
          </div>
        </div>
      </div>

      {/* Events Grid */}
      {isLoading ? (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {Array.from({ length: 6 }).map((_, i) => (
            <EventCardSkeleton key={i} />
          ))}
        </div>
      ) : error ? (
        <div className="p-8 rounded-2xl bg-rose-50 border border-rose-200 text-center space-y-3">
          <AlertCircle className="w-8 h-8 text-rose-600 mx-auto" />
          <h3 className="font-semibold text-rose-900">Failed to load events</h3>
          <p className="text-sm text-rose-700">{(error as Error).message}</p>
          <button
            onClick={() => refetch()}
            className="px-4 py-2 bg-rose-600 text-white rounded-xl text-xs font-semibold hover:bg-rose-700"
          >
            Retry
          </button>
        </div>
      ) : data?.items.length === 0 ? (
        <EmptyState
          icon={Calendar}
          title="No events found"
          description="Try adjusting your search criteria or date filters to find upcoming campus events."
          actionText="Clear Filters"
          onAction={() => {
            setSearchTerm('');
            setFromDate('');
            setToDate('');
            setHasSpots(false);
          }}
        />
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {data?.items.map((event) => (
            <EventCard key={event.id} event={event} />
          ))}
        </div>
      )}
    </div>
  );
};
