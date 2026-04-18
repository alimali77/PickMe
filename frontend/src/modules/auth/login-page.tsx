import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { toast } from 'sonner';
import { loginSchema, type LoginInput } from '@pickme/shared/validation';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { FormField } from '@/components/ui/form-field';
import { SEOHead } from '@/components/shared/seo-head';
import { authApi } from './auth-api';
import { useAuthStore } from '@/lib/auth-store';

export function LoginPage() {
  const navigate = useNavigate();
  const [params] = useSearchParams();
  const setTokens = useAuthStore((s) => s.setTokens);
  const setUser = useAuthStore((s) => s.setUser);

  const form = useForm<LoginInput>({
    resolver: zodResolver(loginSchema),
    defaultValues: { email: '', password: '' },
  });

  const onSubmit = async (values: LoginInput) => {
    try {
      const tokens = await authApi.login(values);
      setTokens(tokens);
      const me = await authApi.me();
      setUser(me);
      toast.success('Hoş geldiniz!');
      const next = params.get('next');
      if (next) navigate(next);
      else if (me.role === 'Admin') navigate('/admin');
      else if (me.role === 'Driver') navigate('/driver');
      else navigate('/hesabim');
    } catch (err) {
      const message = (err as Error).message;
      toast.error(message);
      form.setError('password', { message: ' ' });
    }
  };

  return (
    <>
      <SEOHead title="Giriş Yap – Pick Me" canonicalPath="/giris" />

      <div className="container py-12 md:py-20 max-w-md">
        <Card>
          <CardHeader className="space-y-2 text-center">
            <CardTitle>Giriş Yap</CardTitle>
            <CardDescription>Hesabınıza giriş yapın ve rezervasyonunuzu yönetin.</CardDescription>
          </CardHeader>
          <CardContent>
            <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
              <FormField label="E-posta" required error={form.formState.errors.email?.message}>
                <Input type="email" autoComplete="email" {...form.register('email')} />
              </FormField>
              <FormField label="Şifre" required error={form.formState.errors.password?.message}>
                <Input type="password" autoComplete="current-password" {...form.register('password')} />
              </FormField>
              <Button type="submit" className="w-full" size="lg" loading={form.formState.isSubmitting}>
                {form.formState.isSubmitting ? 'Giriş yapılıyor...' : 'Giriş Yap'}
              </Button>
            </form>

            <div className="mt-6 flex flex-col items-center gap-3 text-sm">
              <Link to="/sifremi-unuttum" className="text-primary hover:underline">
                Şifremi unuttum
              </Link>
              <div className="text-muted-foreground">
                Hesabın yok mu?{' '}
                <Link to="/kayit" className="font-medium text-primary hover:underline">
                  Kayıt ol
                </Link>
              </div>
            </div>
          </CardContent>
        </Card>
      </div>
    </>
  );
}
