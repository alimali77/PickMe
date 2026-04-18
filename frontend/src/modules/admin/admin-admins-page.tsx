import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { Plus, ShieldCheck, Trash2, Pencil } from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
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
import { adminApi } from './admin-api';
import { EmptyState, PageHeader, SkeletonRows } from './admin-ui';
import { useAuthStore } from '@/lib/auth-store';
import { formatDate } from '@/lib/utils';
import type { AdminUserDto } from '@pickme/shared/api-types';

export function AdminAdminsPage() {
  const qc = useQueryClient();
  const currentUser = useAuthStore((s) => s.user);
  const [createOpen, setCreateOpen] = useState(false);
  const [editing, setEditing] = useState<AdminUserDto | null>(null);
  const [deleting, setDeleting] = useState<AdminUserDto | null>(null);

  const { data, isLoading } = useQuery({
    queryKey: ['admin-admins'],
    queryFn: () => adminApi.admins.list(),
  });

  const deleteMut = useMutation({
    mutationFn: (id: string) => adminApi.admins.remove(id),
    onSuccess: () => { toast.success('Yönetici silindi.'); setDeleting(null); qc.invalidateQueries({ queryKey: ['admin-admins'] }); },
    onError: (e: Error) => toast.error(e.message),
  });

  return (
    <div>
      <PageHeader
        title="Yöneticiler"
        description="Admin paneline erişimi olan kullanıcılar. Kendinizi veya son yöneticiyi silemezsiniz."
        actions={<Button onClick={() => setCreateOpen(true)}><Plus /> Yeni Yönetici</Button>}
      />

      {isLoading ? <SkeletonRows /> : (data?.length ?? 0) === 0 ? (
        <EmptyState icon={ShieldCheck} title="Yönetici yok" description="Sistemde en az bir yönetici olmalıdır." />
      ) : (
        <Card>
          <CardContent className="p-0">
            <ul className="divide-y">
              {data!.map((a) => {
                const isSelf = currentUser?.email === a.email;
                return (
                  <li key={a.id} className="p-4 flex items-start gap-4">
                    <div className="flex h-10 w-10 items-center justify-center rounded-lg bg-primary/10 text-primary">
                      <ShieldCheck className="size-5" aria-hidden />
                    </div>
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2">
                        <span className="font-semibold">{a.fullName}</span>
                        {isSelf ? <span className="text-[11px] text-muted-foreground">(siz)</span> : null}
                      </div>
                      <div className="text-xs text-muted-foreground">{a.email}</div>
                      <div className="text-[11px] text-muted-foreground">Kayıt: {formatDate(a.createdAtUtc)}</div>
                    </div>
                    <div className="flex gap-1">
                      <Button variant="ghost" size="icon" onClick={() => setEditing(a)} aria-label="Düzenle"><Pencil className="size-4" /></Button>
                      <Button
                        variant="ghost" size="icon"
                        onClick={() => setDeleting(a)}
                        aria-label="Sil"
                        disabled={isSelf}
                        className="text-destructive hover:text-destructive disabled:opacity-30"
                      >
                        <Trash2 className="size-4" />
                      </Button>
                    </div>
                  </li>
                );
              })}
            </ul>
          </CardContent>
        </Card>
      )}

      <CreateAdminDialog open={createOpen} onOpenChange={setCreateOpen} onDone={() => qc.invalidateQueries({ queryKey: ['admin-admins'] })} />
      <EditAdminDialog
        admin={editing}
        onOpenChange={(v) => { if (!v) setEditing(null); }}
        onDone={() => qc.invalidateQueries({ queryKey: ['admin-admins'] })}
      />

      <Dialog open={!!deleting} onOpenChange={(v) => { if (!v) setDeleting(null); }}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>Yöneticiyi sil</DialogTitle>
            <DialogDescription>{deleting?.fullName} sistemden kaldırılacak.</DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setDeleting(null)}>Vazgeç</Button>
            <Button variant="destructive" onClick={() => deleting && deleteMut.mutate(deleting.id)} loading={deleteMut.isPending}>Sil</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}

function CreateAdminDialog({ open, onOpenChange, onDone }: { open: boolean; onOpenChange: (v: boolean) => void; onDone: () => void }) {
  const [form, setForm] = useState({ fullName: '', email: '', password: '' });
  const [errors, setErrors] = useState<Record<string, string>>({});

  const mut = useMutation({
    mutationFn: () => adminApi.admins.create(form),
    onSuccess: () => {
      toast.success('Yönetici eklendi.');
      onOpenChange(false);
      setForm({ fullName: '', email: '', password: '' }); setErrors({});
      onDone();
    },
    onError: (e: Error & { errors?: Record<string, string[]> }) => {
      toast.error(e.message);
      if (e.errors) setErrors(Object.fromEntries(Object.entries(e.errors).map(([k, v]) => [k, v[0]])));
    },
  });

  return (
    <Dialog open={open} onOpenChange={(v) => { onOpenChange(v); if (!v) setForm({ fullName: '', email: '', password: '' }); }}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Yeni Yönetici</DialogTitle>
          <DialogDescription>Admin paneline erişim yetkisi olan bir kullanıcı ekleyin.</DialogDescription>
        </DialogHeader>
        <div className="space-y-4">
          <FormField label="Ad Soyad" required error={errors.fullName}>
            <Input value={form.fullName} onChange={(e) => setForm({ ...form, fullName: e.target.value })} />
          </FormField>
          <FormField label="E-posta" required error={errors.email}>
            <Input type="email" value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} />
          </FormField>
          <FormField label="Başlangıç Şifresi" required error={errors.password} description="En az 8 karakter, büyük+küçük harf ve rakam">
            <Input type="text" value={form.password} onChange={(e) => setForm({ ...form, password: e.target.value })} />
          </FormField>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>Vazgeç</Button>
          <Button onClick={() => mut.mutate()} loading={mut.isPending}>Oluştur</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}

function EditAdminDialog({ admin, onOpenChange, onDone }: { admin: AdminUserDto | null; onOpenChange: (v: boolean) => void; onDone: () => void }) {
  const [fullName, setFullName] = useState('');
  const open = !!admin;
  const [syncedId, setSyncedId] = useState<string | null>(null);
  if (admin && syncedId !== admin.id) {
    setSyncedId(admin.id);
    setFullName(admin.fullName);
  }

  const mut = useMutation({
    mutationFn: () => adminApi.admins.update(admin!.id, fullName),
    onSuccess: () => {
      toast.success('Güncellendi.');
      onOpenChange(false);
      setSyncedId(null);
      onDone();
    },
    onError: (e: Error) => toast.error(e.message),
  });

  return (
    <Dialog open={open} onOpenChange={(v) => { onOpenChange(v); if (!v) setSyncedId(null); }}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Yöneticiyi Düzenle</DialogTitle>
        </DialogHeader>
        <FormField label="Ad Soyad" required>
          <Input value={fullName} onChange={(e) => setFullName(e.target.value)} />
        </FormField>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>Vazgeç</Button>
          <Button onClick={() => mut.mutate()} loading={mut.isPending}>Kaydet</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
