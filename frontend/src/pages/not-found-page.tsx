import { Link } from 'react-router-dom';
import { Button } from '@/components/ui/button';
import { SEOHead } from '@/components/shared/seo-head';

export function NotFoundPage() {
  return (
    <>
      <SEOHead title="Sayfa Bulunamadı – Pick Me" description="Aradığınız sayfa mevcut değil." />
      <div className="container py-24 md:py-32 text-center">
        <div className="text-7xl md:text-8xl font-bold tracking-tight text-muted-foreground/40">404</div>
        <h1 className="mt-6 text-3xl md:text-4xl font-bold">Sayfa Bulunamadı</h1>
        <p className="mx-auto mt-4 max-w-md text-muted-foreground">
          Aradığınız sayfa taşınmış veya silinmiş olabilir. Ana sayfadan devam edebilirsiniz.
        </p>
        <div className="mt-8 flex flex-col sm:flex-row gap-3 justify-center">
          <Button asChild size="lg">
            <Link to="/">Ana Sayfaya Dön</Link>
          </Button>
          <Button asChild size="lg" variant="outline">
            <Link to="/iletisim">İletişim</Link>
          </Button>
        </div>
      </div>
    </>
  );
}
