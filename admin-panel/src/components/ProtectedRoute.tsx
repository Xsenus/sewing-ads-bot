import React from 'react';
import { Navigate } from 'react-router-dom';
import { isAuthed } from '../auth';

/**
 * Обёртка для защищённых страниц.
 */
export default function ProtectedRoute({ children }: { children: React.ReactNode }) {
  if (!isAuthed()) {
    return <Navigate to="/login" replace />;
  }
  return <>{children}</>;
}
