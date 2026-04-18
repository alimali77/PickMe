import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { ArrowLeft, Calendar, MapPin, Phone, Star, UserCog, X } from 'lucide-react';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Textarea } from '@/components/ui/textarea';
import { StatusBadge } from '@/components/shared/status-badge';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { reservationsApi } from '@/modules/reservations/reservations-api';
import { SERVICE_TYPE_LABELS } from '@pickme/shared/constants';
import { formatDate, cn } from '@/lib/utils';
import type { DriverSummary } from '@pickme/shared/api-types';

export function AdminReservationDetailPage() {
  const { id } = useParams<{ id: string }>();
  const qc = useQueryClient();
  const [assignOpen, setAssignOpen] = useState(false);
  const [cancelOpen, setCancelOpen] = useState(false);

  const { data, isLoading } = useQuery({
    queryKey: ['admin-reservation', id],
    queryFn: () => reservationsApi.adminGet(id!),
    enabled: !!id,
  });

  if (isLoading || !data) return <div className="h-64 rounded-xl bg-card border animate-pulse" />;

  const r = data;
  const canAssignOrReassign = r.status === 'Pending' || r.status === 'Assigned';
  const canCancel = r.status !== 'Completed' && r.status !== 'Cancelled';

  return (
    <div className="space-y-6">
      <Button asChild variant="ghost" size="sm">
        <Link to="/admin/rezervasyonlar"><ArrowLeft className="size-4" /> Rezervasyonlar</Link>
      </Button>

      <Card>
        <CardHeader>
          <div className="flex flex-wrap items-center justify-between gap-3">
            <div>
              <StatusBadge status={r.status} />
              <CardTitle className="mt-2">{SERVICE_TYPE_LABELS[r.serviceType]}</CardTitle>
            </div>
            <div className="flex flex-wrap gap-2">
              {canAssignOrReassign && (
                <Button onClick={() => setAssignOpen(true)}>
                  <UserCog className="size-4" /> {r.driverId ? 'Yeniden Ata' : 'Şoför Ata'}
                </Button>
              )}
              {canCancel && (
                <Button variant="destructive" onClick={() => setCancelOpen(true)}>
                  <X className="size-4" /> İptal Et
                </Button>
              )}
            </div>
          </div>
        </CardHeader>
        <CardContent className="grid gap-6 md:grid-cols-2">
          <div className="space-y-4">
            <h3 className="text-sm font-semibold text-muted-foreground">Müşteri</h3>
            <div>
              <div className="font-medium">{r.customerName}</div>
              <div className="text-sm text-muted-foreground">{r.customerEmail}</div>
              <a href={`tel:${r.customerPhone}`} className="mt-1 inline-flex items-center gap-1 text-primary hover:underline text-sm">
                <Phone className="size-3" /> {r.customerPhone}
              </a>
            </div>

            <h3 className="text-sm font-semibold text-muted-foreground">Zaman & Konum</h3>
            <div className="space-y-2 text-sm">
              <div className="flex items-start gap-2"><Calendar className="size-4 text-muted-foreground mt-0.5" /> {formatDate(r.reservationDateTimeUtc)}</div>
              <div className="flex items-start gap-2"><MapPin className="size-4 text-muted-foreground mt-0.5" /> {r.address}</div>
              <div className="text-xs text-muted-foreground font-mono">({r.lat.toFixed(4)}, {r.lng.toFixed(4)})</div>
            </div>
            {r.note ? (
              <div className="rounded-md border bg-muted/30 p-3 text-sm">
                <div className="text-xs font-semibold text-muted-foreground mb-1">Müşteri Notu</div>
                {r.note}
              </div>
            ) : null}
          </div>

          <div className="space-y-4">
            <h3 className="text-sm font-semibold text-muted-foreground">Şoför</h3>
            {r.driverName ? (
              <div>
                <div className="font-medium">{r.driverName}</div>
                <a href={`tel:${r.driverPhone}`} className="mt-0.5 inline-flex items-center gap-1 text-primary hover:underline text-sm">
                  <Phone className="size-3" /> {r.driverPhone}
                </a>
                {r.driverAverageRating != null && r.driverAverageRating > 0 ? (
                  <div className="mt-1 flex items-center gap-1 text-xs text-muted-foreground">
                    <Star className="size-3 fill-amber-400 text-amber-400" /> {r.driverAverageRating.toFixed(2)}
                  </div>
                ) : null}
              </div>
            ) : (
              <div className="text-sm text-muted-foreground italic">Henüz şoför atanmadı.</div>
            )}

            <h3 className="text-sm font-semibold text-muted-foreground pt-2">Zaman Çizelgesi</h3>
            <ul className="space-y-1.5 text-sm">
              <TimelineRow label="Oluşturuldu" value={r.createdAtUtc} />
              {r.assignedAtUtc && <TimelineRow label="Atandı" value={r.assignedAtUtc} />}
              {r.startedAtUtc && <TimelineRow label="Yola Çıkıldı" value={r.startedAtUtc} />}
              {r.completedAtUtc && <TimelineRow label="Tamamlandı" value={r.completedAtUtc} />}
              {r.cancelledAtUtc && <TimelineRow label={`İptal (${r.cancelledBy === 'Customer' ? 'Müşteri' : 'Admin'})`} value={r.cancelledAtUtc} />}
            </ul>
            {r.cancellationReason ? (
              <div className="rounded-md border border-destructive/20 bg-destructive/5 p-3 text-sm">
                <div className="font-medium text-destructive text-xs mb-0.5">İptal sebebi:</div>
                {r.cancellationReason}
              </div>
            ) : null}

            {r.hasRating && r.ratingScore ? (
              <div className="rounded-md border bg-muted/30 p-3">
                <div className="text-xs font-semibold text-muted-foreground mb-1">Müşteri Puanı</div>
                <div className="flex items-center gap-1">
                  {[1, 2, 3, 4, 5].map((n) => (
                    <Star key={n} className={cn('size-4', n <= r.ratingScore! ? 'fill-amber-400 text-amber-400' : 'text-muted-foreground/30')} aria-hidden />
                  ))}
                </div>
              </div>
            ) : null}
          </div>
        </CardContent>
      </Card>

      <AssignDriverDialog
        open={assignOpen}
        onOpenChange={setAssignOpen}
        reservationId={r.id}
        currentDriverId={r.driverId ?? null}
        onDone={() => qc.invalidateQueries({ queryKey: ['admin-reservation', r.id] })}
      />

      <CancelDialog
        open={cancelOpen}
        onOpenChange={setCancelOpen}
        reservationId={r.id}
        onDone={() => qc.invalidateQueries({ queryKey: ['admin-reservation', r.id] })}
      />
    </div>
  );
}

