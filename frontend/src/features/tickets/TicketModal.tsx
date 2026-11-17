import React from 'react';
import { useQuery } from '@tanstack/react-query';
import { QRCodeSVG } from 'qrcode.react';
import { MapPin, Ticket, AlertCircle, Copy, Check } from 'lucide-react';
import { registrationsApi } from '../../api/registrations';
import { Modal } from '../../components/common/Modal';
import { RegistrationStatusBadge } from '../../components/common/Badge';

interface TicketModalProps {
  registrationId: string | null;
  onClose: () => void;
}

export const TicketModal: React.FC<TicketModalProps> = ({ registrationId, onClose }) => {
  const [copied, setCopied] = React.useState(false);

  const { data: ticket, isLoading, error } = useQuery({
    queryKey: ['ticket', registrationId],
    queryFn: () => registrationsApi.getTicketDetails(registrationId!),
    enabled: !!registrationId,
  });

  const handleCopyCode = () => {
    if (ticket) {
      navigator.clipboard.writeText(ticket.ticketCode);
      setCopied(true);
      setTimeout(() => setCopied(false), 2000);
    }
  };

  return (
    <Modal
      isOpen={!!registrationId}
      onClose={onClose}
      title="Event Ticket Pass"
      maxWidth="md"
    >
      {isLoading ? (
        <div className="py-12 text-center">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-brand-600 mx-auto"></div>
          <p className="mt-3 text-xs text-slate-500">Generating secure ticket pass...</p>
        </div>
      ) : error || !ticket ? (
        <div className="p-6 text-center space-y-2">
          <AlertCircle className="w-8 h-8 text-rose-500 mx-auto" />
          <h4 className="font-semibold text-slate-900">Failed to load ticket</h4>
          <p className="text-xs text-slate-500">{(error as Error)?.message || 'Ticket could not be retrieved.'}</p>
        </div>
      ) : (
        <div className="space-y-6">
          {/* Top ticket header */}
          <div className="text-center space-y-2">
            <div className="inline-block">
              <RegistrationStatusBadge status={ticket.status} />
            </div>
            <h3 className="text-xl font-bold text-slate-900 tracking-tight">
              {ticket.eventTitle}
            </h3>
            <p className="text-xs text-slate-500 flex items-center justify-center gap-1">
              <MapPin className="w-3.5 h-3.5 text-slate-400" />
              {ticket.eventVenue}
            </p>
          </div>

          {/* QR Code Container */}
          <div className="flex flex-col items-center justify-center p-6 bg-slate-50 rounded-2xl border border-slate-200">
            {ticket.status === 'Waitlisted' ? (
              <div className="text-center p-6 space-y-2">
                <Ticket className="w-12 h-12 text-amber-500 mx-auto" />
                <h4 className="font-bold text-slate-800 text-sm">Waitlisted Pass</h4>
                <p className="text-xs text-slate-500 max-w-xs">
                  Your ticket QR code will activate automatically once a spot opens and your position is confirmed.
                </p>
              </div>
            ) : (
              <>
                <div className="bg-white p-4 rounded-2xl shadow-sm border border-slate-200/80">
                  <QRCodeSVG
                    value={ticket.ticketCode}
                    size={200}
                    level="H"
                    includeMargin
                  />
                </div>
                <p className="text-[11px] text-slate-400 font-medium mt-3">
                  Scan at the entrance door to check in
                </p>
              </>
            )}
          </div>

          {/* Ticket Metadata details */}
          <div className="space-y-3 bg-white p-4 rounded-xl border border-slate-100 text-xs">
            <div className="flex justify-between items-center py-1 border-b border-slate-100">
              <span className="text-slate-400">Attendee</span>
              <span className="font-semibold text-slate-800">{ticket.attendeeName} ({ticket.attendeeEmail})</span>
            </div>
            <div className="flex justify-between items-center py-1 border-b border-slate-100">
              <span className="text-slate-400">Event Start</span>
              <span className="font-semibold text-slate-800">
                {new Date(ticket.eventStartTime).toLocaleString(undefined, {
                  month: 'short',
                  day: 'numeric',
                  hour: 'numeric',
                  minute: '2-digit',
                })}
              </span>
            </div>
            <div className="flex justify-between items-center py-1">
              <span className="text-slate-400">Ticket Code</span>
              <div className="flex items-center gap-1.5 font-mono text-[11px] text-slate-700">
                <span>{ticket.ticketCode}</span>
                <button
                  onClick={handleCopyCode}
                  className="p-1 hover:bg-slate-100 rounded text-slate-400 hover:text-slate-600 transition-colors"
                  title="Copy code"
                >
                  {copied ? <Check className="w-3.5 h-3.5 text-emerald-600" /> : <Copy className="w-3.5 h-3.5" />}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}
    </Modal>
  );
};
