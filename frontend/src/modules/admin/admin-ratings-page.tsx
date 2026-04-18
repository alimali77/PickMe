import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { Star, Flag, Undo2 } from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Textarea } from '@/components/ui/textarea';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { adminApi } from './admin-api';
import { EmptyState, PageHeader, SkeletonRows } from './admin-ui';
import { cn, formatDate } from '@/lib/utils';
import type { AdminRatingListItem } from '@pickme/shared/api-types';

export function AdminRatingsPage() {
  const qc = useQueryClient();
  const [page, setPage] = useState(1);
  const [minScore, setMinScore] = useState<number | undefined>();
  const [flagging, setFlagging] = useState<AdminRatingListItem | null>(null);

  const { data, isLoading } = useQuery({
    queryKey: ['admin-ratings', { page, minScore }],
    queryFn: () => adminApi.ratings.list({ page, pageSize: 20, minScore }),
  });

  const unflagMut = useMutation({
    mutationFn: (id: string) => adminApi.ratings.unflag(id),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['admin-ratings'] }); toast.success('Flag kaldırıldı. Şoför ortalaması güncellendi.'); },
    onError: (e: Error) => toast.error(e.message),
  });

  return (
    <div>
      <PageHeader
        title="Değerlendirmeler"
        description="Müşteri puanlarını gözden geçirin. Uygunsuz yorumları flag'leyin; flag'li puanlar şoför ortalamasına dahil edilmez."
        actions={
          <div className="flex gap-2 flex-wrap">
            {[undefined, 1, 2, 3, 4, 5].map((s) => (
              <Button
                key={s ?? 'all'}
                variant={minScore === s ? 'default' : 'outline'}
                size="sm"
                onClick={() => { setMinScore(s); setPage(1); }}
              >
                {s === undefined ? 'Tümü' : `${s}+`}
              </Button>
            ))}
          </div>
        }
      />

      {isLoading ? <SkeletonRows /> : (data?.items.length ?? 0) === 0 ? (
        <EmptyState icon={Star} title="Değerlendirme yok" description="Müşterilerinizden gelen puanlar burada listelenir." />
      ) : (
        <Card>
          <CardContent className="p-0">
            <ul className="divide-y">
              {data!.items.map((r) => (
                <li key={r.id} className="p-4 flex items-start gap-4">
                  <div className="flex-1 min-w-0 space-y-1.5">
                    <div className="flex items-center gap-2 flex-wrap">
                      <div className="flex items-center">
                        {[1, 2, 3, 4, 5].map((n) => (
                          <Star key={n} className={cn('size-4', n <= r.score ? 'fill-amber-400 text-amber-400' : 'text-muted-foreground/30')} aria-hidden />
                        ))}
                      </div>
                      <span className="font-medium text-sm">{r.driverName}</span>
                      <span className="text-xs text-muted-foreground">←</span>
                      <span className="text-xs text-muted-foreground">{r.customerName}</span>
                      {r.isFlagged ? <Badge variant="destructive">Flag'li</Badge> : null}
                    </div>
                    {r.comment ? <p className="text-sm text-muted-foreground">{r.comment}</p> : null}
                    {r.isFlagged && r.flaggedReason ? (
                      <p className="text-xs text-destructive/80">Flag sebebi: {r.flaggedReason}</p>
                    ) : null}
                    <div className="text-[11px] text-muted-foreground">{formatDate(r.createdAtUtc)}</div>
                  </div>
                  {r.isFlagged ? (
                    <Button variant="outline" size="sm" onClick={() => unflagMut.mutate(r.id)} loading={unflagMut.isPending && unflagMut.variables === r.id}>
                      <Undo2 className="size-4" /> Flag'i Kaldır
                    </Button>
                  ) : (
                    <Button variant="outline" size="sm" onClick={() => setFlagging(r)} className="text-destructive hover:text-destructive">
                      <Flag className="size-4" /> Flag'le
                    </Button>
                  )}
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

      <FlagDialog rating={flagging} onOpenChange={(v) => { if (!v) setFlagging(null); }} onDone={() => qc.invalidateQueries({ queryKey: ['admin-ratings'] })} />
    </div>
  );
}

function FlagDialog({
  rating, onOpenChange, onDone,
}: {
  rating: AdminRatingListItem | null;
  onOpenChange: (v: boolean) => void;
  onDone: () => void;
}) {
  const [reason, setReason] = useState('');

  const mut = useMutation({
    mutationFn: () => adminApi.ratings.flag(rating!.id, reason),
    onSuccess: () => {
      toast.success('Puan flag\'lendi. Şoför ortalaması güncellendi.');
      onOpenChange(false);
      setReason('');
      onDone();
    },
    onError: (e: Error) => toast.error(e.message),
  });

  return (
    <Dialog open={!!rating} onOpenChange={(v) => { onOpenChange(v); if (!v) setReason(''); }}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Değerlendirmeyi flag'le</DialogTitle>
          <DialogDescription>Flag sebebi zorunludur. Flag'li puanlar şoför ortalamasına dahil edilmez.</DialogDescription>
        </DialogHeader>
        <Textarea value={reason} onChange={(e) => setReason(e.target.value)} rows={3} placeholder="Flag sebebi (ör: uygunsuz dil)" maxLength={500} />
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>Vazgeç</Button>
          <Button variant="destructive" onClick={() => mut.mutate()} loading={mut.isPending} disabled={reason.trim().length === 0}>
            Flag'le
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
