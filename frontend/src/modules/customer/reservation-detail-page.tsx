import { useState } from 'react';
import { useParams, Link, useNavigate } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { ArrowLeft, Calendar, MapPin, Phone, Star, X } from 'lucide-react';
import { Card, CardContent, CardHeader } from '@/components/ui/card';
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
import { StatusBadge } from '@/components/shared/status-badge';
import { reservationsApi } from '@/modules/reservations/reservations-api';
import { SERVICE_TYPE_LABELS } from '@pickme/shared/constants';
import { cn, formatDate } from '@/lib/utils';

export function ReservationDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const qc = useQueryClient();

  const { data, isLoading } = useQuery({
    queryKey: ['reservation', id],
    queryFn: () => reservationsApi.getMine(id!),
    enabled: !!id,
  });

  const [cancelOpen, setCancelOpen] = useState(false);
  const [ratingOpen, setRatingOpen] = useState(false);

  const cancelMut = useMutation({
    mutationFn: () => reservationsApi.cancel(id!),
    onSuccess: () => {
      toast.success('Rezervasyon iptal edildi.');
      setCancelOpen(false);
      qc.invalidateQueries({ queryKey: ['reservation', id] });
      qc.invalidateQueries({ queryKey: ['my-reservations'] });
    },
    onError: (e: Error) => toast.error(e.message),
  });

  if (isLoading || !data) {
    return (
      <div className="space-y-3" aria-label="Yükleniyor">
        <div className="h-12 w-48 rounded bg-muted animate-pulse" />
        <div className="h-64 rounded-xl bg-muted animate-pulse" />
      </div>
    );
  }

  const r = data;
  const canCancel = r.status === 'Pending';
  const canRate = r.status === 'Completed' && !r.hasRating;
  const canEditRating = r.status === 'Completed' && r.hasRating && r.ratingEditable;

  return (
    <div className="space-y-6">
      <Button asChild variant="ghost" size="sm">
        <Link to="/hesabim/rezervasyonlar"><ArrowLeft className="size-4" /> Rezervasyonlarım</Link>
      </Button>

      <Card>
        <CardHeader>
          <div className="flex flex-wrap items-center justify-between gap-3">
            <div>
              <StatusBadge status={r.status} />
              <h1 className="mt-2 text-2xl font-bold">{SERVICE_TYPE_LABELS[r.serviceType]}</h1>
            </div>
            <div className="flex flex-wrap gap-2">
              {canCancel && (
                <Button variant="outline" onClick={() => setCancelOpen(true)}>
                  <X className="size-4" /> İptal Et
                </Button>
              )}
              {(canRate || canEditRating) && (
                <Button onClick={() => setRatingOpen(true)}>
                  <Star className="size-4" /> {canEditRating ? 'Puanı Düzenle' : 'Puan Ver'}
                </Button>
              )}
            </div>
          </div>
        </CardHeader>
        <CardContent className="space-y-5">
          <InfoRow icon={Calendar} label="Tarih & Saat" value={formatDate(r.reservationDateTimeUtc)} />
          <InfoRow icon={MapPin} label="Konum" value={r.address} />

          {r.driverName ? (
            <div className="rounded-xl border bg-muted/30 p-4 space-y-3">
              <div className="flex items-center justify-between">
                <div>
                  <div className="text-xs text-muted-foreground">Şoförünüz</div>
                  <div className="font-semibold">{r.driverName}</div>
                  {r.driverAverageRating != null && r.driverAverageRating > 0 ? (
                    <div className="flex items-center gap-1 mt-0.5 text-xs text-muted-foreground">
                      <Star className="size-3 fill-amber-400 text-amber-400" aria-hidden />
                      {r.driverAverageRating.toFixed(2)}
                    </div>
                  ) : null}
                </div>
                {r.driverPhone && r.status !== 'Completed' && r.status !== 'Cancelled' ? (
                  <Button asChild size="sm">
                    <a href={`tel:${r.driverPhone}`}><Phone className="size-4" /> Ara</a>
                  </Button>
                ) : null}
              </div>
            </div>
          ) : null}

          {r.note ? (
            <div>
              <div className="text-xs text-muted-foreground mb-1">Not</div>
              <div className="text-sm">{r.note}</div>
            </div>
          ) : null}

          {r.cancellationReason ? (
            <div className="rounded-md border border-destructive/20 bg-destructive/5 p-3 text-sm">
              <div className="font-medium text-destructive mb-0.5">İptal sebebi:</div>
              <div>{r.cancellationReason}</div>
            </div>
          ) : null}

          {r.hasRating && r.ratingScore ? (
            <div className="flex items-center gap-1 text-sm">
              <span className="text-muted-foreground">Verdiğiniz puan:</span>
              {[1, 2, 3, 4, 5].map((n) => (
                <Star
                  key={n}
                  className={cn('size-5', n <= r.ratingScore! ? 'fill-amber-400 text-amber-400' : 'text-muted')}
                  aria-hidden
                />
              ))}
            </div>
          ) : null}
        </CardContent>
      </Card>

      <Dialog open={cancelOpen} onOpenChange={setCancelOpen}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Rezervasyonu iptal etmek istediğinize emin misiniz?</DialogTitle>
            <DialogDescription>
              Bu işlem geri alınamaz. Sadece şoför ataması yapılmadan önce iptal edebilirsiniz.
            </DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setCancelOpen(false)}>Vazgeç</Button>
            <Button variant="destructive" onClick={() => cancelMut.mutate()} loading={cancelMut.isPending}>
              Evet, İptal Et
            </Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>

      <RatingDialog
        open={ratingOpen}
        onOpenChange={setRatingOpen}
        reservationId={r.id}
        initialScore={r.ratingScore}
        isEdit={canEditRating}
        onSuccess={() => qc.invalidateQueries({ queryKey: ['reservation', r.id] })}
      />
    </div>
  );
}

