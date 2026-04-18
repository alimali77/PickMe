import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { Inbox, MapPin, Calendar, ChevronRight } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { StatusBadge } from '@/components/shared/status-badge';
import { reservationsApi } from '@/modules/reservations/reservations-api';
import { SERVICE_TYPE_LABELS } from '@pickme/shared/constants';
import { formatDate } from '@/lib/utils';

export function MyReservationsPage() {
  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['my-reservations'],
    queryFn: () => reservationsApi.listMine(),
  });

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h1 className="text-2xl md:text-3xl font-bold tracking-tight">Rezervasyonlarım</h1>
        <Button asChild size="sm" className="hidden md:flex">
          <Link to="/rezervasyon">Yeni Rezervasyon</Link>
        </Button>
      </div>

      {isLoading ? (
        <div className="space-y-3" aria-label="Yükleniyor">
          {[1, 2, 3].map((k) => (
            <div key={k} className="h-32 rounded-xl border bg-card animate-pulse" />
          ))}
        </div>
      ) : isError ? (
        <Card>
          <CardContent className="py-12 text-center">
            <p className="text-muted-foreground">Rezervasyonlar yüklenirken bir hata oluştu.</p>
            <Button onClick={() => refetch()} variant="outline" className="mt-4">Tekrar Dene</Button>
          </CardContent>
        </Card>
      ) : data && data.items.length === 0 ? (
        <Card>
          <CardContent className="py-16 text-center">
            <div className="mx-auto flex size-14 items-center justify-center rounded-full bg-muted">
              <Inbox className="size-7 text-muted-foreground" aria-hidden />
            </div>
            <h2 className="mt-4 text-lg font-semibold">Henüz rezervasyonun yok</h2>
            <p className="mt-2 text-sm text-muted-foreground">İlk rezervasyonunu 30 saniyede oluştur.</p>
            <Button asChild className="mt-6">
              <Link to="/rezervasyon">İlk Rezervasyonunu Oluştur</Link>
            </Button>
          </CardContent>
        </Card>
      ) : (
        <ul className="space-y-3">
          {data!.items.map((r) => (
            <li key={r.id}>
              <Link
                to={`/hesabim/rezervasyonlar/${r.id}`}
                className="flex items-start gap-4 rounded-xl border bg-card p-5 shadow-soft hover:shadow-elevated transition-shadow"
              >
                <div className="flex-1 min-w-0">
                  <div className="flex flex-wrap items-center gap-2">
                    <StatusBadge status={r.status} />
                    <span className="text-sm font-medium text-muted-foreground">
                      {SERVICE_TYPE_LABELS[r.serviceType]}
                    </span>
                  </div>
                  <div className="mt-3 space-y-1.5 text-sm">
                    <div className="flex items-center gap-2">
                      <Calendar className="size-4 text-muted-foreground" aria-hidden />
                      <span>{formatDate(r.reservationDateTimeUtc)}</span>
                    </div>
                    <div className="flex items-start gap-2">
                      <MapPin className="size-4 text-muted-foreground mt-0.5" aria-hidden />
                      <span className="line-clamp-2">{r.address}</span>
                    </div>
                  </div>
                </div>
                <ChevronRight className="size-5 text-muted-foreground mt-1" aria-hidden />
              </Link>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}
