import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { ArrowLeft, Phone, MapPin, Calendar, ExternalLink, Play, CheckCircle2 } from 'lucide-react';
import { toast } from 'sonner';
import { Button } from '@/components/ui/button';
import { Card, CardContent } from '@/components/ui/card';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { StatusBadge } from '@/components/shared/status-badge';
import { reservationsApi } from '@/modules/reservations/reservations-api';
import { SERVICE_TYPE_LABELS } from '@pickme/shared/constants';
import { formatDate } from '@/lib/utils';

export function DriverTaskDetailPage() {
  const { id } = useParams<{ id: string }>();
  const qc = useQueryClient();
  const [completeOpen, setCompleteOpen] = useState(false);

  const { data, isLoading } = useQuery({
    queryKey: ['driver-task', id],
    queryFn: () => reservationsApi.driverGet(id!),
    enabled: !!id,
  });

  const startMut = useMutation({
    mutationFn: () => reservationsApi.driverStart(id!),
    onSuccess: () => {
      toast.success('Yolculuğa başlandı.');
      qc.invalidateQueries({ queryKey: ['driver-task', id] });
      qc.invalidateQueries({ queryKey: ['driver-tasks'] });
    },
    onError: (e: Error) => toast.error(e.message),
  });

  const completeMut = useMutation({
    mutationFn: () => reservationsApi.driverComplete(id!),
    onSuccess: () => {
      toast.success('Yolculuk tamamlandı.');
      setCompleteOpen(false);
      qc.invalidateQueries({ queryKey: ['driver-task', id] });
      qc.invalidateQueries({ queryKey: ['driver-tasks'] });
    },
    onError: (e: Error) => toast.error(e.message),
  });

  if (isLoading || !data) {
    return <div className="h-64 rounded-xl bg-card border animate-pulse" />;
  }

  const t = data;
  const mapsUrl = `https://www.google.com/maps?q=${t.lat},${t.lng}`;
  const canStart = t.status === 'Assigned';
  const canComplete = t.status === 'OnTheWay';

  return (
    <div className="space-y-4 pb-24 md:pb-0">
      <Button asChild variant="ghost" size="sm">
        <Link to="/driver"><ArrowLeft className="size-4" /> Görev Listesi</Link>
      </Button>

      <Card>
        <CardContent className="p-5 space-y-5">
          <div className="flex items-center justify-between">
            <StatusBadge status={t.status} />
            <span className="text-sm text-muted-foreground">{SERVICE_TYPE_LABELS[t.serviceType]}</span>
          </div>

          <div>
            <div className="text-xs text-muted-foreground mb-1">Müşteri</div>
            <div className="text-lg font-semibold">{t.customerName}</div>
            <a
              href={`tel:${t.customerPhone}`}
              className="mt-2 inline-flex items-center gap-2 text-primary font-medium hover:underline"
            >
              <Phone className="size-4" aria-hidden /> {t.customerPhone}
            </a>
          </div>

          <div>
            <div className="text-xs text-muted-foreground mb-1 flex items-center gap-1"><Calendar className="size-3" /> Tarih</div>
            <div className="font-medium">{formatDate(t.reservationDateTimeUtc)}</div>
          </div>

          <div>
            <div className="text-xs text-muted-foreground mb-1 flex items-center gap-1"><MapPin className="size-3" /> Konum</div>
            <div className="font-medium">{t.address}</div>
            <Button asChild variant="outline" size="sm" className="mt-2">
              <a href={mapsUrl} target="_blank" rel="noreferrer noopener">
                <ExternalLink className="size-4" /> Haritada Aç
              </a>
            </Button>
          </div>

          {t.note ? (
            <div>
              <div className="text-xs text-muted-foreground mb-1">Not</div>
              <div className="text-sm rounded-md border bg-muted/30 p-3">{t.note}</div>
            </div>
          ) : null}
        </CardContent>
      </Card>

      {/* Action buttons — sticky bottom on mobile */}
      {(canStart || canComplete) && (
        <div className="fixed bottom-16 md:bottom-6 left-0 right-0 px-4 md:static md:px-0 z-20">
          <div className="container md:p-0 max-w-full">
            {canStart ? (
              <Button size="xl" className="w-full shadow-elevated" onClick={() => startMut.mutate()} loading={startMut.isPending}>
                <Play className="size-5" /> Yola Çıktım
              </Button>
            ) : null}
            {canComplete ? (
              <Button size="xl" className="w-full shadow-elevated bg-emerald-600 hover:bg-emerald-700" onClick={() => setCompleteOpen(true)}>
                <CheckCircle2 className="size-5" /> Yolculuğu Tamamla
              </Button>
            ) : null}
          </div>
        </div>
      )}

      <Dialog open={completeOpen} onOpenChange={setCompleteOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Yolculuğu tamamladınız mı?</DialogTitle>
            <DialogDescription>
              Bu işlem geri alınamaz. Müşteriye değerlendirme daveti gönderilecek.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setCompleteOpen(false)}>Vazgeç</Button>
            <Button onClick={() => completeMut.mutate()} loading={completeMut.isPending} className="bg-emerald-600 hover:bg-emerald-700">
              Evet, Tamamla
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}
