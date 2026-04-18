import { useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { toast } from 'sonner';
import { changePasswordSchema, type ChangePasswordInput } from '@pickme/shared/validation';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { FormField } from '@/components/ui/form-field';
import { authApi } from '@/modules/auth/auth-api';
import { useAuthStore } from '@/lib/auth-store';

export function DriverChangePasswordPage() {
  const navigate = useNavigate();
  const setUser = useAuthStore((s) => s.setUser);
  const form = useForm<ChangePasswordInput>({
    resolver: zodResolver(changePasswordSchema),
    defaultValues: { currentPassword: '', newPassword: '', newPasswordConfirm: '' },
  });

  const onSubmit = async (values: ChangePasswordInput) => {
    try {
      await authApi.changePassword(values);
      toast.success('Şifreniz güncellendi.');
      const me = await authApi.me();
      setUser(me);
      navigate('/driver');
    } catch (e) {
      toast.error((e as Error).message);
    }
  };

  return (
    <div className="max-w-md mx-auto py-8">
      <Card>
        <CardHeader>
          <CardTitle>Şifrenizi Değiştirin</CardTitle>
          <CardDescription>
            Devam etmeden önce başlangıç şifrenizi yeni bir şifre ile değiştirmeniz gerekir.
          </CardDescription>
        </CardHeader>
        <CardContent>
          <form onSubmit={form.handleSubmit(onSubmit)} className="space-y-4">
            <FormField label="Mevcut (Başlangıç) Şifre" required error={form.formState.errors.currentPassword?.message}>
              <Input type="password" {...form.register('currentPassword')} />
            </FormField>
            <FormField label="Yeni Şifre" required error={form.formState.errors.newPassword?.message} description="En az 8 karakter, büyük+küçük harf ve rakam">
              <Input type="password" {...form.register('newPassword')} />
            </FormField>
            <FormField label="Yeni Şifre (Tekrar)" required error={form.formState.errors.newPasswordConfirm?.message}>
              <Input type="password" {...form.register('newPasswordConfirm')} />
            </FormField>
            <Button type="submit" className="w-full" size="lg" loading={form.formState.isSubmitting}>
              Şifreyi Güncelle ve Devam Et
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
