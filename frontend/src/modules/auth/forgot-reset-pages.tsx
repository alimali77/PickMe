import { Link, useSearchParams, useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { toast } from 'sonner';
import {
  forgotPasswordSchema,
  resetPasswordSchema,
  type ForgotPasswordInput,
  type ResetPasswordInput,
} from '@pickme/shared/validation';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { FormField } from '@/components/ui/form-field';
import { SEOHead } from '@/components/shared/seo-head';
import { authApi } from './auth-api';
import { useState } from 'react';

export function ForgotPasswordPage() {
  const [sent, setSent] = useState(false);
  const form = useForm<ForgotPasswordInput>({
    resolver: zodResolver(forgotPasswordSchema),
    defaultValues: { email: '' },
  });

  const onSubmit = async (values: ForgotPasswordInput) => {
    try {
      await authApi.forgotPassword(values);
      setSent(true);
    } catch (err) {
      toast.error((err as Error).message);
    }
  };

  return (
    <>
      <SEOHead title="Şifremi Unuttum – Pick Me" canonicalPath="/sifremi-unuttum" />
      <div className="container py-12 md:py-20 max-w-md">
        <Card>
          <CardHeader className="text-center">
            <CardTitle>Şifremi Unuttum</CardTitle>
            <CardDescription>
              E-posta adresinizi girin, size şifre sıfırlama bağlantısı gönderelim.
            </CardDescription>
          </CardHeader>
          <CardContent>
            {sent ? (
              <div className="space-y-4 text-center">
                <p className="text-sm text-muted-foreground">
                  E-posta adresiniz sistemde kayıtlıysa, size bir şifre sıfırlama bağlantısı gönderildi.
                  Lütfen gelen kutunuzu kontrol ediniz.
                </p>
                <Button asChild variant="outline">
                  <Link to="/giris">Giriş Sayfasına Dön</Link>
                </Button>
              </div>
            ) : (
              <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
                <FormField label="E-posta" required error={form.formState.errors.email?.message}>
                  <Input type="email" autoComplete="email" {...form.register('email')} />
                </FormField>
                <Button type="submit" className="w-full" loading={form.formState.isSubmitting}>
                  {form.formState.isSubmitting ? 'Gönderiliyor...' : 'Bağlantı Gönder'}
                </Button>
              </form>
            )}
            <p className="mt-6 text-center text-sm text-muted-foreground">
              <Link to="/giris" className="hover:underline">
                ← Giriş sayfasına dön
              </Link>
            </p>
          </CardContent>
        </Card>
      </div>
    </>
  );
}

export function ResetPasswordPage() {
  const [params] = useSearchParams();
  const navigate = useNavigate();
  const token = params.get('token') ?? '';
  const form = useForm<ResetPasswordInput>({
    resolver: zodResolver(resetPasswordSchema),
    defaultValues: { token, password: '', passwordConfirm: '' },
  });

  const onSubmit = async (values: ResetPasswordInput) => {
    try {
      await authApi.resetPassword(values);
      toast.success('Şifreniz güncellendi! Yeni şifrenizle giriş yapabilirsiniz.');
      navigate('/giris');
    } catch (err) {
      toast.error((err as Error).message);
    }
  };

  return (
    <>
      <SEOHead title="Şifre Sıfırla – Pick Me" canonicalPath="/sifre-sifirla" />
      <div className="container py-12 md:py-20 max-w-md">
        <Card>
          <CardHeader className="text-center">
            <CardTitle>Yeni Şifre Belirle</CardTitle>
            <CardDescription>Şifreniz en az 8 karakter, büyük harf, küçük harf ve rakam içermelidir.</CardDescription>
          </CardHeader>
          <CardContent>
            <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
              <input type="hidden" {...form.register('token')} />
              <FormField label="Yeni Şifre" required error={form.formState.errors.password?.message}>
                <Input type="password" autoComplete="new-password" {...form.register('password')} />
              </FormField>
              <FormField label="Şifre (Tekrar)" required error={form.formState.errors.passwordConfirm?.message}>
                <Input type="password" autoComplete="new-password" {...form.register('passwordConfirm')} />
              </FormField>
              <Button type="submit" className="w-full" loading={form.formState.isSubmitting}>
                {form.formState.isSubmitting ? 'Kaydediliyor...' : 'Şifreyi Güncelle'}
              </Button>
            </form>
          </CardContent>
        </Card>
      </div>
    </>
  );
}
