import React, { useState, useEffect, useRef } from 'react';
import { Html5Qrcode } from 'html5-qrcode';
import { Camera, CameraOff, Keyboard, Clock, RefreshCw } from 'lucide-react';
import { checkInApi } from '../../../api/checkin';
import { EventItem } from '../../../types';
import { Input } from '../../../components/common/Input';
import { Button } from '../../../components/common/Button';
import { CheckInResultPanel, CheckInScanResult } from '../components/CheckInResultPanel';
import { Badge } from '../../../components/common/Badge';

interface CheckInTabProps {
  event: EventItem;
}

interface ScanHistoryItem {
  id: string;
  ticketCode: string;
  attendeeName?: string;
  timestamp: string;
  success: boolean;
  message: string;
}

export const CheckInTab: React.FC<CheckInTabProps> = ({ event }) => {
  const [manualCode, setManualCode] = useState('');
  const [isScanning, setIsScanning] = useState(false);
  const [cameraError, setCameraError] = useState<string | null>(null);
  const [isProcessing, setIsProcessing] = useState(false);
  const [latestResult, setLatestResult] = useState<CheckInScanResult | null>(null);
  const [history, setHistory] = useState<ScanHistoryItem[]>([]);

  const html5QrCodeRef = useRef<Html5Qrcode | null>(null);
  const isCooldownRef = useRef(false);

  const handleCheckIn = async (rawCode: string) => {
    const cleanedCode = rawCode.trim();
    if (!cleanedCode || isCooldownRef.current) return;

    isCooldownRef.current = true;
    setIsProcessing(true);

    try {
      const response = await checkInApi.checkInTicket(event.id, cleanedCode);

      const result: CheckInScanResult = {
        success: true,
        message: response.message,
        attendeeName: response.attendeeName,
        attendeeEmail: response.attendeeEmail,
        checkedInAt: response.checkedInAt,
        ticketCode: cleanedCode,
      };

      setLatestResult(result);
      setHistory((prev) => [
        {
          id: Math.random().toString(),
          ticketCode: cleanedCode,
          attendeeName: response.attendeeName,
          timestamp: new Date().toLocaleTimeString(),
          success: true,
          message: 'Admission verified',
        },
        ...prev.slice(0, 9),
      ]);
      setManualCode('');
    } catch (err: any) {
      const isDuplicate = err.status === 409 || err.message?.toLowerCase().includes('already');

      const result: CheckInScanResult = {
        success: false,
        message: err.message || 'Ticket verification failed.',
        ticketCode: cleanedCode,
        isDuplicate,
      };

      setLatestResult(result);
      setHistory((prev) => [
        {
          id: Math.random().toString(),
          ticketCode: cleanedCode,
          timestamp: new Date().toLocaleTimeString(),
          success: false,
          message: err.message || 'Verification failed',
        },
        ...prev.slice(0, 9),
      ]);
    } finally {
      setIsProcessing(false);
      // Brief cooldown before next camera scan to prevent instant duplicate reads
      setTimeout(() => {
        isCooldownRef.current = false;
      }, 2000);
    }
  };

  const startScanner = async () => {
    setCameraError(null);
    try {
      const qrScanner = new Html5Qrcode('qr-reader');
      html5QrCodeRef.current = qrScanner;

      await qrScanner.start(
        { facingMode: 'environment' },
        {
          fps: 10,
          qrbox: { width: 250, height: 250 },
        },
        (decodedText) => {
          handleCheckIn(decodedText);
        },
        () => {
          // Frame error (no QR in view), safe to ignore
        }
      );

      setIsScanning(true);
    } catch (err: any) {
      setCameraError(
        err.message || 'Camera permission denied or camera not available on this device.'
      );
      setIsScanning(false);
    }
  };

  const stopScanner = async () => {
    if (html5QrCodeRef.current && isScanning) {
      try {
        await html5QrCodeRef.current.stop();
        html5QrCodeRef.current.clear();
      } catch {
        // Ignore cleanup errors
      }
      setIsScanning(false);
    }
  };

  useEffect(() => {
    return () => {
      if (html5QrCodeRef.current && html5QrCodeRef.current.isScanning) {
        html5QrCodeRef.current.stop().catch(() => {});
      }
    };
  }, []);

  const handleManualSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (manualCode.trim()) {
      handleCheckIn(manualCode.trim());
    }
  };

  return (
    <div className="space-y-6">
      {/* Top Scanner & Manual Input Section */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-6">
        {/* Left Column: Camera Scanner */}
        <div className="lg:col-span-6 bg-white p-6 rounded-2xl border border-slate-200/80 shadow-sm space-y-4">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <Camera className="w-5 h-5 text-brand-600" />
              <h3 className="font-bold text-slate-900 text-sm">Door Camera Scanner</h3>
            </div>
            <Button
              variant={isScanning ? 'outline' : 'primary'}
              size="sm"
              onClick={isScanning ? stopScanner : startScanner}
              icon={isScanning ? <CameraOff className="w-4 h-4" /> : <Camera className="w-4 h-4" />}
            >
              {isScanning ? 'Stop Camera' : 'Start Camera'}
            </Button>
          </div>

          {/* Scanner view port */}
          <div className="relative overflow-hidden rounded-xl border border-slate-200 bg-slate-900 min-h-[260px] flex items-center justify-center">
            <div id="qr-reader" className="w-full" />
            {!isScanning && (
              <div className="absolute inset-0 flex flex-col items-center justify-center text-slate-400 p-6 text-center space-y-2 bg-slate-900/90">
                <Camera className="w-10 h-10 text-slate-600" />
                <p className="text-sm font-medium text-slate-300">Camera Inactive</p>
                <p className="text-xs text-slate-500 max-w-xs">
                  Click &ldquo;Start Camera&rdquo; to use your device webcam to scan attendee QR passes.
                </p>
              </div>
            )}
          </div>

          {cameraError && (
            <p className="text-xs text-rose-600 bg-rose-50 p-2.5 rounded-xl border border-rose-200">
              {cameraError}
            </p>
          )}

          {/* Manual input fallback */}
          <div className="pt-4 border-t border-slate-100">
            <form onSubmit={handleManualSubmit} className="space-y-3">
              <div className="flex items-center gap-2 text-xs font-semibold text-slate-700">
                <Keyboard className="w-4 h-4 text-slate-400" />
                <span>Manual Ticket Code Fallback</span>
              </div>
              <div className="flex gap-2">
                <Input
                  placeholder="Paste or enter 36-char Ticket GUID..."
                  value={manualCode}
                  onChange={(e) => setManualCode(e.target.value)}
                  className="font-mono text-xs flex-1"
                />
                <Button
                  type="submit"
                  variant="secondary"
                  size="md"
                  isLoading={isProcessing}
                  disabled={!manualCode.trim()}
                >
                  Verify
                </Button>
              </div>
            </form>
          </div>
        </div>

        {/* Right Column: Large Clear Result Panel & Recent Scans */}
        <div className="lg:col-span-6 space-y-6">
          {/* Result Panel */}
          <div>
            <h3 className="font-bold text-slate-900 text-sm mb-3">Live Scan Result</h3>
            <CheckInResultPanel result={latestResult} />
          </div>

          {/* Recent Scans History */}
          <div className="bg-white p-5 rounded-2xl border border-slate-200/80 shadow-sm space-y-3">
            <div className="flex items-center justify-between border-b border-slate-100 pb-3">
              <div className="flex items-center gap-2">
                <Clock className="w-4 h-4 text-slate-400" />
                <h4 className="font-bold text-xs uppercase tracking-wider text-slate-500">
                  Recent Scans (Session History)
                </h4>
              </div>
              {history.length > 0 && (
                <button
                  onClick={() => setHistory([])}
                  className="text-[11px] text-slate-400 hover:text-slate-600 flex items-center gap-1"
                >
                  <RefreshCw className="w-3 h-3" />
                  Clear
                </button>
              )}
            </div>

            {history.length === 0 ? (
              <p className="text-xs text-slate-400 py-4 text-center">
                No tickets scanned yet this session.
              </p>
            ) : (
              <div className="divide-y divide-slate-100 max-h-60 overflow-y-auto space-y-1">
                {history.map((item) => (
                  <div key={item.id} className="py-2.5 flex items-center justify-between text-xs">
                    <div className="flex flex-col">
                      <span className="font-semibold text-slate-800">
                        {item.attendeeName || `Ticket: ${item.ticketCode.slice(0, 8)}...`}
                      </span>
                      <span className="text-[11px] text-slate-500">{item.message}</span>
                    </div>
                    <div className="flex items-center gap-2">
                      <span className="text-[10px] text-slate-400">{item.timestamp}</span>
                      <Badge variant={item.success ? 'emerald' : 'rose'} size="sm">
                        {item.success ? 'Success' : 'Denied'}
                      </Badge>
                    </div>
                  </div>
                ))}
              </div>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};
