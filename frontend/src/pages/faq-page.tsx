import { useState } from 'react';
import { ChevronDown } from 'lucide-react';
import { SEOHead } from '@/components/shared/seo-head';
import { cn } from '@/lib/utils';

const SAMPLE_FAQS = [
  {
    q: 'Rezervasyon açtıktan sonra onay süresi ne kadar?',
    a: 'Çalışma saatleri içerisinde rezervasyonunuz genellikle 5-15 dakika içinde onaylanır. Şoför atandığında e-postayla bilgilendirilirsiniz.',
  },
  {
    q: 'Rezervasyonumu iptal edebilir miyim?',
    a: 'Şoför ataması yapılmadan önce (Beklemede durumu) dilediğiniz zaman iptal edebilirsiniz. Atama sonrası iptal için lütfen iletişime geçiniz.',
  },
  {
    q: 'Ödeme nasıl gerçekleşir?',
    a: 'Ödemeler şu anda şoförümüze yolculuk sonunda nakit veya karşılıklı anlaşılan yöntemle yapılır. Çevrimiçi ödeme yol haritasında yer almaktadır.',
  },
  {
    q: 'İstanbul dışına hizmet var mı?',
    a: 'Şu an hizmet bölgemiz İstanbul’dur. Şehirlerarası talepler için iletişim formundan bize yazabilirsiniz.',
  },
  {
    q: 'Şoförü nasıl değerlendirebilirim?',
    a: 'Yolculuk tamamlandığında size bir e-posta gönderilir. Giriş yapıp 1-5 yıldız arasında puan ve yorum bırakabilirsiniz.',
  },
];

export function FaqPage() {
  const [open, setOpen] = useState<number | null>(0);

  return (
    <>
      <SEOHead
        title="Sıkça Sorulan Sorular – Pick Me"
        description="Şoför ve vale hizmeti, rezervasyon süreci ve ödeme hakkında sıkça sorulan sorular."
        canonicalPath="/sss"
        jsonLd={{
          '@context': 'https://schema.org',
          '@type': 'FAQPage',
          mainEntity: SAMPLE_FAQS.map((f) => ({
            '@type': 'Question',
            name: f.q,
            acceptedAnswer: { '@type': 'Answer', text: f.a },
          })),
        }}
      />

      <section className="container py-16 md:py-24 max-w-3xl">
        <h1 className="text-4xl md:text-5xl font-bold tracking-tight">Sıkça Sorulan Sorular</h1>
        <p className="mt-4 text-lg text-muted-foreground">
          Sorunuzun cevabını bulamazsanız <a className="text-primary underline-offset-2 hover:underline" href="/iletisim">iletişim formundan</a> bize yazabilirsiniz.
        </p>

        <ul className="mt-10 divide-y rounded-xl border bg-card shadow-soft overflow-hidden">
          {SAMPLE_FAQS.map((f, i) => {
            const isOpen = open === i;
            return (
              <li key={f.q}>
                <button
                  type="button"
                  onClick={() => setOpen(isOpen ? null : i)}
                  aria-expanded={isOpen}
                  className="flex w-full items-center justify-between gap-4 p-5 text-left hover:bg-accent/40 transition-colors"
                >
                  <span className="font-medium">{f.q}</span>
                  <ChevronDown className={cn('size-5 text-muted-foreground transition-transform', isOpen && 'rotate-180')} aria-hidden />
                </button>
                {isOpen ? (
                  <div className="px-5 pb-5 -mt-1 text-muted-foreground animate-fade-in">{f.a}</div>
                ) : null}
              </li>
            );
          })}
        </ul>
      </section>
    </>
  );
}
