import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { Plus, Trash2, Mail } from 'lucide-react';
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
import { adminApi } from './admin-api';
import { EmptyState, PageHeader, SkeletonRows } from './admin-ui';

export function AdminRecipientsPage() {
  const qc = useQueryClient();
  const [addOpen, setAddOpen] = useState(false);

  const { data, isLoading } = useQuery({
    queryKey: ['admin-recipients'],
    queryFn: () => adminApi.recipients.list(),
  });

  const toggleMut = useMutation({
    mutationFn: (p: { id: string; active: boolean }) => adminApi.recipients.setActive(p.id, p.active),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['admin-recipients'] }); toast.success('Güncellendi.'); },
    onError: (e: Error) => toast.error(e.message),
  });

  const removeMut = useMutation({
    mutationFn: (id: string) => adminApi.recipients.remove(id),
    onSuccess: () => { qc.invalidateQueries({ queryKey: ['admin-recipients'] }); toast.success('Silindi.'); },
    onError: (e: Error) => toast.error(e.message),
  });

  return (
    <div>
      <PageHeader
        title="Yönetici Bildirim E-postaları"
        description="Yeni rezervasyonlar geldiğinde bildirim alacak e-postaları yönetin. En az 1 aktif kayıt zorunludur."
        actions={<Button onClick={() => setAddOpen(true)}><Plus /> Yeni E-posta</Button>}
      />

      {isLoading ? <SkeletonRows /> : (data?.length ?? 0) === 0 ? (
        <EmptyState icon={Mail} title="Bildirim alıcısı yok" description="Yeni rezervasyon bildirimleri için en az bir e-posta adresi eklemelisiniz." action={<Button onClick={() => setAddOpen(true)}><Plus /> E-posta Ekle</Button>} />
      ) : (
        <Card>
          <CardContent className="p-0">
            <ul className="divide-y">
              {data!.map((r) => (
                <li key={r.id} className="p-4 flex items-center gap-4">
                  <Mail className="size-5 text-muted-foreground" aria-hidden />
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2">
                      <span className="font-medium truncate">{r.email}</span>
                      {r.isActive ? <Badge variant="success">Aktif</Badge> : <Badge variant="outline">Pasif</Badge>}
                    </div>
                  </div>
                  <Button variant="outline" size="sm" onClick={() => toggleMut.mutate({ id: r.id, active: !r.isActive })} loading={toggleMut.isPending && toggleMut.variables?.id === r.id}>
                    {r.isActive ? 'Pasifleştir' : 'Aktifleştir'}
                  </Button>
                  <Button variant="ghost" size="icon" onClick={() => removeMut.mutate(r.id)} aria-label="Sil" className="text-destructive hover:text-destructive">
                    <Trash2 className="size-4" />
                  </Button>
                </li>
              ))}
            </ul>
          </CardContent>
        </Card>
      )}

      <AddRecipientDialog open={addOpen} onOpenChange={setAddOpen} onDone={() => qc.invalidateQueries({ queryKey: ['admin-recipients'] })} />
    </div>
  );
}

function AddRecipientDialog({ open, onOpenChange, onDone }: { open: boolean; onOpenChange: (v: boolean) => void; onDone: () => void }) {
  const [email, setEmail] = useState('');
  const [err, setErr] = useState<string | undefined>();

  const mut = useMutation({
    mutationFn: () => adminApi.recipients.add(email),
    onSuccess: () => {
      toast.success('E-posta eklendi.');
      onOpenChange(false);
      setEmail(''); setErr(undefined);
      onDone();
    },
    onError: (e: Error & { errors?: Record<string, string[]> }) => {
      toast.error(e.message);
      setErr(e.errors?.email?.[0] ?? e.message);
    },
  });

  return (
    <Dialog open={open} onOpenChange={(v) => { onOpenChange(v); if (!v) { setEmail(''); setErr(undefined); } }}>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Yeni Bildirim E-postası</DialogTitle>
          <DialogDescription>Yeni rezervasyon geldiğinde buraya bildirim gönderilir.</DialogDescription>
        </DialogHeader>
        <FormField label="E-posta" required error={err}>
          <Input type="email" value={email} onChange={(e) => setEmail(e.target.value)} placeholder="ornek@pickme.local" />
        </FormField>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>Vazgeç</Button>
          <Button onClick={() => mut.mutate()} loading={mut.isPending}>Ekle</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
