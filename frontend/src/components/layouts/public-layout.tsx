import { Link, NavLink, Outlet } from 'react-router-dom';
import { Menu, X, CarFront } from 'lucide-react';
import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { WhatsAppFab } from '@/components/shared/whatsapp-fab';
import { CookieBanner } from '@/components/shared/cookie-banner';
import { cn } from '@/lib/utils';
import { useAuthStore } from '@/lib/auth-store';

const nav = [
  { to: '/', label: 'Ana Sayfa' },
  { to: '/hizmetler/sofor', label: 'Şoför Hizmeti' },
  { to: '/hizmetler/vale', label: 'Vale Hizmeti' },
  { to: '/hakkimizda', label: 'Hakkımızda' },
  { to: '/sss', label: 'SSS' },
  { to: '/iletisim', label: 'İletişim' },
];

export function PublicLayout() {
  const [open, setOpen] = useState(false);
  const isAuthed = useAuthStore((s) => !!s.accessToken);
  const user = useAuthStore((s) => s.user);

  return (
    <div className="flex min-h-screen flex-col bg-background">
      <header className="sticky top-0 z-30 border-b bg-background/80 backdrop-blur-md">
        <div className="container flex h-16 items-center justify-between gap-4">
          <Link to="/" className="flex items-center gap-2 font-bold" aria-label="Pick Me Ana Sayfa">
            <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-primary text-primary-foreground">
              <CarFront className="size-5" aria-hidden />
            </div>
            <span className="text-lg tracking-tight">Pick Me</span>
          </Link>

          <nav className="hidden md:flex items-center gap-1" aria-label="Ana menü">
            {nav.map((item) => (
              <NavLink
                key={item.to}
                to={item.to}
                end={item.to === '/'}
                className={({ isActive }) =>
                  cn(
                    'rounded-md px-3 py-2 text-sm font-medium transition-colors',
                    isActive ? 'text-foreground' : 'text-muted-foreground hover:text-foreground hover:bg-accent',
                  )
                }
              >
                {item.label}
              </NavLink>
            ))}
          </nav>

          <div className="hidden md:flex items-center gap-2">
            {isAuthed && user ? (
              <Button asChild variant="outline" size="sm">
                <Link to={user.role === 'Admin' ? '/admin' : user.role === 'Driver' ? '/driver' : '/hesabim'}>
                  {user.firstName || 'Hesabım'}
                </Link>
              </Button>
            ) : (
              <>
                <Button asChild variant="ghost" size="sm">
                  <Link to="/giris">Giriş Yap</Link>
                </Button>
                <Button asChild size="sm">
                  <Link to="/rezervasyon">Rezervasyon Yap</Link>
                </Button>
              </>
            )}
          </div>

          <button
            className="md:hidden inline-flex h-10 w-10 items-center justify-center rounded-md hover:bg-accent"
            aria-label={open ? 'Menüyü kapat' : 'Menüyü aç'}
            aria-expanded={open}
            onClick={() => setOpen((v) => !v)}
          >
            {open ? <X className="size-5" /> : <Menu className="size-5" />}
          </button>
        </div>

        {open ? (
          <div className="md:hidden border-t bg-background">
            <nav className="container flex flex-col py-3">
              {nav.map((item) => (
                <NavLink
                  key={item.to}
                  to={item.to}
                  end={item.to === '/'}
                  onClick={() => setOpen(false)}
                  className={({ isActive }) =>
                    cn(
                      'rounded-md px-3 py-3 text-sm font-medium',
                      isActive ? 'bg-accent text-foreground' : 'text-muted-foreground hover:bg-accent',
                    )
                  }
                >
                  {item.label}
                </NavLink>
              ))}
              <div className="mt-2 flex flex-col gap-2 border-t pt-3">
                {isAuthed && user ? (
                  <Button asChild>
                    <Link to={user.role === 'Admin' ? '/admin' : user.role === 'Driver' ? '/driver' : '/hesabim'}>
                      {user.firstName || 'Hesabım'}
                    </Link>
                  </Button>
                ) : (
                  <>
                    <Button asChild variant="outline">
                      <Link to="/giris">Giriş Yap</Link>
                    </Button>
                    <Button asChild>
                      <Link to="/rezervasyon">Rezervasyon Yap</Link>
                    </Button>
                  </>
                )}
              </div>
            </nav>
          </div>
        ) : null}
      </header>

      <main className="flex-1">
        <Outlet />
      </main>

      <footer className="border-t bg-muted/30">
        <div className="container py-10 grid grid-cols-2 md:grid-cols-4 gap-8 text-sm">
          <div className="col-span-2">
            <div className="flex items-center gap-2 font-bold mb-3">
              <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-primary text-primary-foreground">
                <CarFront className="size-4" aria-hidden />
              </div>
              Pick Me
            </div>
            <p className="text-muted-foreground max-w-sm">
              İstanbul'da 7/24 şoför ve vale hizmeti. Güvenli sürücüler, hızlı rezervasyon, şeffaf fiyat.
            </p>
          </div>
          <div>
            <div className="font-semibold mb-3">Hizmetler</div>
            <ul className="space-y-2 text-muted-foreground">
              <li><Link to="/hizmetler/sofor" className="hover:text-foreground">Şoför Hizmeti</Link></li>
              <li><Link to="/hizmetler/vale" className="hover:text-foreground">Vale Hizmeti</Link></li>
              <li><Link to="/rezervasyon" className="hover:text-foreground">Rezervasyon Yap</Link></li>
            </ul>
          </div>
          <div>
            <div className="font-semibold mb-3">Kurumsal</div>
            <ul className="space-y-2 text-muted-foreground">
              <li><Link to="/hakkimizda" className="hover:text-foreground">Hakkımızda</Link></li>
              <li><Link to="/sss" className="hover:text-foreground">SSS</Link></li>
              <li><Link to="/iletisim" className="hover:text-foreground">İletişim</Link></li>
              <li><Link to="/kvkk" className="hover:text-foreground">KVKK</Link></li>
              <li><Link to="/gizlilik" className="hover:text-foreground">Gizlilik</Link></li>
              <li><Link to="/kullanim-sartlari" className="hover:text-foreground">Kullanım Şartları</Link></li>
            </ul>
          </div>
        </div>
        <div className="border-t">
          <div className="container py-6 text-xs text-muted-foreground flex flex-col sm:flex-row sm:justify-between gap-2">
            <span>© {new Date().getFullYear()} Pick Me. Tüm hakları saklıdır.</span>
            <span>İstanbul, Türkiye</span>
          </div>
        </div>
      </footer>

      <WhatsAppFab />
      <CookieBanner />
    </div>
  );
}
