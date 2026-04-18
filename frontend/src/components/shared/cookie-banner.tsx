import { useEffect, useState } from 'react';
import { Button } from '@/components/ui/button';
import { Link } from 'react-router-dom';
import { cn } from '@/lib/utils';

const STORAGE_KEY = 'pickme-cookie-consent';

type Consent = 'granted' | 'denied' | null;

export function CookieBanner() {
  const [consent, setConsent] = useState<Consent>(null);

  useEffect(() => {
    const existing = localStorage.getItem(STORAGE_KEY) as Consent;
    setConsent(existing ?? null);
  }, []);

  if (consent !== null) return null;

  const decide = (value: Exclude<Consent, null>) => {
    localStorage.setItem(STORAGE_KEY, value);
    setConsent(value);
    const w = window as typeof window & { gtag?: (...args: unknown[]) => void };
    w.gtag?.('consent', 'update', {
      analytics_storage: value,
      ad_storage: 'denied',
    });
  };

  return (
    <div
      role="dialog"
      aria-label="Çerez tercihleri"
      className={cn(
        'fixed bottom-4 left-4 right-4 z-40 mx-auto max-w-3xl rounded-xl border bg-card p-4 shadow-elevated',
        'sm:bottom-6 sm:left-auto sm:right-6 sm:max-w-md md:p-5 animate-fade-in',
      )}
    >
      <p className="text-sm leading-relaxed">
        Pick Me size daha iyi bir deneyim sunmak için çerezler kullanır. Detaylar için{' '}
        <Link to="/kvkk" className="font-medium text-primary underline-offset-2 hover:underline">
          KVKK aydınlatma metnini
        </Link>{' '}
        inceleyebilirsiniz.
      </p>
      <div className="mt-3 flex flex-col-reverse gap-2 sm:flex-row sm:justify-end">
        <Button variant="outline" size="sm" onClick={() => decide('denied')}>
          Sadece Zorunlu
        </Button>
        <Button size="sm" onClick={() => decide('granted')}>
          Tamam, Anladım
        </Button>
      </div>
    </div>
  );
}
