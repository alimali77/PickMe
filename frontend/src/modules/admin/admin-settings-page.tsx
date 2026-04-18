import { useEffect, useState } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { toast } from 'sonner';
import { Save, MessageCircle, Mail, Phone, MapPin, Clock, Globe, KeyRound } from 'lucide-react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { FormField } from '@/components/ui/form-field';
import { adminApi } from './admin-api';
import { PageHeader, SkeletonRows } from './admin-ui';

/**
 * Admin sistem ayarları sayfası — her ayar key-value olarak saklanır.
 * Sensitive olanlar (API key, SMTP parolası) masked olarak gelir.
 * Placeholder boş gelirse "****" ile doldurulmaz; kullanıcı değer girmezse değişmez.
 */
export function AdminSettingsPage() {
  const qc = useQueryClient();
  const { data, isLoading } = useQuery({
    queryKey: ['admin-settings'],
    queryFn: () => adminApi.settings.list(),
  });

  const [values, setValues] = useState<Record<string, string>>({});

  useEffect(() => {
    if (data) {
      const initial: Record<string, string> = {};
      for (const s of data) initial[s.key] = s.isSensitive ? '' : s.value;
      setValues(initial);
    }
  }, [data]);

  const mut = useMutation({
    mutationFn: () => {
      // Sadece dolu olan değerleri gönder (boş bırakılan sensitive değerler değişmemeli)
      const toSend: Record<string, string> = {};
      for (const [k, v] of Object.entries(values)) if (v !== '') toSend[k] = v;
      return adminApi.settings.update(toSend);
    },
    onSuccess: () => {
      toast.success('Ayarlar güncellendi.');
      qc.invalidateQueries({ queryKey: ['admin-settings'] });
    },
    onError: (e: Error) => toast.error(e.message),
  });

  const groups: { title: string; icon: typeof MessageCircle; fields: { key: string; label: string; placeholder?: string; hint?: string; type?: string }[] }[] = [
    {
      title: 'İletişim', icon: MessageCircle,
      fields: [
        { key: 'whatsapp.number', label: 'WhatsApp Numarası', placeholder: '905551234567', hint: 'Uluslararası format, + işareti olmadan' },
        { key: 'contact.email', label: 'E-posta', placeholder: 'info@pickme.local' },
        { key: 'contact.phone', label: 'Telefon', placeholder: '+90 (555) 123 45 67' },
        { key: 'contact.address', label: 'Adres', placeholder: 'İstanbul, Türkiye' },
        { key: 'working.hours', label: 'Çalışma Saatleri', placeholder: '7/24' },
      ],
    },
    {
      title: 'Entegrasyonlar', icon: Globe,
      fields: [
        { key: 'google.maps.api_key', label: 'Google Maps API Key', hint: 'Rezervasyon formunda Places Autocomplete için', placeholder: 'Yeni değer girin (boş = değiştirme)' },
        { key: 'ga4.id', label: 'Google Analytics 4 ID', placeholder: 'G-XXXXXXX' },
      ],
    },
  ];

  const iconFor = (key: string) => {
    if (key.startsWith('contact.email')) return Mail;
    if (key.startsWith('contact.phone')) return Phone;
    if (key.startsWith('contact.address')) return MapPin;
    if (key.startsWith('working.hours')) return Clock;
    if (key.startsWith('whatsapp')) return MessageCircle;
    if (key.includes('api_key')) return KeyRound;
    return Globe;
  };

  if (isLoading) {
    return <><PageHeader title="Sistem Ayarları" /><SkeletonRows count={6} /></>;
  }

  return (
    <div>
      <PageHeader
        title="Sistem Ayarları"
        description="Platform genelindeki ayarları buradan yönetin. Hassas değerler (API key'ler) maskelenir."
        actions={<Button onClick={() => mut.mutate()} loading={mut.isPending}><Save /> Değişiklikleri Kaydet</Button>}
      />

      <div className="space-y-6 max-w-3xl">
        {groups.map((g) => (
          <Card key={g.title}>
            <CardHeader>
              <CardTitle className="text-base flex items-center gap-2">
                <g.icon className="size-4 text-primary" aria-hidden /> {g.title}
              </CardTitle>
              <CardDescription>Bu gruptaki ayarları güncelleyin.</CardDescription>
            </CardHeader>
            <CardContent className="space-y-4">
              {g.fields.map((f) => {
                const setting = data?.find((s) => s.key === f.key);
                const Icon = iconFor(f.key);
                return (
                  <FormField
                    key={f.key}
                    label={f.label}
                    description={f.hint ?? (setting?.isSensitive ? 'Mevcut değer: ' + setting.value + ' — değiştirmek için yeni değer girin' : undefined)}
                  >
                    <div className="relative">
                      <Icon className="absolute left-3 top-1/2 -translate-y-1/2 size-4 text-muted-foreground pointer-events-none" aria-hidden />
                      <Input
                        className="pl-9"
                        type={f.type ?? 'text'}
                        placeholder={f.placeholder}
                        value={values[f.key] ?? ''}
                        onChange={(e) => setValues((v) => ({ ...v, [f.key]: e.target.value }))}
                      />
                    </div>
                  </FormField>
                );
              })}
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  );
}
