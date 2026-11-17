import React from 'react';
import { useQuery } from '@tanstack/react-query';
import {
  Users,
  CheckCircle2,
  Clock,
  Percent,
  Calendar,
  AlertCircle,
  TrendingUp,
} from 'lucide-react';
import {
  ResponsiveContainer,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  Tooltip,
  CartesianGrid,
  AreaChart,
  Area,
} from 'recharts';
import { checkInApi } from '../../../api/checkin';
import { EventItem } from '../../../types';
import { Skeleton } from '../../../components/common/Skeleton';

interface StatsTabProps {
  event: EventItem;
}

export const StatsTab: React.FC<StatsTabProps> = ({ event }) => {
  const { data: stats, isLoading, error } = useQuery({
    queryKey: ['eventStats', event.id],
    queryFn: () => checkInApi.getAttendanceStats(event.id),
  });

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="grid grid-cols-2 sm:grid-cols-5 gap-4">
          {Array.from({ length: 5 }).map((_, i) => (
            <Skeleton key={i} className="h-24 w-full rounded-2xl" />
          ))}
        </div>
        <Skeleton className="h-72 w-full rounded-2xl" />
        <Skeleton className="h-72 w-full rounded-2xl" />
      </div>
    );
  }

  if (error || !stats) {
    return (
      <div className="p-8 rounded-2xl bg-rose-50 border border-rose-200 text-center space-y-2">
        <AlertCircle className="w-8 h-8 text-rose-500 mx-auto" />
        <p className="text-sm font-semibold text-rose-900">Failed to load statistics</p>
        <p className="text-xs text-rose-700">{(error as Error)?.message || 'Statistics unavailable.'}</p>
      </div>
    );
  }

  // Format daily registration chart data
  const dailyData = stats.registrationsPerDay.map((d) => {
    const dateObj = new Date(d.date);
    return {
      date: dateObj.toLocaleDateString(undefined, { month: 'short', day: 'numeric' }),
      registrations: d.count,
    };
  });

  // Format interval check-in velocity chart data
  const intervalData = stats.checkInsPer15MinInterval.map((i) => {
    const timeObj = new Date(i.intervalStart);
    return {
      time: timeObj.toLocaleTimeString(undefined, { hour: 'numeric', minute: '2-digit' }),
      checkIns: i.count,
    };
  });

  return (
    <div className="space-y-8">
      {/* Summary KPI Tiles */}
      <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-5 gap-4">
        <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-sm space-y-2">
          <div className="flex items-center justify-between text-slate-400">
            <span className="text-xs font-semibold uppercase tracking-wider">Capacity</span>
            <Users className="w-4 h-4 text-brand-600" />
          </div>
          <div className="text-2xl font-black text-slate-900">{stats.capacity}</div>
          <p className="text-[11px] text-slate-500">Maximum allowed venue spots</p>
        </div>

        <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-sm space-y-2">
          <div className="flex items-center justify-between text-slate-400">
            <span className="text-xs font-semibold uppercase tracking-wider">Confirmed</span>
            <Calendar className="w-4 h-4 text-brand-600" />
          </div>
          <div className="text-2xl font-black text-brand-600">{stats.confirmedCount}</div>
          <p className="text-[11px] text-slate-500">Reserved active spots</p>
        </div>

        <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-sm space-y-2">
          <div className="flex items-center justify-between text-slate-400">
            <span className="text-xs font-semibold uppercase tracking-wider">Checked In</span>
            <CheckCircle2 className="w-4 h-4 text-emerald-600" />
          </div>
          <div className="text-2xl font-black text-emerald-600">{stats.checkedInCount}</div>
          <p className="text-[11px] text-slate-500">Scanned door attendance</p>
        </div>

        <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-sm space-y-2">
          <div className="flex items-center justify-between text-slate-400">
            <span className="text-xs font-semibold uppercase tracking-wider">Waitlist</span>
            <Clock className="w-4 h-4 text-amber-600" />
          </div>
          <div className="text-2xl font-black text-amber-600">{stats.waitlistCount}</div>
          <p className="text-[11px] text-slate-500">Queued for spot vacancy</p>
        </div>

        <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-sm space-y-2 col-span-2 sm:col-span-1">
          <div className="flex items-center justify-between text-slate-400">
            <span className="text-xs font-semibold uppercase tracking-wider">No-Show Rate</span>
            <Percent className="w-4 h-4 text-rose-600" />
          </div>
          <div className="text-2xl font-black text-slate-900">{stats.noShowRate}%</div>
          <p className="text-[11px] text-slate-500">
            {stats.confirmedCount} unverified / {stats.confirmedCount + stats.checkedInCount} total
          </p>
        </div>
      </div>

      {/* Recharts Analytics Charts */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* Registrations Over Time */}
        <div className="bg-white p-6 rounded-2xl border border-slate-200/80 shadow-sm space-y-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <TrendingUp className="w-5 h-5 text-brand-600" />
              <div>
                <h3 className="text-sm font-bold text-slate-900">Registrations Over Time</h3>
                <p className="text-xs text-slate-500">Signups recorded per day</p>
              </div>
            </div>
            <span className="text-xs font-semibold text-brand-600 bg-brand-50 px-2 py-1 rounded-md">
              Daily Velocity
            </span>
          </div>

          <div className="h-64 w-full pt-4">
            {dailyData.length === 0 ? (
              <div className="h-full flex items-center justify-center text-xs text-slate-400">
                No registration history available yet.
              </div>
            ) : (
              <ResponsiveContainer width="100%" height="100%">
                <AreaChart data={dailyData}>
                  <defs>
                    <linearGradient id="colorReg" x1="0" y1="0" x2="0" y2="1">
                      <stop offset="5%" stopColor="#4f46e5" stopOpacity={0.4} />
                      <stop offset="95%" stopColor="#4f46e5" stopOpacity={0} />
                    </linearGradient>
                  </defs>
                  <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#f1f5f9" />
                  <XAxis dataKey="date" tick={{ fontSize: 11, fill: '#64748b' }} />
                  <YAxis allowDecimals={false} tick={{ fontSize: 11, fill: '#64748b' }} />
                  <Tooltip
                    contentStyle={{
                      backgroundColor: '#ffffff',
                      borderRadius: '12px',
                      border: '1px solid #e2e8f0',
                      fontSize: '12px',
                    }}
                  />
                  <Area
                    type="monotone"
                    dataKey="registrations"
                    stroke="#4f46e5"
                    strokeWidth={2}
                    fillOpacity={1}
                    fill="url(#colorReg)"
                  />
                </AreaChart>
              </ResponsiveContainer>
            )}
          </div>
        </div>

        {/* Check-ins per 15-Minute Interval */}
        <div className="bg-white p-6 rounded-2xl border border-slate-200/80 shadow-sm space-y-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <CheckCircle2 className="w-5 h-5 text-emerald-600" />
              <div>
                <h3 className="text-sm font-bold text-slate-900">Door Check-In Velocity</h3>
                <p className="text-xs text-slate-500">Arrivals grouped by 15-minute intervals</p>
              </div>
            </div>
            <span className="text-xs font-semibold text-emerald-700 bg-emerald-50 px-2 py-1 rounded-md">
              15-Min Buckets (SQL)
            </span>
          </div>

          <div className="h-64 w-full pt-4">
            {intervalData.length === 0 ? (
              <div className="h-full flex items-center justify-center text-xs text-slate-400">
                No check-in scans recorded yet.
              </div>
            ) : (
              <ResponsiveContainer width="100%" height="100%">
                <BarChart data={intervalData}>
                  <CartesianGrid strokeDasharray="3 3" vertical={false} stroke="#f1f5f9" />
                  <XAxis dataKey="time" tick={{ fontSize: 11, fill: '#64748b' }} />
                  <YAxis allowDecimals={false} tick={{ fontSize: 11, fill: '#64748b' }} />
                  <Tooltip
                    contentStyle={{
                      backgroundColor: '#ffffff',
                      borderRadius: '12px',
                      border: '1px solid #e2e8f0',
                      fontSize: '12px',
                    }}
                  />
                  <Bar dataKey="checkIns" fill="#10b981" radius={[6, 6, 0, 0]} />
                </BarChart>
              </ResponsiveContainer>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};
