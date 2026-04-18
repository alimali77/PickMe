import { SEOHead } from '@/components/shared/seo-head';

interface LegalProps {
  kind: 'kvkk' | 'gizlilik' | 'kullanim-sartlari';
}

const META = {
  kvkk: {
    title: 'KVKK Aydınlatma Metni – Pick Me',
    heading: 'KVKK Aydınlatma Metni',
  },
  gizlilik: {
    title: 'Gizlilik Politikası – Pick Me',
    heading: 'Gizlilik Politikası',
  },
  'kullanim-sartlari': {
    title: 'Kullanım Şartları – Pick Me',
    heading: 'Kullanım Şartları',
  },
} as const;

export function LegalPage({ kind }: LegalProps) {
  const m = META[kind];
  return (
    <>
      <SEOHead title={m.title} description={m.heading} canonicalPath={`/${kind}`} />
      <section className="container py-16 md:py-24 max-w-3xl">
        <h1 className="text-4xl md:text-5xl font-bold tracking-tight">{m.heading}</h1>
        <div className="prose prose-neutral dark:prose-invert mt-8 space-y-6 text-muted-foreground">
          <p>
            Bu sayfa içeriği müşteri tarafından sağlanacak hukuki metinle doldurulacaktır. Lütfen son
            metni ilgili kanalla iletin; yayına alınacaktır.
          </p>
          <p className="text-sm">Son güncelleme: {new Date().toLocaleDateString('tr-TR')}</p>
        </div>
      </section>
    </>
  );
}
