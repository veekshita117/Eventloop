import React from 'react';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AuthProvider } from './context/AuthContext';
import { ToastProvider } from './context/ToastContext';
import { Navbar } from './components/layout/Navbar';
import { Footer } from './components/layout/Footer';
import { ProtectedRoute } from './components/layout/ProtectedRoute';

import { LoginPage } from './features/auth/LoginPage';
import { RegisterPage } from './features/auth/RegisterPage';
import { EventsListPage } from './features/events/EventsListPage';
import { EventDetailsPage } from './features/events/EventDetailsPage';
import { MyTicketsPage } from './features/tickets/MyTicketsPage';
import { OrganizerDashboardPage } from './features/organizer/OrganizerDashboardPage';
import { EventManagePage } from './features/organizer/EventManagePage';

const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      retry: 1,
      refetchOnWindowFocus: false,
      staleTime: 1000 * 30, // 30 seconds
    },
  },
});

export const App: React.FC = () => {
  return (
    <QueryClientProvider client={queryClient}>
      <AuthProvider>
        <ToastProvider>
          <BrowserRouter>
            <div className="flex flex-col min-h-screen">
              <Navbar />
              <main className="flex-1">
                <Routes>
                  {/* Public routes */}
                  <Route path="/" element={<Navigate to="/events" replace />} />
                  <Route path="/events" element={<EventsListPage />} />
                  <Route path="/events/:id" element={<EventDetailsPage />} />
                  <Route path="/login" element={<LoginPage />} />
                  <Route path="/register" element={<RegisterPage />} />

                  {/* Student authenticated routes */}
                  <Route
                    path="/my-tickets"
                    element={
                      <ProtectedRoute>
                        <MyTicketsPage />
                      </ProtectedRoute>
                    }
                  />

                  {/* Organizer / Admin routes */}
                  <Route
                    path="/organizer/dashboard"
                    element={
                      <ProtectedRoute allowedRoles={['Organizer', 'Admin']}>
                        <OrganizerDashboardPage />
                      </ProtectedRoute>
                    }
                  />
                  <Route
                    path="/organizer/events/:id"
                    element={
                      <ProtectedRoute allowedRoles={['Organizer', 'Admin']}>
                        <EventManagePage />
                      </ProtectedRoute>
                    }
                  />

                  {/* Catch all */}
                  <Route path="*" element={<Navigate to="/events" replace />} />
                </Routes>
              </main>
              <Footer />
            </div>
          </BrowserRouter>
        </ToastProvider>
      </AuthProvider>
    </QueryClientProvider>
  );
};

export default App;
