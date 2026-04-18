import { Link, useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { toast } from 'sonner';
import { registerSchema, type RegisterInput } from '@pickme/shared/validation';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { FormField } from '@/components/ui/form-field';
import { SEOHead } from '@/components/shared/seo-head';
import { authApi } from './auth-api';

export function RegisterPage() {
  const navigate = useNavigate();
  const form = useForm<RegisterInput>({
    resolver: zodResolver(registerSchema),
    defaultValues: {
      firstName: '',
      lastName: '',
      email: '',
      phone: '',
      password: '',
      passwordConfirm: '',
      kvkkAccepted: false,
    },
  });

  const onSubmit = async (values: RegisterInput) => {
    try {
      await authApi.register(values);
      toast.success('Kaydınız alındı! E-posta adresinize doğrulama bağlantısı gönderildi.');
      navigate('/giris', { state: { justRegistered: true, email: values.email } });
    } catch (err) {
      const e = err as Error & { errors?: Record<string, string[]>; code?: string };
      toast.error(e.message);
      if (e.errors) {
        for (const [field, msgs] of Object.entries(e.errors)) {
          if (field in values) {
            form.setError(field as keyof RegisterInput, { message: msgs[0] });
          }
        }
      }
    }
  };

  return (
    <>
      <SEOHead title="Kayıt Ol – Pick Me" canonicalPath="/kayit" />

      <div className="container py-12 md:py-20 max-w-lg">
        <Card>
          <CardHeader className="space-y-2 text-center">
            <CardTitle>Hesap Oluştur</CardTitle>
            <CardDescription>30 saniyede hesabın hazır. Hemen rezervasyona başla.</CardDescription>
          </CardHeader>
          <CardContent>
            <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
              <div className="grid grid-cols-2 gap-4">
                <FormField label="Ad" required error={form.formState.errors.firstName?.message}>
                  <Input {...form.register('firstName')} autoComplete="given-name" />
                </FormField>
                <FormField label="Soyad" required error={form.formState.errors.lastName?.message}>
                  <Input {...form.register('lastName')} autoComplete="family-name" />
                </FormField>
              </div>

              <FormField label="E-posta" required error={form.formState.errors.email?.message}>
                <Input type="email" {...form.register('email')} autoComplete="email" />
              </FormField>

              <FormField
                label="Telefon"
                required
                error={form.formState.errors.phone?.message}
                description="Türkiye cep telefonu, ör: 05551234567"
              >
                <Input type="tel" {...form.register('phone')} autoComplete="tel" placeholder="05551234567" />
              </FormField>

              <FormField
                label="Şifre"
                required
                error={form.formState.errors.password?.message}
                description="En az 8 karakter, büyük harf, küçük harf ve rakam içermeli"
              >
                <Input type="password" {...form.register('password')} autoComplete="new-password" />
              </FormField>

              <FormField label="Şifre (Tekrar)" required error={form.formState.errors.passwordConfirm?.message}>
                <Input type="password" {...form.register('passwordConfirm')} autoComplete="new-password" />
              </FormField>

              <label className="flex items-start gap-3 text-sm">
                <input
                  type="checkbox"
                  {...form.register('kvkkAccepted')}
                  className="mt-1 size-4 rounded border-input accent-primary"
                  aria-invalid={!!form.formState.errors.kvkkAccepted}
                />
                <span className="text-muted-foreground">
                  <Link to="/kvkk" className="text-primary hover:underline">
                    KVKK Aydınlatma Metnini
                  </Link>{' '}
                  okudum, kişisel verilerimin işlenmesine açık rıza gösteriyorum.
                </span>
              </label>
              {form.formState.errors.kvkkAccepted ? (
                <p role="alert" className="text-xs font-medium text-destructive">
                  {form.formState.errors.kvkkAccepted.message}
                </p>
              ) : null}

              <Button type="submit" className="w-full" size="lg" loading={form.formState.isSubmitting}>
                {form.formState.isSubmitting ? 'Kaydediliyor...' : 'Hesap Oluştur'}
              </Button>
            </form>

            <p className="mt-6 text-center text-sm text-muted-foreground">
              Zaten bir hesabın var mı?{' '}
              <Link to="/giris" className="font-medium text-primary hover:underline">
                Giriş yap
              </Link>
            </p>
          </CardContent>
        </Card>
      </div>
    </>
  );
}
