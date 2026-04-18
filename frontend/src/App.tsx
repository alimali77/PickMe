import { RouterProvider } from 'react-router-dom';
import { QueryClientProvider } from '@tanstack/react-query';
import { HelmetProvider } from 'react-helmet-async';
import { Toaster } from 'sonner';
import { router } from '@/routes/router';
import { queryClient } from '@/lib/query-client';

export default function App() {
  return (
    <HelmetProvider>
      <QueryClientProvider client={queryClient}>
        <RouterProvider router={router} />
        <Toaster richColors position="top-right" closeButton />
      </QueryClientProvider>
    </HelmetProvider>
  );
}
