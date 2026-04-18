import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { CalendarRange, Clock, UserCog, CheckCircle2 } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { StatusBadge } from '@/components/shared/status-badge';
import { reservationsApi } from '@/modules/reservations/reservations-api';
import { formatDate } from '@/lib/utils';

export function AdminDashboardPage() {
  const { data, isLoading } = useQuery({
    queryKey: ['admin-reservations', 'dashboard'],
    queryFn: () => reservationsApi.adminList({ page: 1, pageSize: 10 }),
  });
  const { data: pending } = useQuery({
    queryKey: ['admin-reservations', 'pending-count'],
    queryFn: () => reservationsApi.adminList({ status: 'Pending', page: 1, pageSize: 1 }),
  });
  const { data: drivers } = useQuery({
    queryKey: ['active-drivers'],
    queryFn: () => reservationsApi.adminActiveDrivers(),
  });

  const stats = [
    { label: 'Son 10 Rezervasyon', value: data?.totalCount ?? '–', icon: CalendarRange },
    { label: 'Bekleyen Talep', value: pending?.totalCount ?? '–', icon: Clock },
    { label: 'Aktif Şoför', value: drivers?.length ?? '–', icon: UserCog },
    { label: 'Ortalama Puan', value: drivers?.length ? (drivers.reduce((a, d) => a + d.averageRating, 0) / drivers.length).toFixed(2) : '–', icon: CheckCircle2 },
  ];

  return (
    <div className="space-y-6">
      <h1 className="text-2xl md:text-3xl font-bold tracking-tight">Gösterge Paneli</h1>

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        {stats.map((s) => (
          <Card key={s.label}>
            <CardContent className="p-5">
              <div className="flex items-center justify-between">
                <div>
                  <div className="text-xs text-muted-foreground">{s.label}</div>
                  <div className="mt-1 text-2xl font-bold">{s.value}</div>
                </div>
                <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10 text-primary">
                  <s.icon className="size-5" aria-hidden />
                </div>
              </div>
            </CardContent>
          </Card>
        ))}
      </div>

      <Card>
        <CardHeader className="flex flex-row items-center justify-between space-y-0">
          <CardTitle>Son Rezervasyonlar</CardTitle>
          <Button asChild variant="outline" size="sm">
            <Link to="/admin/rezervasyonlar">Tümünü Gör</Link>
          </Button>
        </CardHeader>
        <CardContent>
          {isLoading ? (
            <div className="space-y-2">
              {[1, 2, 3].map((k) => <div key={k} className="h-12 rounded bg-muted animate-pulse" />)}
            </div>
          ) : (data?.items ?? []).length === 0 ? (
            <p className="text-sm text-muted-foreground text-center py-8">Henüz rezervasyon yok.</p>
          ) : (
            <ul className="divide-y">
              {data!.items.slice(0, 10).map((r) => (
                <li key={r.id}>
                  <Link to={`/admin/rezervasyonlar/${r.id}`} className="flex items-center gap-3 py-3 hover:bg-accent/50 -mx-2 px-2 rounded-md">
                    <StatusBadge status={r.status} />
                    <div className="flex-1 min-w-0">
                      <div className="font-medium truncate">{r.customerName}</div>
                      <div className="text-xs text-muted-foreground truncate">{r.address}</div>
                    </div>
                    <div className="text-xs text-muted-foreground whitespace-nowrap">{formatDate(r.reservationDateTimeUtc)}</div>
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </CardContent>
      </Card>
    </div>
  );
}
