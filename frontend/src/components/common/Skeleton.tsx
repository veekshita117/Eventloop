import React from 'react';

export const Skeleton: React.FC<{ className?: string }> = ({ className = '' }) => (
  <div className={`animate-pulse bg-slate-200 rounded-lg ${className}`} />
);

export const EventCardSkeleton: React.FC = () => (
  <div className="bg-white rounded-2xl border border-slate-200/80 p-6 space-y-4 shadow-sm">
    <div className="flex justify-between items-start">
      <Skeleton className="h-6 w-3/4 rounded-md" />
      <Skeleton className="h-5 w-20 rounded-md" />
    </div>
    <Skeleton className="h-4 w-1/2 rounded-md" />
    <Skeleton className="h-16 w-full rounded-md" />
    <div className="pt-2 border-t border-slate-100 flex justify-between items-center">
      <Skeleton className="h-4 w-28 rounded-md" />
      <Skeleton className="h-8 w-24 rounded-xl" />
    </div>
  </div>
);

export const TableSkeleton: React.FC<{ rows?: number }> = ({ rows = 5 }) => (
  <div className="w-full space-y-3">
    <Skeleton className="h-10 w-full rounded-xl" />
    {Array.from({ length: rows }).map((_, i) => (
      <Skeleton key={i} className="h-12 w-full rounded-xl" />
    ))}
  </div>
);
