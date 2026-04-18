import { Link } from 'react-router-dom';
import { ArrowRight, CarFront, ParkingCircle, ShieldCheck, Clock, Star, CheckCircle2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { SEOHead } from '@/components/shared/seo-head';

const steps = [
  { icon: CarFront, title: 'Rezervasyon Aç', desc: 'Tarih, saat ve konumu seç; 30 saniyede rezervasyonunu aç.' },
  { icon: ShieldCheck, title: 'Şoför Atansın', desc: 'Ekibimiz en uygun şoförü sana atar, bilgilendirme maili gelir.' },
  { icon: Clock, title: 'Yolculuğun Başlasın', desc: 'Şoförün yolda; anlık olarak görev durumunu takip edebilirsin.' },
  { icon: Star, title: 'Değerlendir', desc: 'Tamamlanan yolculuğu 1-5 yıldız ve yorumla değerlendir.' },
];

const usps = [
  'Deneyimli, ehliyet sahibi şoförler',
  'Sabit ve şeffaf fiyatlandırma',
  'İstanbul genelinde 7/24 hizmet',
  'Rezervasyon onayı 5 dakika içinde',
];

export function HomePage() {
  return (
    <>
      <SEOHead
        title="Pick Me – İstanbul Şoför & Vale Hizmeti"
        description="İstanbul'da profesyonel şoför ve vale hizmeti. Hızlı rezervasyon, güvenli sürücüler, şeffaf fiyat. 30 saniyede rezervasyon yapın."
        canonicalPath="/"
        jsonLd={{
          '@context': 'https://schema.org',
          '@type': 'LocalBusiness',
          name: 'Pick Me',
          areaServed: 'İstanbul',
          description: 'Şoför ve vale hizmeti rezervasyon platformu.',
        }}
      />

      <section className="relative overflow-hidden">
        <div className="container py-16 md:py-24 lg:py-32 text-center">
          <div className="mx-auto inline-flex items-center gap-2 rounded-full border bg-card px-4 py-1.5 text-xs font-medium text-muted-foreground shadow-soft">
            <span className="flex h-2 w-2 rounded-full bg-emerald-500" />
            7/24 aktif hizmet — İstanbul genelinde
          </div>

          <h1 className="mx-auto mt-6 max-w-3xl text-4xl md:text-5xl lg:text-6xl font-bold tracking-tight">
            Şoförünü <span className="text-primary">dakikalar içinde</span> ayarla
          </h1>
          <p className="mx-auto mt-5 max-w-2xl text-lg text-muted-foreground">
            İstanbul'un her yerinde profesyonel şoför ve vale hizmeti. Hızlı rezervasyon, güvenli sürücüler, net fiyat.
          </p>

          <div className="mt-8 flex flex-col sm:flex-row gap-3 justify-center">
            <Button asChild size="xl">
              <Link to="/rezervasyon">
                Hemen Rezervasyon Yap
                <ArrowRight />
              </Link>
            </Button>
            <Button asChild size="xl" variant="outline">
              <Link to="/hizmetler/sofor">Hizmetleri İncele</Link>
            </Button>
          </div>

          <ul className="mx-auto mt-10 flex flex-wrap justify-center gap-x-6 gap-y-2 text-sm text-muted-foreground">
            {usps.map((u) => (
              <li key={u} className="flex items-center gap-2">
                <CheckCircle2 className="size-4 text-primary" aria-hidden />
                {u}
              </li>
            ))}
          </ul>
        </div>
        <div className="pointer-events-none absolute inset-x-0 top-0 -z-10 h-[600px] bg-gradient-to-b from-primary/10 to-transparent" />
      </section>

      <section className="border-y bg-muted/30">
        <div className="container py-16 md:py-20">
          <div className="text-center mb-12">
            <h2 className="text-3xl md:text-4xl font-bold tracking-tight">Nasıl Çalışır?</h2>
            <p className="mt-3 text-muted-foreground">Dört basit adımda yolculuğun başlar.</p>
          </div>
          <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-4">
            {steps.map((s, i) => (
              <div key={s.title} className="relative rounded-xl border bg-card p-6 shadow-soft">
                <div className="flex h-11 w-11 items-center justify-center rounded-lg bg-primary/10 text-primary">
                  <s.icon className="size-5" aria-hidden />
                </div>
                <div className="absolute top-6 right-6 text-4xl font-bold text-muted/40 tabular-nums">
                  {String(i + 1).padStart(2, '0')}
                </div>
                <h3 className="mt-4 text-lg font-semibold">{s.title}</h3>
                <p className="mt-2 text-sm text-muted-foreground">{s.desc}</p>
              </div>
            ))}
          </div>
        </div>
      </section>

      <section className="container py-16 md:py-20">
        <div className="text-center mb-12">
          <h2 className="text-3xl md:text-4xl font-bold tracking-tight">Hizmetlerimiz</h2>
          <p className="mt-3 text-muted-foreground">Her ihtiyacına uygun çözüm.</p>
        </div>
        <div className="grid gap-6 md:grid-cols-2">
          <Link to="/hizmetler/sofor" className="group rounded-2xl border bg-card p-8 shadow-soft hover:shadow-elevated transition-shadow">
            <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-primary/10 text-primary">
              <CarFront className="size-6" aria-hidden />
            </div>
            <h3 className="mt-6 text-2xl font-semibold">Şoför Hizmeti</h3>
            <p className="mt-3 text-muted-foreground">
              Aracınızı güvenle kullanıp sizi istediğiniz yere götürecek profesyonel şoförler.
            </p>
            <div className="mt-6 flex items-center gap-1 text-sm font-medium text-primary">
              Detayları Gör
              <ArrowRight className="size-4 transition-transform group-hover:translate-x-1" aria-hidden />
            </div>
          </Link>
          <Link to="/hizmetler/vale" className="group rounded-2xl border bg-card p-8 shadow-soft hover:shadow-elevated transition-shadow">
            <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-primary/10 text-primary">
              <ParkingCircle className="size-6" aria-hidden />
            </div>
            <h3 className="mt-6 text-2xl font-semibold">Vale Hizmeti</h3>
            <p className="mt-3 text-muted-foreground">
              Restoran, otel, etkinlik ve özel organizasyonlar için profesyonel vale hizmeti.
            </p>
            <div className="mt-6 flex items-center gap-1 text-sm font-medium text-primary">
              Detayları Gör
              <ArrowRight className="size-4 transition-transform group-hover:translate-x-1" aria-hidden />
            </div>
          </Link>
        </div>
      </section>

      <section className="border-t">
        <div className="container py-16 md:py-20 text-center">
          <h2 className="text-3xl md:text-4xl font-bold tracking-tight">Hazırsan, başlayalım</h2>
          <p className="mx-auto mt-4 max-w-xl text-muted-foreground">
            Rezervasyonunu şimdi aç; şoförün atandığında seni bilgilendirelim.
          </p>
          <Button asChild size="xl" className="mt-8">
            <Link to="/rezervasyon">Rezervasyon Yap</Link>
          </Button>
        </div>
      </section>
    </>
  );
}
