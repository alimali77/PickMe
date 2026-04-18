import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { toast } from 'sonner';
import { CarFront, ParkingCircle, CheckCircle2, ArrowRight } from 'lucide-react';
import { createReservationSchema, type CreateReservationInput } from '@pickme/shared/validation';
import { ServiceType, SERVICE_TYPE_LABELS } from '@pickme/shared/constants';
import { Button } from '@/components/ui/button';
import { Textarea } from '@/components/ui/textarea';
import { Input } from '@/components/ui/input';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { FormField } from '@/components/ui/form-field';
import { SEOHead } from '@/components/shared/seo-head';
import { cn } from '@/lib/utils';
import { LocationPicker, type SelectedPlace } from './location-picker';
import { reservationsApi } from './reservations-api';

function nowPlusMinutesLocal(minutes: number) {
  const d = new Date(Date.now() + minutes * 60_000);
  const pad = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}`;
}

export function ReservationFormPage() {
  const navigate = useNavigate();
  const [place, setPlace] = useState<SelectedPlace | null>(null);
  const [submitted, setSubmitted] = useState<{ id: string } | null>(null);

  const form = useForm<CreateReservationInput>({
    resolver: zodResolver(createReservationSchema),
    defaultValues: {
      serviceType: ServiceType.Driver,
      reservationDateTimeUtc: new Date(Date.now() + 60 * 60_000).toISOString(),
      address: '',
      lat: 0,
      lng: 0,
      note: '',
      placeSelectedFromAutocomplete: false,
    },
  });

  const [localWhen, setLocalWhen] = useState(nowPlusMinutesLocal(60));

  const onSubmit = async (values: CreateReservationInput) => {
    try {
      const id = await reservationsApi.create({
        serviceType: values.serviceType,
        reservationDateTimeUtc: new Date(values.reservationDateTimeUtc).toISOString(),
        address: values.address,
        lat: values.lat,
        lng: values.lng,
        note: values.note || undefined,
        placeSelectedFromAutocomplete: values.placeSelectedFromAutocomplete,
      });
      setSubmitted({ id });
      toast.success('Rezervasyonunuz alındı!');
    } catch (err) {
      const e = err as Error & { errors?: Record<string, string[]> };
      toast.error(e.message);
      if (e.errors) {
        for (const [field, msgs] of Object.entries(e.errors)) {
          form.setError(field as keyof CreateReservationInput, { message: msgs[0] });
        }
      }
    }
  };

  if (submitted) {
    return (
      <>
        <SEOHead title="Rezervasyon Alındı – Pick Me" />
        <div className="container py-16 md:py-24 max-w-xl text-center">
          <div className="mx-auto flex size-14 items-center justify-center rounded-full bg-emerald-100 text-emerald-600">
            <CheckCircle2 className="size-8" aria-hidden />
          </div>
          <h1 className="mt-5 text-3xl md:text-4xl font-bold tracking-tight">Rezervasyonunuz Alındı</h1>
          <p className="mx-auto mt-4 max-w-md text-muted-foreground">
            Ekibimiz size en uygun şoförü atayacak ve e-posta ile bilgilendireceğiz. Rezervasyon numaranız:{' '}
            <span className="font-mono text-foreground">{submitted.id.slice(0, 8).toUpperCase()}</span>
          </p>
          <div className="mt-8 flex flex-col sm:flex-row gap-3 justify-center">
            <Button asChild size="lg">
              <a href="/hesabim/rezervasyonlar">Rezervasyonlarım</a>
            </Button>
            <Button variant="outline" size="lg" onClick={() => { setSubmitted(null); form.reset(); setPlace(null); setLocalWhen(nowPlusMinutesLocal(60)); navigate('/rezervasyon'); }}>
              Yeni Rezervasyon
            </Button>
          </div>
        </div>
      </>
    );
  }

  return (
    <>
      <SEOHead title="Rezervasyon Yap – Pick Me" canonicalPath="/rezervasyon" />

      <div className="container py-12 md:py-16 max-w-2xl">
        <Card>
          <CardHeader className="text-center">
            <CardTitle>Rezervasyon Yap</CardTitle>
            <CardDescription>30 saniyede bilgilerinizi girin, biz sizi uygun şoförle buluşturalım.</CardDescription>
          </CardHeader>
          <CardContent>
            <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-5">
              <div>
                <div className="text-sm font-medium mb-2">Hizmet Türü</div>
                <Controller
                  control={form.control}
                  name="serviceType"
                  render={({ field }) => (
                    <div className="grid grid-cols-2 gap-3">
                      {(['Driver', 'Valet'] as const).map((t) => {
                        const selected = field.value === t;
                        const Icon = t === 'Driver' ? CarFront : ParkingCircle;
                        return (
                          <button
                            type="button"
                            key={t}
                            onClick={() => field.onChange(t)}
                            aria-pressed={selected}
                            className={cn(
                              'flex items-center gap-3 rounded-xl border p-4 text-left transition-all',
                              selected ? 'border-primary bg-primary/5 shadow-soft' : 'hover:bg-accent',
                            )}
                          >
                            <div className={cn('flex h-10 w-10 items-center justify-center rounded-lg', selected ? 'bg-primary text-primary-foreground' : 'bg-accent text-primary')}>
                              <Icon className="size-5" aria-hidden />
                            </div>
                            <div className="min-w-0">
                              <div className="font-semibold">{SERVICE_TYPE_LABELS[t]}</div>
                              <div className="text-xs text-muted-foreground truncate">
                                {t === 'Driver' ? 'Aracınızı şoför kullansın' : 'Etkinlik + restoran vale'}
                              </div>
                            </div>
                          </button>
                        );
                      })}
                    </div>
                  )}
                />
              </div>

              <FormField label="Tarih ve Saat" required error={form.formState.errors.reservationDateTimeUtc?.message} description="Şu andan en az 30 dakika sonra">
                <Input
                  type="datetime-local"
                  value={localWhen}
                  onChange={(e) => {
                    setLocalWhen(e.target.value);
                    if (e.target.value) {
                      const iso = new Date(e.target.value).toISOString();
                      form.setValue('reservationDateTimeUtc', iso, { shouldValidate: true });
                    }
                  }}
                  min={nowPlusMinutesLocal(30)}
                />
              </FormField>

              <div className="space-y-2">
                <div className="text-sm font-medium">Konum <span className="text-destructive">*</span></div>
                <Controller
                  control={form.control}
                  name="address"
                  render={({ field }) => (
                    <LocationPicker
                      value={place}
                      onChange={(p) => {
                        setPlace(p);
                        form.setValue('address', p?.address ?? '', { shouldValidate: true });
                        form.setValue('lat', p?.lat ?? 0, { shouldValidate: true });
                        form.setValue('lng', p?.lng ?? 0, { shouldValidate: true });
                        form.setValue('placeSelectedFromAutocomplete', !!p, { shouldValidate: true });
                        field.onChange(p?.address ?? '');
                      }}
                      error={
                        form.formState.errors.placeSelectedFromAutocomplete?.message ??
                        form.formState.errors.address?.message
                      }
                    />
                  )}
                />
              </div>

              <FormField
                label="Not (opsiyonel)"
                error={form.formState.errors.note?.message}
                description="Şoförümüzle paylaşmak istediğiniz bilgi (ör: bekleme noktası, bagaj durumu)"
              >
                <Textarea
                  {...form.register('note')}
                  maxLength={500}
                  placeholder="Örn: Otoparkın 2. katında bekleyeceğim, mavi aracım var..."
                  rows={3}
                />
              </FormField>

              <div className="pt-2">
                <Button type="submit" size="xl" className="w-full" loading={form.formState.isSubmitting}>
                  {form.formState.isSubmitting ? 'Gönderiliyor...' : 'Rezervasyonu Onayla'}
                  {!form.formState.isSubmitting ? <ArrowRight /> : null}
                </Button>
                <p className="mt-3 text-center text-xs text-muted-foreground">
                  Rezervasyonunuz onaylandığında e-posta ile bilgilendirileceksiniz.
                </p>
              </div>
            </form>
          </CardContent>
        </Card>
      </div>
    </>
  );
}
