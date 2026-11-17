import React from 'react';
import { CheckCircle2, AlertCircle, XCircle, Clock, QrCode } from 'lucide-react';

export interface CheckInScanResult {
  success: boolean;
  message: string;
  attendeeName?: string;
  attendeeEmail?: string;
  checkedInAt?: string;
  ticketCode?: string;
  isDuplicate?: boolean;
}

interface CheckInResultPanelProps {
  result: CheckInScanResult | null;
}

export const CheckInResultPanel: React.FC<CheckInResultPanelProps> = ({ result }) => {
  if (!result) {
    return (
      <div className="p-8 rounded-2xl border-2 border-dashed border-slate-200 bg-slate-50/50 flex flex-col items-center justify-center text-center text-slate-400 space-y-2">
        <QrCode className="w-10 h-10 text-slate-300 animate-pulse" />
        <p className="text-sm font-medium text-slate-600">Ready to Scan</p>
        <p className="text-xs text-slate-400 max-w-xs">
          Point the camera at an attendee's QR ticket pass or enter the code manually.
        </p>
      </div>
    );
  }

  if (result.success) {
    return (
      <div
        data-testid="checkin-success-panel"
        className="p-6 rounded-2xl bg-emerald-50 border-2 border-emerald-300 text-emerald-950 flex flex-col sm:flex-row items-center sm:items-start gap-4 shadow-sm animate-in fade-in zoom-in-95 duration-150"
      >
        <div className="p-3 bg-emerald-600 text-white rounded-2xl flex-shrink-0 shadow-md">
          <CheckCircle2 className="w-8 h-8" />
        </div>
        <div className="flex-1 text-center sm:text-left space-y-1">
          <div className="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full bg-emerald-100 text-emerald-800 text-[11px] font-bold uppercase tracking-wider">
            Verified Admission
          </div>
          <h3 className="text-2xl font-extrabold tracking-tight text-emerald-900">
            Checked in: {result.attendeeName}
          </h3>
          {result.attendeeEmail && (
            <p className="text-sm font-medium text-emerald-800">{result.attendeeEmail}</p>
          )}
          {result.checkedInAt && (
            <p className="text-xs text-emerald-700 flex items-center justify-center sm:justify-start gap-1 pt-1">
              <Clock className="w-3.5 h-3.5" />
              <span>Timestamp: {new Date(result.checkedInAt).toLocaleTimeString()}</span>
            </p>
          )}
        </div>
      </div>
    );
  }

  // Error case (Duplicate, Wrong Event, Waitlist, Cancelled, Not Found)
  return (
    <div
      data-testid="checkin-error-panel"
      className="p-6 rounded-2xl bg-rose-50 border-2 border-rose-300 text-rose-950 flex flex-col sm:flex-row items-center sm:items-start gap-4 shadow-sm animate-in fade-in zoom-in-95 duration-150"
    >
      <div className="p-3 bg-rose-600 text-white rounded-2xl flex-shrink-0 shadow-md">
        {result.isDuplicate ? <AlertCircle className="w-8 h-8" /> : <XCircle className="w-8 h-8" />}
      </div>
      <div className="flex-1 text-center sm:text-left space-y-1">
        <div className="inline-flex items-center gap-1.5 px-2.5 py-0.5 rounded-full bg-rose-100 text-rose-800 text-[11px] font-bold uppercase tracking-wider">
          {result.isDuplicate ? 'Duplicate Check-In' : 'Admission Denied'}
        </div>
        <h3 className="text-xl font-bold tracking-tight text-rose-900">
          {result.isDuplicate ? 'Already Checked In' : 'Invalid Ticket'}
        </h3>
        <p className="text-sm font-medium text-rose-800 break-words">
          {result.message}
        </p>
        {result.ticketCode && (
          <p className="text-xs font-mono text-rose-600 pt-1">
            Scanned Ticket: {result.ticketCode}
          </p>
        )}
      </div>
    </div>
  );
};
