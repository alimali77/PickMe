import { ReactNode, useEffect, useState } from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import { useAuthStore } from '@/lib/auth-store';
import { authApi } from '@/modules/auth/auth-api';
import { Loader2 } from 'lucide-react';
import type { CurrentUserDto } from '@pickme/shared/api-types';

interface RequireAuthProps {
  role?: CurrentUserDto['role'];
  children: ReactNode;
}

export function RequireAuth({ role, children }: RequireAuthProps) {
  const accessToken = useAuthStore((s) => s.accessToken);
  const user = useAuthStore((s) => s.user);
  const setUser = useAuthStore((s) => s.setUser);
  const clear = useAuthStore((s) => s.clear);
  const location = useLocation();

  const [loading, setLoading] = useState(!!accessToken && !user);

  useEffect(() => {
    if (accessToken && !user) {
      setLoading(true);
      authApi
        .me()
        .then((u) => setUser(u))
        .catch(() => clear())
        .finally(() => setLoading(false));
    }
  }, [accessToken, user, setUser, clear]);

  if (!accessToken) {
    return <Navigate to={`/giris?next=${encodeURIComponent(location.pathname)}`} replace />;
  }

  if (loading) {
    return (
      <div className="flex items-center justify-center py-32 text-muted-foreground">
        <Loader2 className="mr-2 size-5 animate-spin" /> Yükleniyor...
      </div>
    );
  }

  if (!user) return null;
  if (role && user.role !== role) {
    return <Navigate to="/" replace />;
  }
  if (user.role === 'Driver' && user.mustChangePassword && !location.pathname.startsWith('/driver/sifre-degistir')) {
    return <Navigate to="/driver/sifre-degistir" replace />;
  }

  return <>{children}</>;
}
