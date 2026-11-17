import React, { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Download, Search, Users, AlertCircle } from 'lucide-react';
import { registrationsApi } from '../../../api/registrations';
import { checkInApi } from '../../../api/checkin';
import { EventItem, RegistrationStatus } from '../../../types';
import { RegistrationStatusBadge } from '../../../components/common/Badge';
import { Button } from '../../../components/common/Button';
import { Input } from '../../../components/common/Input';
import { TableSkeleton } from '../../../components/common/Skeleton';
import { EmptyState } from '../../../components/common/EmptyState';
import { useToast } from '../../../context/ToastContext';

interface AttendeesTabProps {
  event: EventItem;
}

export const AttendeesTab: React.FC<AttendeesTabProps> = ({ event }) => {
  const { error: toastError, success: toastSuccess } = useToast();
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState<string>('All');
  const [isExporting, setIsExporting] = useState(false);

  const { data, isLoading, error, refetch } = useQuery({
    queryKey: ['eventRegistrations', event.id, searchTerm, statusFilter],
    queryFn: () =>
      registrationsApi.getEventRegistrations(event.id, {
        searchTerm: searchTerm.trim() || undefined,
        status: statusFilter === 'All' ? undefined : (statusFilter as RegistrationStatus),
        pageSize: 100,
      }),
  });

  const handleExportCsv = async () => {
    setIsExporting(true);
    try {
      await checkInApi.downloadAttendeesCsv(event.id, event.title);
      toastSuccess('Attendee roster exported successfully.');
    } catch (err: any) {
      toastError(err.message || 'Failed to export CSV.');
    } finally {
      setIsExporting(false);
    }
  };

  return (
    <div className="space-y-6">
      {/* Search, Filter & CSV Export Header */}
      <div className="flex flex-col sm:flex-row items-stretch sm:items-center justify-between gap-4 bg-white p-4 rounded-2xl border border-slate-200/80 shadow-sm">
        <div className="flex flex-col sm:flex-row items-center gap-3 flex-1">
          <div className="relative w-full sm:max-w-xs">
            <Input
              placeholder="Search by name or email..."
              value={searchTerm}
              onChange={(e) => setSearchTerm(e.target.value)}
              className="pl-9"
            />
            <Search className="w-4 h-4 text-slate-400 absolute left-3 top-2.5 pointer-events-none" />
          </div>

          <select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value)}
            className="w-full sm:w-auto rounded-xl border border-slate-300 px-3 py-2 text-sm bg-white focus:outline-none focus:ring-2 focus:ring-brand-100 focus:border-brand-500 font-medium text-slate-700"
          >
            <option value="All">All Statuses</option>
            <option value="Confirmed">Confirmed</option>
            <option value="CheckedIn">Checked In</option>
            <option value="Waitlisted">Waitlisted</option>
            <option value="Cancelled">Cancelled</option>
          </select>
        </div>

        <Button
          variant="outline"
          size="md"
          icon={<Download className="w-4 h-4" />}
          isLoading={isExporting}
          onClick={handleExportCsv}
        >
          Export CSV Roster
        </Button>
      </div>

      {/* Table view */}
      {isLoading ? (
        <TableSkeleton rows={6} />
      ) : error ? (
        <div className="p-8 rounded-2xl bg-rose-50 border border-rose-200 text-center space-y-2">
          <AlertCircle className="w-8 h-8 text-rose-500 mx-auto" />
          <p className="text-sm font-semibold text-rose-900">Failed to load attendees</p>
          <p className="text-xs text-rose-700">{(error as Error).message}</p>
          <button
            onClick={() => refetch()}
            className="px-3 py-1.5 bg-rose-600 text-white rounded-lg text-xs font-medium"
          >
            Retry
          </button>
        </div>
      ) : data?.items.length === 0 ? (
        <EmptyState
          icon={Users}
          title="No attendees found"
          description="No registrations match your search criteria or status filter."
          actionText={searchTerm || statusFilter !== 'All' ? 'Clear Filters' : undefined}
          onAction={() => {
            setSearchTerm('');
            setStatusFilter('All');
          }}
        />
      ) : (
        <div className="bg-white rounded-2xl border border-slate-200/80 shadow-sm overflow-hidden">
          <div className="overflow-x-auto">
            <table className="w-full text-left text-sm text-slate-600">
              <thead className="bg-slate-50 border-b border-slate-200 text-xs font-semibold text-slate-500 uppercase tracking-wider">
                <tr>
                  <th className="px-6 py-3.5">Attendee</th>
                  <th className="px-6 py-3.5">Email</th>
                  <th className="px-6 py-3.5">Status</th>
                  <th className="px-6 py-3.5">Queue #</th>
                  <th className="px-6 py-3.5">Registered</th>
                  <th className="px-6 py-3.5">Checked In</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {data?.items.map((reg) => (
                  <tr key={reg.id} className="hover:bg-slate-50/50 transition-colors">
                    <td className="px-6 py-4 font-semibold text-slate-900">{reg.userName}</td>
                    <td className="px-6 py-4 text-slate-500 text-xs">{reg.userEmail}</td>
                    <td className="px-6 py-4">
                      <RegistrationStatusBadge status={reg.status} />
                    </td>
                    <td className="px-6 py-4 font-mono text-xs text-slate-600">
                      {reg.waitlistPosition != null ? `#${reg.waitlistPosition}` : '—'}
                    </td>
                    <td className="px-6 py-4 text-xs text-slate-500">
                      {new Date(reg.registeredAt).toLocaleString(undefined, {
                        month: 'short',
                        day: 'numeric',
                        hour: 'numeric',
                        minute: '2-digit',
                      })}
                    </td>
                    <td className="px-6 py-4 text-xs text-slate-500">
                      {reg.checkedInAt ? (
                        <span className="text-emerald-700 font-medium">
                          {new Date(reg.checkedInAt).toLocaleTimeString()}
                        </span>
                      ) : (
                        <span className="text-slate-400">—</span>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div className="px-6 py-3 bg-slate-50 border-t border-slate-200 text-xs text-slate-500 flex justify-between items-center">
            <span>Showing {data?.items.length} of {data?.totalCount} attendees</span>
          </div>
        </div>
      )}
    </div>
  );
};
