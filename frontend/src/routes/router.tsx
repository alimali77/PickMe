import { createBrowserRouter, Navigate } from 'react-router-dom';
import { PublicLayout } from '@/components/layouts/public-layout';
import { RequireAuth } from '@/components/shared/require-auth';

import { HomePage } from '@/pages/home-page';
import { ServicePage } from '@/pages/service-pages';
import { AboutPage } from '@/pages/about-page';
import { FaqPage } from '@/pages/faq-page';
import { ContactPage } from '@/pages/contact-page';
import { LegalPage } from '@/pages/legal-pages';
import { NotFoundPage } from '@/pages/not-found-page';

import { LoginPage } from '@/modules/auth/login-page';
import { RegisterPage } from '@/modules/auth/register-page';
import { VerifyEmailPage } from '@/modules/auth/verify-email-page';
import { ForgotPasswordPage, ResetPasswordPage } from '@/modules/auth/forgot-reset-pages';

import { ReservationFormPage } from '@/modules/reservations/reservation-form';

import { CustomerLayout } from '@/modules/customer/customer-layout';
import { MyReservationsPage } from '@/modules/customer/my-reservations-page';
import { ReservationDetailPage } from '@/modules/customer/reservation-detail-page';
import { ProfilePage } from '@/modules/customer/profile-page';

import { DriverLayout } from '@/modules/driver/driver-layout';
import { DriverTasksPage } from '@/modules/driver/driver-tasks-page';
import { DriverTaskDetailPage } from '@/modules/driver/driver-task-detail-page';
import { DriverChangePasswordPage } from '@/modules/driver/driver-change-password-page';

import { AdminLayout } from '@/modules/admin/admin-layout';
import { AdminDashboardPage } from '@/modules/admin/admin-dashboard-page';
import { AdminReservationsPage } from '@/modules/admin/admin-reservations-page';
import { AdminReservationDetailPage } from '@/modules/admin/admin-reservation-detail-page';
import { AdminDriversPage } from '@/modules/admin/admin-drivers-page';
import { AdminCustomersPage } from '@/modules/admin/admin-customers-page';
import { AdminRatingsPage } from '@/modules/admin/admin-ratings-page';
import { AdminContactMessagesPage } from '@/modules/admin/admin-contact-messages-page';
import { AdminFaqsPage } from '@/modules/admin/admin-faqs-page';
import { AdminRecipientsPage } from '@/modules/admin/admin-recipients-page';
import { AdminAdminsPage } from '@/modules/admin/admin-admins-page';
import { AdminSettingsPage } from '@/modules/admin/admin-settings-page';

export const router = createBrowserRouter([
  // --- Public ---
  {
    element: <PublicLayout />,
    children: [
      { path: '/', element: <HomePage /> },
      { path: '/hizmetler/sofor', element: <ServicePage type="sofor" /> },
      { path: '/hizmetler/vale', element: <ServicePage type="vale" /> },
      { path: '/hakkimizda', element: <AboutPage /> },
      { path: '/sss', element: <FaqPage /> },
      { path: '/iletisim', element: <ContactPage /> },
      { path: '/kvkk', element: <LegalPage kind="kvkk" /> },
      { path: '/gizlilik', element: <LegalPage kind="gizlilik" /> },
      { path: '/kullanim-sartlari', element: <LegalPage kind="kullanim-sartlari" /> },
      { path: '/giris', element: <LoginPage /> },
      { path: '/kayit', element: <RegisterPage /> },
      { path: '/eposta-dogrula', element: <VerifyEmailPage /> },
      { path: '/sifremi-unuttum', element: <ForgotPasswordPage /> },
      { path: '/sifre-sifirla', element: <ResetPasswordPage /> },
      {
        path: '/rezervasyon',
        element: (
          <RequireAuth role="Customer">
            <ReservationFormPage />
          </RequireAuth>
        ),
      },
      { path: '*', element: <NotFoundPage /> },
    ],
  },

  // --- Customer portal ---
  {
    path: '/hesabim',
    element: (
      <RequireAuth role="Customer">
        <CustomerLayout />
      </RequireAuth>
    ),
    children: [
      { index: true, element: <Navigate to="/hesabim/rezervasyonlar" replace /> },
      { path: 'rezervasyonlar', element: <MyReservationsPage /> },
      { path: 'rezervasyonlar/:id', element: <ReservationDetailPage /> },
      { path: 'profil', element: <ProfilePage /> },
    ],
  },

  // --- Driver panel ---
  {
    path: '/driver',
    element: (
      <RequireAuth role="Driver">
        <DriverLayout />
      </RequireAuth>
    ),
    children: [
      { index: true, element: <DriverTasksPage /> },
      { path: 'gorevler/:id', element: <DriverTaskDetailPage /> },
      { path: 'sifre-degistir', element: <DriverChangePasswordPage /> },
      { path: 'profil', element: <DriverTasksPage /> },
    ],
  },

  // --- Admin panel ---
  {
    path: '/admin',
    element: (
      <RequireAuth role="Admin">
        <AdminLayout />
      </RequireAuth>
    ),
    children: [
      { index: true, element: <AdminDashboardPage /> },
      { path: 'rezervasyonlar', element: <AdminReservationsPage /> },
      { path: 'rezervasyonlar/:id', element: <AdminReservationDetailPage /> },
      { path: 'soforler', element: <AdminDriversPage /> },
      { path: 'musteriler', element: <AdminCustomersPage /> },
      { path: 'degerlendirmeler', element: <AdminRatingsPage /> },
      { path: 'mesajlar', element: <AdminContactMessagesPage /> },
      { path: 'sss', element: <AdminFaqsPage /> },
      { path: 'bildirim-alicilari', element: <AdminRecipientsPage /> },
      { path: 'yoneticiler', element: <AdminAdminsPage /> },
      { path: 'ayarlar', element: <AdminSettingsPage /> },
    ],
  },
]);