function InfoRow({ icon: Icon, label, value }: { icon: typeof Calendar; label: string; value: string }) {
  return (
    <div className="flex items-start gap-3">
      <div className="flex h-9 w-9 items-center justify-center rounded-lg bg-primary/10 text-primary shrink-0">
        <Icon className="size-4" aria-hidden />
      </div>
      <div className="min-w-0">
        <div className="text-xs text-muted-foreground">{label}</div>
        <div className="font-medium">{value}</div>
      </div>
    </div>
  );
}

function RatingDialog({
  open,
  onOpenChange,
  reservationId,
  initialScore,
  isEdit,
  onSuccess,
}: {
  open: boolean;
  onOpenChange: (v: boolean) => void;
  reservationId: string;
  initialScore?: number | null;
  isEdit: boolean;
  onSuccess: () => void;
}) {
  const [score, setScore] = useState(initialScore ?? 0);
  const [comment, setComment] = useState('');
  const qc = useQueryClient();

  const mut = useMutation({
    mutationFn: (payload: { score: number; comment?: string }) =>
      isEdit
        ? reservationsApi.editRating(reservationId, payload)
        : reservationsApi.rate(reservationId, payload),
    onSuccess: () => {
      toast.success(isEdit ? 'Puanınız güncellendi.' : 'Teşekkürler, puanınız kaydedildi!');
      qc.invalidateQueries({ queryKey: ['my-reservations'] });
      onSuccess();
      onOpenChange(false);
    },
    onError: (e: Error) => toast.error(e.message),
  });

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{isEdit ? 'Puanı Düzenle' : 'Şoförünüzü Değerlendirin'}</DialogTitle>
          <DialogDescription>Deneyiminizi 1 ile 5 yıldız arasında değerlendirin.</DialogDescription>
        </DialogHeader>
        <div className="space-y-4">
          <div className="flex items-center justify-center gap-2">
            {[1, 2, 3, 4, 5].map((n) => (
              <button
                key={n}
                type="button"
                onClick={() => setScore(n)}
                aria-label={`${n} yıldız`}
                className="p-1 transition-transform hover:scale-110"
              >
                <Star
                  className={cn('size-9', n <= score ? 'fill-amber-400 text-amber-400' : 'text-muted-foreground/40')}
                  aria-hidden
                />
              </button>
            ))}
          </div>
          <Textarea
            value={comment}
            onChange={(e) => setComment(e.target.value)}
            placeholder="Yorumunuz (opsiyonel)..."
            maxLength={500}
            rows={3}
          />
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>Vazgeç</Button>
          <Button
            onClick={() => mut.mutate({ score, comment: comment || undefined })}
            loading={mut.isPending}
            disabled={score < 1 || score > 5}
          >
            {isEdit ? 'Güncelle' : 'Puanı Gönder'}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
