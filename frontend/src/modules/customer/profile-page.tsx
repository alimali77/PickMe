import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { toast } from 'sonner';
import {
  updateProfileSchema,
  changePasswordSchema,
  type UpdateProfileInput,
  type ChangePasswordInput,
} from '@pickme/shared/validation';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { FormField } from '@/components/ui/form-field';
import { authApi } from '@/modules/auth/auth-api';
import { useAuthStore } from '@/lib/auth-store';

export function ProfilePage() {
  const user = useAuthStore((s) => s.user);
  const setUser = useAuthStore((s) => s.setUser);

  const profileForm = useForm<UpdateProfileInput>({
    resolver: zodResolver(updateProfileSchema),
    defaultValues: { firstName: user?.firstName ?? '', lastName: user?.lastName ?? '', phone: user?.phone ?? '' },
  });

  useEffect(() => {
    if (user) profileForm.reset({ firstName: user.firstName, lastName: user.lastName, phone: user.phone });
  }, [user, profileForm]);

  const pwForm = useForm<ChangePasswordInput>({
    resolver: zodResolver(changePasswordSchema),
    defaultValues: { currentPassword: '', newPassword: '', newPasswordConfirm: '' },
  });

  const onProfile = async (values: UpdateProfileInput) => {
    try {
      const updated = await authApi.updateMe(values);
      setUser(updated);
      toast.success('Bilgileriniz güncellendi.');
    } catch (e) {
      toast.error((e as Error).message);
    }
  };

  const onPassword = async (values: ChangePasswordInput) => {
    try {
      await authApi.changePassword(values);
      toast.success('Şifreniz güncellendi. Lütfen yeniden giriş yapın.');
      useAuthStore.getState().clear();
      window.location.href = '/giris';
    } catch (e) {
      toast.error((e as Error).message);
    }
  };

  return (
    <div className="space-y-6 max-w-2xl">
      <h1 className="text-2xl md:text-3xl font-bold">Hesap Bilgilerim</h1>

      <Card>
        <CardHeader>
          <CardTitle>Kişisel Bilgiler</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={profileForm.handleSubmit(onProfile)} className="space-y-4">
            <div className="grid grid-cols-2 gap-4">
              <FormField label="Ad" required error={profileForm.formState.errors.firstName?.message}>
                <Input {...profileForm.register('firstName')} />
              </FormField>
              <FormField label="Soyad" required error={profileForm.formState.errors.lastName?.message}>
                <Input {...profileForm.register('lastName')} />
              </FormField>
            </div>
            <FormField label="E-posta">
              <Input value={user?.email ?? ''} disabled />
            </FormField>
            <FormField label="Telefon" required error={profileForm.formState.errors.phone?.message}>
              <Input type="tel" {...profileForm.register('phone')} />
            </FormField>
            <Button type="submit" loading={profileForm.formState.isSubmitting}>
              {profileForm.formState.isSubmitting ? 'Kaydediliyor...' : 'Bilgileri Güncelle'}
            </Button>
          </form>
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle>Şifre Değiştir</CardTitle>
        </CardHeader>
        <CardContent>
          <form onSubmit={pwForm.handleSubmit(onPassword)} className="space-y-4">
            <FormField label="Mevcut Şifre" required error={pwForm.formState.errors.currentPassword?.message}>
              <Input type="password" autoComplete="current-password" {...pwForm.register('currentPassword')} />
            </FormField>
            <FormField label="Yeni Şifre" required error={pwForm.formState.errors.newPassword?.message}>
              <Input type="password" autoComplete="new-password" {...pwForm.register('newPassword')} />
            </FormField>
            <FormField label="Yeni Şifre (Tekrar)" required error={pwForm.formState.errors.newPasswordConfirm?.message}>
              <Input type="password" autoComplete="new-password" {...pwForm.register('newPasswordConfirm')} />
            </FormField>
            <Button type="submit" loading={pwForm.formState.isSubmitting}>
              {pwForm.formState.isSubmitting ? 'Kaydediliyor...' : 'Şifreyi Değiştir'}
            </Button>
          </form>
        </CardContent>
      </Card>
    </div>
  );
}
