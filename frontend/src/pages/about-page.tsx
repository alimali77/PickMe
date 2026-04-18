import { SEOHead } from '@/components/shared/seo-head';

export function AboutPage() {
  return (
    <>
      <SEOHead
        title="Hakkımızda – Pick Me"
        description="İstanbul'da şoför ve vale hizmeti sunan Pick Me platformu hakkında bilgi."
        canonicalPath="/hakkimizda"
      />
      <section className="container py-16 md:py-24 max-w-3xl">
        <h1 className="text-4xl md:text-5xl font-bold tracking-tight">Hakkımızda</h1>
        <div className="prose prose-neutral dark:prose-invert mt-8 space-y-6 text-lg text-muted-foreground">
          <p>
            Pick Me, İstanbul'da kurulmuş bir şoför ve vale hizmeti platformudur. Amacımız; aracınızla
            seyahat ederken size güvenli, profesyonel ve şeffaf bir hizmet sunmaktır.
          </p>
          <p>
            Ekibimizdeki tüm şoförler ehliyetlidir ve kurumsal eğitimden geçmiştir. Kendi aracınızla
            istediğiniz yere güvenle ulaşmanız için gerekli her adımı özenle tasarlıyoruz.
          </p>
          <p>
            <em>Kurumsal içerik müşteri tarafından sonra sağlanacaktır — bu sayfa placeholder'dır.</em>
          </p>
        </div>
      </section>
    </>
  );
}
