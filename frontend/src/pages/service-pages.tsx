import { Link } from 'react-router-dom';
import { ArrowRight, CheckCircle2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { SEOHead } from '@/components/shared/seo-head';

interface ServicePageProps {
  type: 'sofor' | 'vale';
}

const CONTENT = {
  sofor: {
    title: 'Şoför Hizmeti – Pick Me',
    description: 'Aracınızı güvenle kullanıp sizi istediğiniz yere götürecek profesyonel şoför hizmeti. İstanbul genelinde 7/24.',
    heading: 'Profesyonel Şoför Hizmeti',
    lead: 'Aracınızla seyahat ederken güvenli ve konforlu bir deneyim yaşayın. Ehliyetli, deneyimli ve kurumsal şoförlerimiz sizin için hazır.',
    features: [
      'Ehliyetli ve deneyimli kurumsal şoförler',
      'Kısa mesafe, şehir içi, havalimanı transferi',
      'Özel günler, iş toplantıları, etkinlik transferi',
      'Rezervasyonunuz onaylandığında şoför bilgileri ile birlikte mail gelir',
      'Aracınızı hiçbir zaman şoförümüzle yalnız bırakmak zorunda değilsiniz',
    ],
  },
  vale: {
    title: 'Vale Hizmeti – Pick Me',
    description: 'Restoran, otel, etkinlik ve özel organizasyonlar için profesyonel vale hizmeti. İstanbul genelinde 7/24.',
    heading: 'Profesyonel Vale Hizmeti',
    lead: 'Özel etkinlikler, restoranlar, oteller ve özel davetler için kurumsal vale hizmeti. Misafirleriniz ve siz konfor içinde olun.',
    features: [
      'Kurumsal ve etkinlik vale hizmeti',
      'Sigortalı, deneyimli vale personeli',
      'Açılış, düğün, özel davet, restoran ve otel organizasyonları',
      'Önceden rezervasyon ile planlı hizmet',
      'Tüm süreç şeffaf ve izlenebilir',
    ],
  },
} as const;

export function ServicePage({ type }: ServicePageProps) {
  const c = CONTENT[type];
  return (
    <>
      <SEOHead title={c.title} description={c.description} canonicalPath={`/hizmetler/${type}`} />
      <section className="container py-16 md:py-24">
        <div className="max-w-3xl">
          <h1 className="text-4xl md:text-5xl font-bold tracking-tight">{c.heading}</h1>
          <p className="mt-5 text-lg text-muted-foreground">{c.lead}</p>
        </div>

        <ul className="mt-10 grid gap-4 md:grid-cols-2 max-w-4xl">
          {c.features.map((f) => (
            <li key={f} className="flex items-start gap-3 rounded-xl border bg-card p-4 shadow-soft">
              <CheckCircle2 className="size-5 text-primary mt-0.5 shrink-0" aria-hidden />
              <span>{f}</span>
            </li>
          ))}
        </ul>

        <div className="mt-12 flex flex-col sm:flex-row gap-3">
          <Button asChild size="lg">
            <Link to="/rezervasyon">
              Rezervasyon Yap
              <ArrowRight />
            </Link>
          </Button>
          <Button asChild variant="outline" size="lg">
            <Link to="/iletisim">Soru Sor</Link>
          </Button>
        </div>
      </section>
    </>
  );
}
