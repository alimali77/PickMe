import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom';
import {
  CalendarRange, LayoutDashboard, LogOut, Menu, ShieldCheck, X,
  Users, UserCog, HelpCircle, Star, Mail, Settings, Bell,
} from 'lucide-react';
import { useState } from 'react';
import { Button } from '@/components/ui/button';
import { useAuthStore } from '@/lib/auth-store';
import { authApi } from '@/modules/auth/auth-api';
import { cn } from '@/lib/utils';

const nav = [
  { to: '/admin', label: 'Dashboard', icon: LayoutDashboard, end: true },
  { to: '/admin/rezervasyonlar', label: 'Rezervasyonlar', icon: CalendarRange, end: false },
  { to: '/admin/soforler', label: 'Şoförler', icon: UserCog, end: false },
  { to: '/admin/musteriler', label: 'Müşteriler', icon: Users, end: false },
  { to: '/admin/degerlendirmeler', label: 'Değerlendirmeler', icon: Star, end: false },
  { to: '/admin/mesajlar', label: 'İletişim Mesajları', icon: Mail, end: false },
  { to: '/admin/sss', label: 'SSS', icon: HelpCircle, end: false },
  { to: '/admin/bildirim-alicilari', label: 'Bildirim E-postaları', icon: Bell, end: false },
  { to: '/admin/yoneticiler', label: 'Yöneticiler', icon: ShieldCheck, end: false },
  { to: '/admin/ayarlar', label: 'Sistem Ayarları', icon: Settings, end: false },
];

export function AdminLayout() {
  const navigate = useNavigate();
  const user = useAuthStore((s) => s.user);
  const refresh = useAuthStore((s) => s.refreshToken);
  const clear = useAuthStore((s) => s.clear);
  const [open, setOpen] = useState(false);

  const logout = async () => {
    if (refresh) await authApi.logout(refresh).catch(() => undefined);
    clear();
    navigate('/giris');
  };

  return (
    <div className="min-h-screen bg-muted/30">
      <header className="sticky top-0 z-30 border-b bg-background">
        <div className="container flex h-16 items-center justify-between">
          <Link to="/admin" className="flex items-center gap-2 font-bold">
            <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-primary text-primary-foreground">
              <ShieldCheck className="size-5" aria-hidden />
            </div>
            Pick Me <span className="text-xs font-normal text-muted-foreground ml-1">Admin</span>
          </Link>
          <div className="flex items-center gap-2">
            <span className="hidden sm:block text-sm">{user?.firstName}</span>
            <Button variant="ghost" size="sm" onClick={logout}>
              <LogOut className="size-4" /> <span className="hidden sm:inline">Çıkış</span>
            </Button>
            <button
              className="md:hidden inline-flex h-10 w-10 items-center justify-center rounded-md hover:bg-accent"
              onClick={() => setOpen((v) => !v)}
              aria-label="Menü"
              aria-expanded={open}
            >
              {open ? <X className="size-5" /> : <Menu className="size-5" />}
            </button>
          </div>
        </div>
      </header>

      <div className="container py-6 grid gap-6 md:grid-cols-[240px_1fr]">
        <aside className={cn('md:block', open ? 'block' : 'hidden')}>
          <nav className="flex flex-col gap-0.5 md:sticky md:top-20 max-h-[calc(100vh-6rem)] overflow-y-auto pr-2" aria-label="Admin menü">
            {nav.map((i) => (
              <NavLink
                key={i.to}
                to={i.to}
                end={i.end}
                onClick={() => setOpen(false)}
                className={({ isActive }) =>
                  cn(
                    'flex items-center gap-3 rounded-md px-3 py-2 text-sm font-medium transition-colors',
                    isActive ? 'bg-primary/10 text-primary' : 'text-muted-foreground hover:bg-accent hover:text-foreground',
                  )
                }
              >
                <i.icon className="size-4 shrink-0" aria-hidden />
                {i.label}
              </NavLink>
            ))}
          </nav>
        </aside>
        <main className="min-w-0"><Outlet /></main>
      </div>
    </div>
  );
}
