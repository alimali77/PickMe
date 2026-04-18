import { test, expect } from '@playwright/test';

test.describe('Kurumsal site', () => {
  test('ana sayfa yüklenir ve hero görünür', async ({ page }) => {
    await page.goto('/');
    await expect(page).toHaveTitle(/Pick Me/);
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
    await expect(page.getByRole('link', { name: /Hemen Rezervasyon/i })).toBeVisible();
  });

  test('WhatsApp FAB tüm public sayfalarda görünür', async ({ page }) => {
    for (const path of ['/', '/hizmetler/sofor', '/hizmetler/vale', '/hakkimizda', '/sss', '/iletisim']) {
      await page.goto(path);
      await expect(page.getByRole('link', { name: /WhatsApp/i })).toBeVisible();
    }
  });

  test('SSS sayfasında accordion açılır', async ({ page }) => {
    await page.goto('/sss');
    const firstButton = page.getByRole('button').first();
    await firstButton.click();
    await expect(firstButton).toHaveAttribute('aria-expanded', 'true');
  });

  test('iletişim formu validation hatalarını gösterir', async ({ page }) => {
    await page.goto('/iletisim');
    await page.getByRole('button', { name: /Mesaj Gönder/i }).click();
    // Zod tarafından zorunlu alanlar boş gösterilir
    await expect(page.getByText(/boş bırakılamaz|en az/i).first()).toBeVisible();
  });

  test('404 sayfası mevcut olmayan rota için gösterilir', async ({ page }) => {
    await page.goto('/boyle-bir-sayfa-yok');
    await expect(page.getByRole('heading', { name: /404|Bulunamadı/i })).toBeVisible();
  });
});
