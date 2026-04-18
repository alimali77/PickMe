import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import {
  Search,
  Plus,
  Star,
  MoreVertical,
  Power,
  KeyRound,
  Trash2,
  UserPlus,
  Users,
} from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { FormField } from '@/components/ui/form-field';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuSeparator,
  DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { adminApi } from './admin-api';
import { PageHeader, EmptyState, SkeletonRows } from './admin-ui';
import type { DriverListItem } from '@pickme/shared/api-types';

export function AdminDriversPage() {
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [createOpen, setCreateOpen] = useState(false);
  const [editing, setEditing] = useState<DriverListItem | null>(null);
  const [resetting, setResetting] = useState<DriverListItem | null>(null);
  const [deleting, setDeleting] = useState<DriverListItem | null>(null);
  const qc = useQueryClient();

  const { data, isLoading } = useQuery({
    queryKey: ['admin-drivers', { search, page }],
    queryFn: () => adminApi.drivers.list({ search: search || undefined, page, pageSize: 20 }),
  });

  const setActiveMut = useMutation({
    mutationFn: (p: { id: string; active: boolean }) => adminApi.drivers.setActive(p.id, p.active),
    onSuccess: (_, vars) => {
      toast.success(vars.active ? 'Şoför aktifleştirildi.' : 'Şoför pasifleştirildi.');
      qc.invalidateQueries({ queryKey: ['admin-drivers'] });
      qc.invalidateQueries({ queryKey: ['active-drivers'] });
    },
    onError: (e: Error) => toast.error(e.message),
  });

  return (
    <div>
      <PageHeader
        title="Şoförler"
        description="Şoför hesaplarını yönet, şifrelerini sıfırla ve aktif/pasif durumlarını kontrol et."
        actions={
          <Button onClick={() => setCreateOpen(true)}>
            <Plus /> Yeni Şoför
          </Button>
        }
      />

      <div className="mb-4">
        <div className="relative max-w-sm">
          <Search className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground pointer-events-none" aria-hidden />
          <Input
            className="pl-9"
            placeholder="Ad, soyad, e-posta veya telefon ile ara..."
            value={search}
            onChange={(e) => { setSearch(e.target.value); setPage(1); }}
          />
        </div>
      </div>

      {isLoading ? (
        <SkeletonRows />
      ) : data && data.items.length === 0 ? (
        <EmptyState
          icon={Users}
          title="Henüz şoför eklenmemiş"
          description="Yeni bir şoför ekleyerek başlayın. Oluşturulduğunda şoföre giriş bilgileri e-posta ile iletilecek."
          action={<Button onClick={() => setCreateOpen(true)}><UserPlus /> İlk Şoförü Ekle</Button>}
        />
      ) : (
        <Card>
          <CardContent className="p-0 overflow-hidden">
            <ul className="divide-y">
              {data!.items.map((d) => (
                <li key={d.id} className="p-4 flex items-start gap-4 hover:bg-accent/30 transition-colors">
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 flex-wrap">
                      <span className="font-semibold">{d.firstName} {d.lastName}</span>
                      {d.status === 'Active' ? (
                        <Badge variant="success">Aktif</Badge>
                      ) : (
                        <Badge variant="outline">Pasif</Badge>
                      )}
                      {d.mustChangePassword ? (
                        <Badge variant="warning">Şifre değiştirmesi gerekiyor</Badge>
                      ) : null}
                    </div>
                    <div className="mt-1 text-xs text-muted-foreground grid sm:grid-cols-2 md:grid-cols-4 gap-x-4 gap-y-1">
                      <span className="truncate">{d.email}</span>
                      <span>{d.phoneNumber}</span>
                      <span className="flex items-center gap-1">
                        <Star className="size-3 fill-amber-400 text-amber-400" aria-hidden />
                        {d.averageRating > 0 ? d.averageRating.toFixed(2) : '—'}
                        <span className="text-muted-foreground/70">({d.totalTrips} yolc.)</span>
                      </span>
                    </div>
                  </div>
                  <DropdownMenu>
                    <DropdownMenuTrigger asChild>
                      <Button variant="ghost" size="icon" aria-label="Aksiyon menüsü">
                        <MoreVertical className="size-4" aria-hidden />
                      </Button>
                    </DropdownMenuTrigger>
                    <DropdownMenuContent align="end">
                      <DropdownMenuItem onSelect={() => setEditing(d)}>
                        Düzenle
                      </DropdownMenuItem>
                      <DropdownMenuItem onSelect={() => setActiveMut.mutate({ id: d.id, active: d.status !== 'Active' })}>
                        <Power className="size-4" /> {d.status === 'Active' ? 'Pasifleştir' : 'Aktifleştir'}
                      </DropdownMenuItem>
                      <DropdownMenuItem onSelect={() => setResetting(d)}>
                        <KeyRound className="size-4" /> Şifre Sıfırla
                      </DropdownMenuItem>
                      <DropdownMenuSeparator />
                      <DropdownMenuItem onSelect={() => setDeleting(d)} className="text-destructive focus:text-destructive">
                        <Trash2 className="size-4" /> Sil
                      </DropdownMenuItem>
                    </DropdownMenuContent>
                  </DropdownMenu>
                </li>
              ))}
            </ul>
          </CardContent>
        </Card>
      )}

      {data && data.totalPages > 1 ? (
        <div className="mt-4 flex items-center justify-between text-sm">
          <span className="text-muted-foreground">Toplam {data.totalCount} — Sayfa {data.page} / {data.totalPages}</span>
          <div className="flex gap-2">
            <Button variant="outline" size="sm" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>Önceki</Button>
            <Button variant="outline" size="sm" disabled={page >= data.totalPages} onClick={() => setPage((p) => p + 1)}>Sonraki</Button>
          </div>
        </div>
      ) : null}

      <CreateDriverDialog open={createOpen} onOpenChange={setCreateOpen} onDone={() => qc.invalidateQueries({ queryKey: ['admin-drivers'] })} />
      <EditDriverDialog driver={editing} onOpenChange={(v) => { if (!v) setEditing(null); }} onDone={() => qc.invalidateQueries({ queryKey: ['admin-drivers'] })} />

      <ConfirmDialog
        open={!!resetting}
        onOpenChange={(v) => { if (!v) setResetting(null); }}
        title="Şifre sıfırlansın mı?"
        description={resetting ? `${resetting.firstName} ${resetting.lastName} adlı şoförün şifresi sıfırlanacak ve yeni şifre e-posta ile gönderilecek.` : ''}
        confirmText="Evet, Sıfırla"
        variant="default"
        onConfirm={async () => {
          if (!resetting) return;
          try {
            await adminApi.drivers.resetPassword(resetting.id);
            toast.success('Şifre sıfırlandı ve şoföre e-posta gönderildi.');
            setResetting(null);
            qc.invalidateQueries({ queryKey: ['admin-drivers'] });
          } catch (e) { toast.error((e as Error).message); }
        }}
      />

      <ConfirmDialog
        open={!!deleting}
        onOpenChange={(v) => { if (!v) setDeleting(null); }}
        title="Şoförü silmek istediğinize emin misiniz?"
        description={deleting ? `${deleting.firstName} ${deleting.lastName} soft-delete edilecek; aktif görevi varsa işlem reddedilir.` : ''}
        confirmText="Evet, Sil"
        variant="destructive"
        onConfirm={async () => {
          if (!deleting) return;
          try {
            await adminApi.drivers.remove(deleting.id);
            toast.success('Şoför silindi.');
            setDeleting(null);
            qc.invalidateQueries({ queryKey: ['admin-drivers'] });
          } catch (e) { toast.error((e as Error).message); }
        }}
      />
    </div>
  );
}

