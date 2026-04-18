import { useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { Mail, MailOpen, Inbox, Phone } from 'lucide-react';
import { Card, CardContent } from '@/components/ui/card';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import { adminApi } from './admin-api';
import { EmptyState, PageHeader, SkeletonRows } from './admin-ui';
import { formatDate } from '@/lib/utils';
import type { ContactMessageDto } from '@pickme/shared/api-types';

export function AdminContactMessagesPage() {
  const qc = useQueryClient();
  const [unreadOnly, setUnreadOnly] = useState(false);
  const [page, setPage] = useState(1);
  const [viewing, setViewing] = useState<ContactMessageDto | null>(null);

  const { data, isLoading } = useQuery({
    queryKey: ['admin-contact-messages', { unreadOnly, page }],
    queryFn: () => adminApi.contact.list({ unreadOnly, page, pageSize: 20 }),
  });

  const markReadMut = useMutation({
    mutationFn: (id: string) => adminApi.contact.markRead(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['admin-contact-messages'] }),
    onError: (e: Error) => toast.error(e.message),
  });

  return (
    <div>
      <PageHeader
        title="İletişim Mesajları"
        description="Siteden gelen iletişim formu mesajları."
        actions={
          <Button variant={unreadOnly ? 'default' : 'outline'} onClick={() => { setUnreadOnly(v => !v); setPage(1); }}>
            {unreadOnly ? 'Tümünü Göster' : 'Sadece Okunmamış'}
          </Button>
        }
      />

      {isLoading ? <SkeletonRows /> : (data?.items.length ?? 0) === 0 ? (
        <EmptyState icon={Inbox} title="Mesaj yok" description="Henüz iletişim formundan mesaj gelmemiş." />
      ) : (
        <Card>
          <CardContent className="p-0">
            <ul className="divide-y">
              {data!.items.map((m) => (
                <li key={m.id}>
                  <button
                    type="button"
                    onClick={() => {
                      setViewing(m);
                      if (!m.isRead) markReadMut.mutate(m.id);
                    }}
                    className="w-full flex items-start gap-4 p-4 text-left hover:bg-accent/30 transition-colors"
                  >
                    {m.isRead ? <MailOpen className="size-5 text-muted-foreground mt-1" aria-hidden /> : <Mail className="size-5 text-primary mt-1" aria-hidden />}
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 flex-wrap">
                        <span className={m.isRead ? 'font-medium' : 'font-semibold'}>{m.subject}</span>
                        {!m.isRead ? <Badge variant="default">Yeni</Badge> : null}
                      </div>
                      <div className="mt-1 text-xs text-muted-foreground">
                        <span>{m.firstName}</span> · <span>{m.email}</span> · <span>{formatDate(m.createdAtUtc)}</span>
                      </div>
                      <p className="mt-1 text-sm text-muted-foreground line-clamp-1">{m.message}</p>
                    </div>
                  </button>
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

      <Dialog open={!!viewing} onOpenChange={(v) => { if (!v) setViewing(null); }}>
        <DialogContent className="max-w-xl">
          <DialogHeader>
            <DialogTitle>{viewing?.subject}</DialogTitle>
          </DialogHeader>
          {viewing ? (
            <div className="space-y-3">
              <div className="text-sm">
                <span className="text-muted-foreground">Gönderen:</span>{' '}
                <span className="font-medium">{viewing.firstName}</span>
              </div>
              <div className="flex gap-2 text-sm">
                <a href={`mailto:${viewing.email}`} className="inline-flex items-center gap-1 text-primary hover:underline">
                  <Mail className="size-3" /> {viewing.email}
                </a>
                <span className="text-muted-foreground">·</span>
                <a href={`tel:${viewing.phone}`} className="inline-flex items-center gap-1 text-primary hover:underline">
                  <Phone className="size-3" /> {viewing.phone}
                </a>
              </div>
              <div className="rounded-md border bg-muted/30 p-3 whitespace-pre-wrap text-sm">{viewing.message}</div>
              <div className="text-xs text-muted-foreground">Gönderim: {formatDate(viewing.createdAtUtc)}</div>
            </div>
          ) : null}
        </DialogContent>
      </Dialog>
    </div>
  );
}
