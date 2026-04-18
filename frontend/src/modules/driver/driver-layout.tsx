import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom';
import { ClipboardList, LogOut, User, CarFront } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { useAuthStore } from '@/lib/auth-store';
import { authApi } from '@/modules/auth/auth-api';
import { cn } from '@/lib/utils';

export function DriverLayout() {
  const navigate = useNavigate();
  const user = useAuthStore((s) => s.user);
  const clear = useAuthStore((s) => s.clear);
  const refresh = useAuthStore((s) => s.refreshToken);

  const handleLogout = async () => {
    if (refresh) await authApi.logout(refresh).catch(() => undefined);
    clear();
    navigate('/giris');
  };

  return (
    <div className="min-h-screen bg-muted/30 pb-16 md:pb-0">
      <header className="border-b bg-background sticky top-0 z-20">
        <div className="container flex h-14 items-center justify-between">
          <Link to="/driver" className="flex items-center gap-2 font-bold">
            <div className="flex h-8 w-8 items-center justify-center rounded-lg bg-primary text-primary-foreground">
              <CarFront className="size-4" aria-hidden />
            </div>
            Pick Me <span className="text-xs font-normal text-muted-foreground ml-1">Şoför</span>
          </Link>
          <div className="flex items-center gap-3">
            <span className="hidden sm:block text-sm">{user?.firstName}</span>
            <Button variant="ghost" size="sm" onClick={handleLogout} aria-label="Çıkış">
              <LogOut className="size-4" />
              <span className="hidden sm:inline">Çıkış</span>
            </Button>
          </div>
        </div>
      </header>

      <main className="container py-6">
        <Outlet />
      </main>

      <nav className="md:hidden fixed bottom-0 left-0 right-0 z-20 border-t bg-background" aria-label="Alt menü">
        <div className="grid grid-cols-2">
          <NavLink
            to="/driver"
            end
            className={({ isActive }) =>
              cn('flex flex-col items-center justify-center py-3 text-xs font-medium',
                isActive ? 'text-primary' : 'text-muted-foreground')
            }
          >
            <ClipboardList className="size-5" aria-hidden />
            Görevler
          </NavLink>
          <NavLink
            to="/driver/profil"
            className={({ isActive }) =>
              cn('flex flex-col items-center justify-center py-3 text-xs font-medium',
                isActive ? 'text-primary' : 'text-muted-foreground')
            }
          >
            <User className="size-5" aria-hidden />
            Profil
          </NavLink>
        </div>
      </nav>
    </div>
  );
}
