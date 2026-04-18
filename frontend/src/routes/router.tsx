import { createBrowserRouter, Navigate } from 'react-router-dom';
import { PublicLayout } from '@/components/layouts/public-layout';
import { HomePage } from '@/pages/home-page';
import { ServicePage } from '@/pages/service-pages';
import { AboutPage } from '@/pages/about-page';
import { FaqPage } from '@/pages/faq-page';
import { ContactPage } from '@/pages/contact-page';
import { LegalPage } from '@/pages/legal-pages';
import { LoginPage } from '@/modules/auth/login-page';
import { RegisterPage } from '@/modules/auth/register-page';
import { VerifyEmailPage } from '@/modules/auth/verify-email-page';
import { ForgotPasswordPage, ResetPasswordPage } from '@/modules/auth/forgot-reset-pages';
import { NotFoundPage } from '@/pages/not-found-page';

export const router = createBrowserRouter([
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
          <div className="container py-20 text-center">
            <h1 className="text-3xl font-bold">Rezervasyon Formu</h1>
            <p className="mt-4 text-muted-foreground">Faz 4'te uygulanacak — Google Maps Autocomplete ile.</p>
          </div>
        ),
      },
      {
        path: '/hesabim',
        element: (
          <div className="container py-20 text-center">
            <h1 className="text-3xl font-bold">Hesabım</h1>
            <p className="mt-4 text-muted-foreground">Faz 4'te müşteri paneli uygulanacak.</p>
          </div>
        ),
      },
      { path: '/admin/*', element: <Navigate to="/giris" replace /> },
      { path: '/driver/*', element: <Navigate to="/giris" replace /> },
      { path: '*', element: <NotFoundPage /> },
    ],
  },
]);
