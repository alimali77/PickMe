import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { Plus, Pencil, Trash2, HelpCircle } from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Textarea } from '@/components/ui/textarea';
import { Badge } from '@/components/ui/badge';
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
import type { FaqAdminDto } from '@pickme/shared/api-types';

export function AdminFaqsPage() {
  const qc = useQueryClient();
  const [editing, setEditing] = useState<FaqAdminDto | 'new' | null>(null);
  const [deleting, setDeleting] = useState<FaqAdminDto | null>(null);

  const { data, isLoading } = useQuery({
    queryKey: ['admin-faqs'],
    queryFn: () => adminApi.faqs.list(),
  });

  return (
    <div>
      <PageHeader
        title="Sıkça Sorulan Sorular"
        description="Kullanıcıların SSS sayfasında göreceği soruları yönetin."
        actions={<Button onClick={() => setEditing('new')}><Plus /> Yeni Soru</Button>}
      />

      {isLoading ? <SkeletonRows /> :
       (data?.length ?? 0) === 0 ? (
        <EmptyState icon={HelpCircle} title="Henüz soru yok" description="Kullanıcılarınızın sık sorduğu soruları ekleyin." action={<Button onClick={() => setEditing('new')}><Plus /> İlk Soruyu Ekle</Button>} />
       ) : (
        <Card>
          <CardContent className="p-0">
            <ul className="divide-y">
              {data!.map((f) => (
                <li key={f.id} className="p-4 flex items-start gap-4">
                  <div className="size-8 flex items-center justify-center rounded-md bg-primary/10 text-primary text-xs font-semibold">
                    #{f.displayOrder}
                  </div>
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 flex-wrap">
                      <span className="font-medium">{f.question}</span>
                      {!f.isActive ? <Badge variant="outline">Pasif</Badge> : null}
                    </div>
                    <p className="mt-1 text-sm text-muted-foreground line-clamp-2">{f.answer}</p>
                  </div>
                  <div className="flex gap-1">
                    <Button variant="ghost" size="icon" onClick={() => setEditing(f)} aria-label="Düzenle"><Pencil className="size-4" /></Button>
                    <Button variant="ghost" size="icon" onClick={() => setDeleting(f)} aria-label="Sil" className="text-destructive hover:text-destructive"><Trash2 className="size-4" /></Button>
                  </div>
                </li>
              ))}
            </ul>
          </CardContent>
        </Card>
       )}

      <FaqDialog
        mode={editing}
        onOpenChange={(v) => { if (!v) setEditing(null); }}
        onDone={() => qc.invalidateQueries({ queryKey: ['admin-faqs'] })}
      />

      <Dialog open={!!deleting} onOpenChange={(v) => { if (!v) setDeleting(null); }}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>SSS silinsin mi?</DialogTitle>
            <DialogDescription>Bu işlem geri alınamaz.</DialogDescription>
          </DialogHeader>
          <DialogFooter>
            <Button variant="outline" onClick={() => setDeleting(null)}>Vazgeç</Button>
            <Button variant="destructive" onClick={async () => {
              if (!deleting) return;
              try { await adminApi.faqs.remove(deleting.id); toast.success('Silindi.'); setDeleting(null); qc.invalidateQueries({ queryKey: ['admin-faqs'] }); }
              catch (e) { toast.error((e as Error).message); }
            }}>Sil</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}

function FaqDialog({
  mode, onOpenChange, onDone,
}: {
  mode: FaqAdminDto | 'new' | null;
  onOpenChange: (v: boolean) => void;
  onDone: () => void;
}) {
  const isNew = mode === 'new';
  const existing = mode && mode !== 'new' ? mode : null;
  const [form, setForm] = useState({ question: '', answer: '', displayOrder: 1, isActive: true });

  // Mode değiştiğinde formu hidrasyon et
  const open = mode !== null;
  const syncedId = existing?.id ?? (isNew ? 'new' : null);
  const [lastSyncedId, setLastSyncedId] = useState<string | null>(null);
  if (syncedId !== lastSyncedId && open) {
    setLastSyncedId(syncedId);
    setForm(existing
      ? { question: existing.question, answer: existing.answer, displayOrder: existing.displayOrder, isActive: existing.isActive }
      : { question: '', answer: '', displayOrder: 1, isActive: true });
  }

  const mut = useMutation<void, Error>({
    mutationFn: async () => {
      if (existing) {
        await adminApi.faqs.update(existing.id, form);
      } else {
        await adminApi.faqs.create({ question: form.question, answer: form.answer, displayOrder: form.displayOrder });
      }
    },
    onSuccess: () => {
      toast.success(existing ? 'Güncellendi.' : 'Eklendi.');
      onOpenChange(false);
      setLastSyncedId(null);
      onDone();
    },
    onError: (e: Error) => toast.error(e.message),
  });

  return (
    <Dialog open={open} onOpenChange={(v) => { onOpenChange(v); if (!v) setLastSyncedId(null); }}>
      <DialogContent className="max-w-2xl">
        <DialogHeader>
          <DialogTitle>{existing ? 'SSS Düzenle' : 'Yeni SSS'}</DialogTitle>
          <DialogDescription>Sıklıkla sorulan bir soru ekleyin veya düzenleyin.</DialogDescription>
        </DialogHeader>
        <div className="space-y-4">
          <FormField label="Soru" required>
            <Input value={form.question} onChange={(e) => setForm({ ...form, question: e.target.value })} maxLength={500} />
          </FormField>
          <FormField label="Cevap" required>
            <Textarea rows={5} value={form.answer} onChange={(e) => setForm({ ...form, answer: e.target.value })} maxLength={4000} />
          </FormField>
          <div className="grid grid-cols-2 gap-4">
            <FormField label="Sıralama" required description="Küçük rakam önde gelir">
              <Input type="number" min={0} value={form.displayOrder} onChange={(e) => setForm({ ...form, displayOrder: Number(e.target.value) || 0 })} />
            </FormField>
            {existing ? (
              <label className="flex items-center gap-2 self-end pb-2.5">
                <input type="checkbox" checked={form.isActive} onChange={(e) => setForm({ ...form, isActive: e.target.checked })} className="size-4 accent-primary" />
                <span className="text-sm">Aktif (SSS sayfasında göster)</span>
              </label>
            ) : null}
          </div>
        </div>
        <DialogFooter>
          <Button variant="outline" onClick={() => onOpenChange(false)}>Vazgeç</Button>
          <Button onClick={() => mut.mutate()} loading={mut.isPending}>{existing ? 'Kaydet' : 'Oluştur'}</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
