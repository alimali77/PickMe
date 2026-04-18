import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { MapPin, Calendar, ChevronRight, Inbox } from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
import { StatusBadge } from '@/components/shared/status-badge';
import { reservationsApi } from '@/modules/reservations/reservations-api';
import { SERVICE_TYPE_LABELS } from '@pickme/shared/constants';
import { formatDate } from '@/lib/utils';

export function DriverTasksPage() {
  const { data, isLoading } = useQuery({
    queryKey: ['driver-tasks'],
    queryFn: () => reservationsApi.driverTasks(),
    refetchInterval: 60_000, // Yeni görevler için 1 dk'da bir yenile
  });

  return (
    <div className="space-y-4">
      <h1 className="text-2xl md:text-3xl font-bold tracking-tight">Görevlerim</h1>

      {isLoading ? (
        <div className="space-y-3">
          {[1, 2].map((k) => <div key={k} className="h-28 rounded-xl bg-card border animate-pulse" />)}
        </div>
      ) : data && data.items.length === 0 ? (
        <Card>
          <CardContent className="py-12 text-center">
            <Inbox className="mx-auto size-10 text-muted-foreground" />
            <h2 className="mt-3 font-semibold">Henüz atanmış görev yok</h2>
            <p className="mt-1 text-sm text-muted-foreground">Size atanan görevler burada görünecek.</p>
          </CardContent>
        </Card>
      ) : (
        <ul className="space-y-3">
          {data!.items.map((t) => (
            <li key={t.id}>
              <Link
                to={`/driver/gorevler/${t.id}`}
                className="flex items-center gap-4 rounded-xl border bg-card p-4 shadow-soft hover:shadow-elevated transition"
              >
                <div className="flex-1 min-w-0">
                  <div className="flex items-center gap-2 flex-wrap">
                    <StatusBadge status={t.status} />
                    <span className="text-xs text-muted-foreground">{SERVICE_TYPE_LABELS[t.serviceType]}</span>
                  </div>
                  <div className="mt-2 space-y-1 text-sm">
                    <div className="flex items-center gap-2"><Calendar className="size-4 text-muted-foreground" aria-hidden /><span>{formatDate(t.reservationDateTimeUtc)}</span></div>
                    <div className="flex items-start gap-2"><MapPin className="size-4 text-muted-foreground mt-0.5" aria-hidden /><span className="line-clamp-1">{t.address}</span></div>
                  </div>
                </div>
                <ChevronRight className="size-5 text-muted-foreground" aria-hidden />
              </Link>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
