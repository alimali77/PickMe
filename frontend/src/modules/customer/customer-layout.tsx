import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom';
import { CalendarRange, UserCog, CarFront, LogOut } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { cn } from '@/lib/utils';
import { useAuthStore } from '@/lib/auth-store';
import { authApi } from '@/modules/auth/auth-api';
import { WhatsAppFab } from '@/components/shared/whatsapp-fab';

const items = [
  { to: '/hesabim/rezervasyonlar', label: 'Rezervasyonlarım', icon: CalendarRange },
  { to: '/hesabim/profil', label: 'Hesap Bilgilerim', icon: UserCog },
];

export function CustomerLayout() {
  const navigate = useNavigate();
  const user = useAuthStore((s) => s.user);
  const clear = useAuthStore((s) => s.clear);
  const refresh = useAuthStore((s) => s.refreshToken);

  const handleLogout = async () => {
    if (refresh) await authApi.logout(refresh).catch(() => undefined);
    clear();
    navigate('/');
  };

  return (
    <div className="min-h-screen bg-muted/30">
      <header className="border-b bg-background">
        <div className="container flex h-16 items-center justify-between">
          <Link to="/" className="flex items-center gap-2 font-bold">
            <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-primary text-primary-foreground">
              <CarFront className="size-5" aria-hidden />
            </div>
            Pick Me
          </Link>
          <div className="flex items-center gap-3">
            <span className="hidden sm:block text-sm text-muted-foreground">
              {user ? `${user.firstName} ${user.lastName}` : null}
            </span>
            <Button variant="ghost" size="sm" onClick={handleLogout}>
              <LogOut className="size-4" /> Çıkış
            </Button>
          </div>
        </div>
      </header>

      <div className="container py-8 grid gap-6 md:grid-cols-[240px_1fr]">
        <aside>
          <nav className="flex md:flex-col gap-1 overflow-x-auto md:overflow-visible" aria-label="Hesap menüsü">
            <Button asChild variant="default" className="md:hidden">
              <Link to="/rezervasyon">Yeni Rezervasyon</Link>
            </Button>
            {items.map((i) => (
              <NavLink
                key={i.to}
                to={i.to}
                className={({ isActive }) =>
                  cn(
                    'flex items-center gap-2 rounded-md px-3 py-2 text-sm font-medium transition-colors whitespace-nowrap',
                    isActive ? 'bg-primary/10 text-primary' : 'text-muted-foreground hover:bg-accent hover:text-foreground',
                  )
                }
              >
                <i.icon className="size-4" aria-hidden />
                {i.label}
              </NavLink>
            ))}
            <Button asChild className="hidden md:flex mt-3">
              <Link to="/rezervasyon">+ Yeni Rezervasyon</Link>
            </Button>
          </nav>
        </aside>

        <main className="min-w-0">
          <Outlet />
        </main>
      </div>

      <WhatsAppFab />
    </div>
  );
}
