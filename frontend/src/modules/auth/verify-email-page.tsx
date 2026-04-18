import { useEffect, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { CheckCircle2, XCircle, Loader2 } from 'lucide-react';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { SEOHead } from '@/components/shared/seo-head';
import { authApi } from './auth-api';

type Status = 'loading' | 'success' | 'error';

export function VerifyEmailPage() {
  const [params] = useSearchParams();
  const token = params.get('token');
  const [status, setStatus] = useState<Status>('loading');
  const [message, setMessage] = useState<string>('');

  useEffect(() => {
    if (!token) {
      setStatus('error');
      setMessage('Doğrulama bağlantısı geçersiz.');
      return;
    }
    let active = true;
    (async () => {
      try {
        await authApi.verifyEmail(token);
        if (active) setStatus('success');
      } catch (err) {
        if (active) {
          setStatus('error');
          setMessage((err as Error).message);
        }
      }
    })();
    return () => {
      active = false;
    };
  }, [token]);

  return (
    <>
      <SEOHead title="E-posta Doğrulama – Pick Me" canonicalPath="/eposta-dogrula" />

      <div className="container py-12 md:py-20 max-w-md">
        <Card>
          <CardHeader className="text-center">
            {status === 'loading' && <Loader2 className="mx-auto size-10 text-primary animate-spin" />}
            {status === 'success' && <CheckCircle2 className="mx-auto size-10 text-emerald-500" />}
            {status === 'error' && <XCircle className="mx-auto size-10 text-destructive" />}
            <CardTitle className="mt-4">
              {status === 'loading' && 'Doğrulanıyor...'}
              {status === 'success' && 'Hesabınız Doğrulandı!'}
              {status === 'error' && 'Doğrulama Başarısız'}
            </CardTitle>
            <CardDescription>
              {status === 'loading' && 'Lütfen birkaç saniye bekleyin.'}
              {status === 'success' && 'Artık giriş yapıp rezervasyon oluşturabilirsiniz.'}
              {status === 'error' && (message || 'Bağlantı geçersiz veya süresi dolmuş olabilir.')}
            </CardDescription>
          </CardHeader>
          <CardContent className="flex justify-center">
            {status === 'success' && (
              <Button asChild size="lg">
                <Link to="/giris">Giriş Yap</Link>
              </Button>
            )}
            {status === 'error' && (
              <Button asChild variant="outline">
                <Link to="/giris">Giriş Sayfasına Dön</Link>
              </Button>
            )}
          </CardContent>
        </Card>
      </div>
    </>
  );
}
