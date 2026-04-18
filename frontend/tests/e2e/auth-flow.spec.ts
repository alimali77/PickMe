import { test, expect } from '@playwright/test';

test.describe('Auth akışı', () => {
  test('kayıt formu validation kurallarını frontend Zod ile enforce eder', async ({ page }) => {
    await page.goto('/kayit');

    // Boş submit → zorunlu alan hataları
    await page.getByRole('button', { name: /Hesap Oluştur/i }).click();
    await expect(page.getByText(/Ad boş bırakılamaz/i).first()).toBeVisible();

    // Geçersiz email → frontend Zod reddeder (backend'e gitmeden)
    await page.getByLabel('E-posta').fill('gecersiz-email');
    await page.getByRole('button', { name: /Hesap Oluştur/i }).click();
    await expect(page.getByText(/Geçerli bir e-posta/i).first()).toBeVisible();
  });

  test('giriş sayfası render olur ve link\'ler doğru', async ({ page }) => {
    await page.goto('/giris');
    await expect(page.getByRole('heading', { name: /Giriş Yap/i })).toBeVisible();
    await expect(page.getByRole('link', { name: /Şifremi unuttum/i })).toBeVisible();
    await expect(page.getByRole('link', { name: /Kayıt ol/i })).toBeVisible();
  });

  test('admin hesabı ile giriş yapılır ve /admin dashboard gelir', async ({ page }) => {
    // Seed admin: admin@pickme.local / Admin123!Change (DatabaseInitializer startup'ta oluşturur)
    await page.goto('/giris');
    await page.getByLabel('E-posta').fill('admin@pickme.local');
    await page.getByLabel('Şifre').fill('Admin123!Change');
    await page.getByRole('button', { name: /^Giriş Yap$/i }).click();

    await expect(page).toHaveURL(/\/admin/);
    await expect(page.getByRole('heading', { name: /Gösterge Paneli/i })).toBeVisible();
  });

  test('yetkisiz kullanıcı /admin\'e erişmeye çalışınca girişe yönlendirilir', async ({ page, context }) => {
    // State temizliği — localStorage boşalt
    await context.clearCookies();
    await page.goto('/');
    await page.evaluate(() => window.localStorage.clear());

    await page.goto('/admin');
    await expect(page).toHaveURL(/\/giris/);
  });
});
