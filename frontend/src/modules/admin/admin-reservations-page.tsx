import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { Search, ChevronRight } from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Button } from '@/components/ui/button';
import { StatusBadge } from '@/components/shared/status-badge';
import { reservationsApi } from '@/modules/reservations/reservations-api';
import { RESERVATION_LOCAL_LABELS, SERVICE_TYPE_LABELS, ReservationStatus } from '@pickme/shared/constants';
import { formatDate, cn } from '@/lib/utils';
import type { ReservationStatusLiteral } from '@pickme/shared/api-types';

const STATUSES: ReservationStatusLiteral[] = ['Pending', 'Assigned', 'OnTheWay', 'Completed', 'Cancelled'];

export function AdminReservationsPage() {
  const [status, setStatus] = useState<ReservationStatusLiteral | ''>('');
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);

  const { data, isLoading } = useQuery({
    queryKey: ['admin-reservations', { status, search, page }],
    queryFn: () =>
      reservationsApi.adminList({
        status: status || undefined,
        search: search || undefined,
        page,
        pageSize: 20,
      }),
  });

  return (
    <div className="space-y-5">
      <div className="flex items-center justify-between flex-wrap gap-3">
        <h1 className="text-2xl md:text-3xl font-bold tracking-tight">Rezervasyonlar</h1>
        <Button asChild variant="outline" size="sm">
          <a href={`${import.meta.env.VITE_API_URL ?? '/api'}/admin/reservations/export?status=${status ?? ''}`}>
            CSV Dışa Aktar
          </a>
        </Button>
      </div>

      <div className="flex flex-col md:flex-row gap-3">
        <div className="relative flex-1">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground pointer-events-none" aria-hidden />
          <Input
            className="pl-9"
            value={search}
            onChange={(e) => { setSearch(e.target.value); setPage(1); }}
            placeholder="Adres veya not içinde ara..."
          />
        </div>
        <div className="flex gap-2 overflow-x-auto">
          <button
            onClick={() => { setStatus(''); setPage(1); }}
            className={cn('px-3 h-11 rounded-md border text-sm font-medium whitespace-nowrap transition-colors',
              status === '' ? 'bg-primary text-primary-foreground' : 'hover:bg-accent')}
          >
            Tümü
          </button>
          {STATUSES.map((s) => (
            <button
              key={s}
              onClick={() => { setStatus(s); setPage(1); }}
              className={cn('px-3 h-11 rounded-md border text-sm font-medium whitespace-nowrap transition-colors',
                status === s ? 'bg-primary text-primary-foreground' : 'hover:bg-accent')}
            >
              {RESERVATION_LOCAL_LABELS[ReservationStatus[s]]}
            </button>
          ))}
        </div>
      </div>

      <Card>
        <CardContent className="p-0">
          {isLoading ? (
            <div className="p-8 space-y-2">
              {[1, 2, 3, 4].map((k) => <div key={k} className="h-12 rounded bg-muted animate-pulse" />)}
            </div>
          ) : data && data.items.length === 0 ? (
            <div className="py-16 text-center text-muted-foreground">Rezervasyon bulunamadı.</div>
          ) : (
            <ul className="divide-y">
              {data!.items.map((r) => (
                <li key={r.id}>
                  <Link
                    to={`/admin/rezervasyonlar/${r.id}`}
                    className="flex items-start md:items-center gap-3 p-4 hover:bg-accent/40 transition-colors flex-col md:flex-row"
                  >
                    <div className="flex items-center gap-2 md:w-32 shrink-0">
                      <StatusBadge status={r.status} />
                    </div>
                    <div className="flex-1 min-w-0 md:flex md:items-center md:gap-6">
                      <div className="min-w-0 md:flex-1">
                        <div className="font-semibold truncate">{r.customerName}</div>
                        <div className="text-xs text-muted-foreground truncate">{r.address}</div>
                      </div>
                      <div className="text-xs text-muted-foreground md:w-40">
                        <div>{SERVICE_TYPE_LABELS[r.serviceType]}</div>
                        <div>{formatDate(r.reservationDateTimeUtc)}</div>
                      </div>
                      <div className="text-xs text-muted-foreground md:w-40">
                        {r.driverName ? <span className="truncate">{r.driverName}</span> : <span className="italic">Atanmamış</span>}
                      </div>
                    </div>
                    <ChevronRight className="hidden md:block size-5 text-muted-foreground" aria-hidden />
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </CardContent>
      </Card>

      {data && data.totalPages > 1 ? (
        <div className="flex items-center justify-between text-sm">
          <div className="text-muted-foreground">
            Toplam {data.totalCount} — Sayfa {data.page} / {data.totalPages}
          </div>
          <div className="flex gap-2">
            <Button variant="outline" size="sm" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>Önceki</Button>
            <Button variant="outline" size="sm" disabled={page >= data.totalPages} onClick={() => setPage((p) => p + 1)}>Sonraki</Button>
          </div>
        </div>
      ) : null}
    </div>
  );
}