function TimelineRow({ label, value }: { label: string; value: string }) {
  return (
    <li className="flex items-center gap-2">
      <span className="size-1.5 rounded-full bg-primary" />
      <span className="text-muted-foreground">{label}:</span>
      <span className="ml-auto">{formatDate(value)}</span>
    </li>
  );
}

function AssignDriverDialog({
  open,
  onOpenChange,
  reservationId,
  currentDriverId,
  onDone,
}: {
  open: boolean;
  onOpenChange: (v: boolean) => void;
  reservationId: string;
  currentDriverId: string | null;
  onDone: () => void;
}) {
  const [selected, setSelected] = useState<string | null>(currentDriverId);
  const { data: drivers, isLoading } = useQuery({
    queryKey: ['active-drivers'],
    queryFn: () => reservationsApi.adminActiveDrivers(),
    enabled: open,
  });

  const mut = useMutation({
    mutationFn: (driverId: string) => reservationsApi.adminAssign(reservationId, driverId),
    onSuccess: () => {
      toast.success('Şoför atandı. Müşteri ve şoföre bildirim gönderildi.');
      onOpenChange(false);
      onDone();
    },
    onError: (e: Error) => toast.error(e.message),
  });

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-lg">
        <DialogHeader>
          <DialogTitle>Şoför Ata</DialogTitle>
          <DialogDescription>Aktif şoförler arasından seçim yapın.</DialogDescription>
        </DialogHeader>
        <div className="max-h-80 overflow-auto space-y-2">
          {isLoading ? <div className="text-sm text-muted-foreground">Yükleniyor...</div> : null}
          {drivers?.map((d: DriverSummary) => {
            const isSelected = selected === d.id;
            return (
              <button
                key={d.id}
                type="button"
                onClick={() => setSelected(d.id)}
                className={cn('flex w-full items-center justify-between rounded-lg border p-3 text-left transition-colors',
                  isSelected ? 'border-primary bg-primary/5' : 'hover:bg-accent')}
              >
                <div>
                  <div className="font-medium">{d.firstName} {d.lastName}</div>
                  <div className="text-xs text-muted-foreground">{d.phoneNumber}</div>
                </div>
                <div className="text-right">
                  <div className="flex items-center gap-1 text-sm font-medium">
                    <Star className="size-3 fill-amber-400 text-amber-400" />
                    {d.averageRating > 0 ? d.averageRating.toFixed(2) : '—'}
                  </div>
                  <div className="text-xs text-muted-foreground">{d.totalTrips} yolculuk</div>
                </div>
              </button>
            );
          })}
          {drivers && drivers.length === 0 ? (
            <p className="text-sm text-muted-foreground text-center py-4">Aktif şoför yok. Önce Şoförler modülünden ekleyin.</p>
          ) : null}
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>Vazgeç</Button>
          <Button disabled={!selected} onClick={() => selected && mut.mutate(selected)} loading={mut.isPending}>
            Atamayı Onayla
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

function CancelDialog({
  open,
  onOpenChange,
  reservationId,
  onDone,
}: {
  open: boolean;
  onOpenChange: (v: boolean) => void;
  reservationId: string;
  onDone: () => void;
}) {
  const [reason, setReason] = useState('');
  const mut = useMutation({
    mutationFn: () => reservationsApi.adminCancel(reservationId, reason),
    onSuccess: () => {
      toast.success('Rezervasyon iptal edildi.');
      onOpenChange(false);
      onDone();
    },
    onError: (e: Error) => toast.error(e.message),
  });

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Rezervasyonu İptal Et</DialogTitle>
          <DialogDescription>İptal sebebi zorunludur. Müşteri ve şoföre bildirim gönderilecek.</DialogDescription>
        </DialogHeader>
        <Textarea
          value={reason}
          onChange={(e) => setReason(e.target.value)}
          placeholder="İptal sebebini yazın..."
          rows={3}
          maxLength={1000}
        />
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>Vazgeç</Button>
          <Button variant="destructive" disabled={reason.trim().length === 0} onClick={() => mut.mutate()} loading={mut.isPending}>
            İptal Et
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
