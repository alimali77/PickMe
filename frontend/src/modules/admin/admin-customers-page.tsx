import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Search, Users } from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { adminApi } from './admin-api';
import { EmptyState, PageHeader, SkeletonRows } from './admin-ui';
import { formatDate } from '@/lib/utils';

export function AdminCustomersPage() {
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);

  const { data, isLoading } = useQuery({
    queryKey: ['admin-customers', { search, page }],
    queryFn: () => adminApi.customers.list({ search: search || undefined, page, pageSize: 20 }),
  });

  return (
    <div>
      <PageHeader title="Müşteriler" description="Kayıtlı müşterileri görüntüleyin." />

      <div className="mb-4 relative max-w-sm">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground pointer-events-none" aria-hidden />
        <Input className="pl-9" placeholder="Ad, e-posta veya telefon ara..." value={search} onChange={(e) => { setSearch(e.target.value); setPage(1); }} />
      </div>

      {isLoading ? <SkeletonRows /> : (data?.items.length ?? 0) === 0 ? (
        <EmptyState icon={Users} title="Müşteri bulunamadı" description="Arama kriterine uyan müşteri yok." />
      ) : (
        <Card>
          <CardContent className="p-0">
            <ul className="divide-y">
              {data!.items.map((c) => (
                <li key={c.id} className="p-4 flex items-start gap-4">
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 flex-wrap">
                      <span className="font-semibold">{c.firstName} {c.lastName}</span>
                      {c.isActive ? <Badge variant="success">Aktif</Badge> : <Badge variant="outline">Pasif</Badge>}
                    </div>
                    <div className="mt-1 text-xs text-muted-foreground grid sm:grid-cols-3 gap-x-4">
                      <span className="truncate">{c.email}</span>
                      <span>{c.phoneNumber}</span>
                      <span>{c.reservationCount} rezervasyon</span>
                    </div>
                    <div className="mt-0.5 text-[11px] text-muted-foreground">
                      Kayıt: {formatDate(c.createdAtUtc)}
                    </div>
                  </div>
                </li>
              ))}
            </ul>
          </CardContent>
        </Card>
      )}

      {data && data.totalPages > 1 ? (
        <div className="mt-4 flex items-center justify-between text-sm">
          <span className="text-muted-foreground">Toplam {data.totalCount}</span>
          <div className="flex gap-2">
            <Button variant="outline" size="sm" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>Önceki</Button>
            <Button variant="outline" size="sm" disabled={page >= data.totalPages} onClick={() => setPage((p) => p + 1)}>Sonraki</Button>
          </div>
        </div>
      ) : null}
    </div>
  );
}
