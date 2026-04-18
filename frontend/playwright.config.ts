import { defineConfig, devices } from '@playwright/test';

/**
 * Playwright konfigürasyonu.
 *
 * Çalıştırma öncesi:
 *   1) LocalDB ayakta olmalı (SqlLocalDb start MSSQLLocalDB)
 *   2) Backend: dotnet run --project backend/src/PickMe.Api --urls http://localhost:5080
 *      (seed admin otomatik oluşur: admin@pickme.local / Admin123!Change)
 *   3) `pnpm dev` (otomatik başlıyor — webServer altında tanımlı)
 *
 * Chromium kurulumu (ilk kez):
 *   pnpm -C frontend exec playwright install --with-deps chromium
 *
 * Çalıştırma:
 *   pnpm -C frontend test:e2e
 *
 * Brief §16-E: 10+ senaryo hedefi. Bu dosyada şu an 3 core senaryo var,
 * ekip tarafından genişletilmek üzere tests/e2e/TODO.md'de kalan liste var.
 */

const BASE_URL = process.env.E2E_BASE_URL ?? 'http://localhost:5173';

export default defineConfig({
  testDir: './tests/e2e',
  fullyParallel: false, // DB state paylaşıyor; sıralı koş
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 1 : 0,
  workers: 1,
  reporter: process.env.CI ? [['github'], ['html', { open: 'never' }]] : 'list',
  use: {
    baseURL: BASE_URL,
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
    locale: 'tr-TR',
    timezoneId: 'Europe/Istanbul',
    actionTimeout: 10_000,
    navigationTimeout: 20_000,
  },
  projects: [
    {
      name: 'chromium',
      use: { ...devices['Desktop Chrome'] },
    },
  ],
  webServer: [
    // Frontend dev server'ı otomatik başlat
    {
      command: 'pnpm dev',
      url: BASE_URL,
      reuseExistingServer: true,
      timeout: 60_000,
    },
    // Backend başlatma: CI'da docker-compose ile yapılır; lokal'de manuel başlatılmalı.
    // Not: CI konfigürasyonu .github/workflows/ci.yml içinde mssql + smtp4dev servis
    // container'ları ile tanımlı.
  ],
});
