import React from 'react';
import { Ticket } from 'lucide-react';

export const Footer: React.FC = () => {
  return (
    <footer className="bg-white border-t border-slate-200 mt-auto py-8">
      <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 flex flex-col sm:flex-row items-center justify-between gap-4 text-xs text-slate-500">
        <div className="flex items-center gap-2">
          <div className="w-5 h-5 rounded-md bg-brand-600 flex items-center justify-center text-white">
            <Ticket className="w-3 h-3 rotate-45" />
          </div>
          <span className="font-semibold text-slate-700">Eventloop</span>
          <span>&mdash; College Event Registration & Check-in System</span>
        </div>
        <p>&copy; {new Date().getFullYear()} Eventloop. All rights reserved.</p>
      </div>
    </footer>
  );
};