function CreateDriverDialog({ open, onOpenChange, onDone }: { open: boolean; onOpenChange: (v: boolean) => void; onDone: () => void }) {
  const [form, setForm] = useState({ firstName: '', lastName: '', email: '', phone: '', initialPassword: '' });
  const [errors, setErrors] = useState<Record<string, string>>({});

  const mut = useMutation({
    mutationFn: () =>
      adminApi.drivers.create({
        firstName: form.firstName,
        lastName: form.lastName,
        email: form.email,
        phone: form.phone,
        initialPassword: form.initialPassword || undefined,
      }),
    onSuccess: () => {
      toast.success('Şoför eklendi. Giriş bilgileri e-posta ile gönderildi.');
      onOpenChange(false);
      setForm({ firstName: '', lastName: '', email: '', phone: '', initialPassword: '' });
      setErrors({});
      onDone();
    },
    onError: (e: Error & { errors?: Record<string, string[]> }) => {
      toast.error(e.message);
      if (e.errors) {
        setErrors(Object.fromEntries(Object.entries(e.errors).map(([k, v]) => [k, v[0]])));
      }
    },
  });

  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent className="max-w-lg">
        <DialogHeader>
          <DialogTitle>Yeni Şoför Ekle</DialogTitle>
          <DialogDescription>Şifre alanını boş bırakırsanız sistem otomatik üretir ve şoföre mail ile gönderir.</DialogDescription>
        </DialogHeader>
        <div className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <FormField label="Ad" required error={errors.firstName}>
              <Input value={form.firstName} onChange={(e) => setForm({ ...form, firstName: e.target.value })} />
            </FormField>
            <FormField label="Soyad" required error={errors.lastName}>
              <Input value={form.lastName} onChange={(e) => setForm({ ...form, lastName: e.target.value })} />
            </FormField>
          </div>
          <FormField label="E-posta" required error={errors.email}>
            <Input type="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} />
          </FormField>
          <FormField label="Telefon" required error={errors.phone} description="Türkiye cep telefonu, ör: 05551234567">
            <Input type="tel" value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} />
          </FormField>
          <FormField label="Başlangıç Şifresi (opsiyonel)" error={errors.initialPassword} description="Boş bırakırsanız otomatik üretilir.">
            <Input type="text" value={form.initialPassword} onChange={(e) => setForm({ ...form, initialPassword: e.target.value })} placeholder="Örn: Strong1Pass" />
          </FormField>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>Vazgeç</Button>
          <Button onClick={() => mut.mutate()} loading={mut.isPending}>Şoförü Oluştur</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

function EditDriverDialog({ driver, onOpenChange, onDone }: { driver: DriverListItem | null; onOpenChange: (v: boolean) => void; onDone: () => void }) {
  const [form, setForm] = useState({ firstName: '', lastName: '', phone: '' });
  const [errors, setErrors] = useState<Record<string, string>>({});

  const open = !!driver;
  if (driver && (form.firstName === '' && form.lastName === '' && form.phone === '')) {
    setForm({ firstName: driver.firstName, lastName: driver.lastName, phone: driver.phoneNumber });
  }

  const mut = useMutation({
    mutationFn: () => adminApi.drivers.update(driver!.id, form),
    onSuccess: () => {
      toast.success('Şoför güncellendi.');
      onOpenChange(false);
      setForm({ firstName: '', lastName: '', phone: '' });
      setErrors({});
      onDone();
    },
    onError: (e: Error & { errors?: Record<string, string[]> }) => {
      toast.error(e.message);
      if (e.errors) setErrors(Object.fromEntries(Object.entries(e.errors).map(([k, v]) => [k, v[0]])));
    },
  });

  return (
    <Dialog open={open} onOpenChange={(v) => { onOpenChange(v); if (!v) setForm({ firstName: '', lastName: '', phone: '' }); }}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Şoförü Düzenle</DialogTitle>
          <DialogDescription>Bilgileri güncelleyin. E-posta değiştirilemez.</DialogDescription>
        </DialogHeader>
        <div className="space-y-4">
          <div className="grid grid-cols-2 gap-4">
            <FormField label="Ad" required error={errors.firstName}>
              <Input value={form.firstName} onChange={(e) => setForm({ ...form, firstName: e.target.value })} />
            </FormField>
            <FormField label="Soyad" required error={errors.lastName}>
              <Input value={form.lastName} onChange={(e) => setForm({ ...form, lastName: e.target.value })} />
            </FormField>
          </div>
          <FormField label="Telefon" required error={errors.phone}>
            <Input type="tel" value={form.phone} onChange={(e) => setForm({ ...form, phone: e.target.value })} />
          </FormField>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>Vazgeç</Button>
          <Button onClick={() => mut.mutate()} loading={mut.isPending}>Kaydet</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

function ConfirmDialog({
  open, onOpenChange, title, description, confirmText, variant = 'default', onConfirm,
}: {
  open: boolean;
  onOpenChange: (v: boolean) => void;
  title: string;
  description: string;
  confirmText: string;
  variant?: 'default' | 'destructive';
  onConfirm: () => void | Promise<void>;
}) {
  const [busy, setBusy] = useState(false);
  return (
    <Dialog open={open} onOpenChange={onOpenChange}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>{title}</DialogTitle>
          <DialogDescription>{description}</DialogDescription>
        </DialogHeader>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>Vazgeç</Button>
          <Button
            variant={variant}
            loading={busy}
            onClick={async () => {
              setBusy(true);
              try { await onConfirm(); } finally { setBusy(false); }
            }}
          >
            {confirmText}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
