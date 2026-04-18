# Pick Me — Şoför & Vale Platformu

İstanbul merkezli şoför + vale hizmeti rezervasyon platformu.
**Kurumsal site** + **müşteri portalı** + **admin paneli** + **şoför paneli**.

## Özet durum

- **127 otomatik test yeşil** (61 unit + 3 architecture + 51 integration + 12 frontend)
- **36+ REST endpoint** (auth + rezervasyon + rating + 9 admin yönetim modülü + public)
- **Uçtan uca çalışan** akış: kayıt → doğrulama → rezervasyon → şoför atama → yolculuk → puan
- **Live demo**: LocalDB üzerinde smoke test geçti, Swagger UI + Hangfire dashboard aktif

## Mimari

| Katman | Teknoloji |
|---|---|
| Frontend | React 18 + Vite + TypeScript + Tailwind CSS + shadcn/ui + TanStack Query + Zustand |
| Backend | ASP.NET Core 10 + Clean Architecture (Domain/Application/Infrastructure/Api) |
| DB | MSSQL (LocalDB dev, SQL Server 2022 prod) + EF Core |
| Auth | JWT (HS256) access 60 dk + refresh 7 gün rotation + BCrypt |
| Mail | MailKit + Hangfire durable queue (3 retry: 5dk/30dk/2h) |
| Harita | Google Places Autocomplete (yalnızca rezervasyon formu) |
| Hosting | Windows Server + IIS + MSSQL |

## Önkoşullar

- Node.js 22+ ve pnpm 10+
- .NET SDK 10
- SQL Server LocalDB (Windows dev) **veya** Docker + MSSQL 2022
- (Opsiyonel) Docker — smtp4dev ve full E2E için

## Lokal çalıştırma (LocalDB — önerilen dev kurulum)

```bash
# 1) Bağımlılıklar
pnpm install
dotnet restore backend/PickMe.slnx

# 2) LocalDB'yi başlat (Windows)
sqllocaldb start MSSQLLocalDB

# 3) Backend çalıştır — migration + admin seed otomatik
pnpm run backend:run
#    admin@pickme.local / Admin123!Change hazır seed edilir

# 4) Frontend (ayrı terminal)
pnpm dev
```

**Açılan URL'ler:**
- Frontend: http://localhost:5173
- Backend API: http://localhost:5080
- **Swagger UI**: http://localhost:5080/swagger
- **Hangfire Dashboard**: http://localhost:5080/hangfire (admin giriş sonrası)
- Health: http://localhost:5080/health

## Lokal çalıştırma (Docker Compose — mail + full MSSQL)

```bash
docker compose -f infra/dev/docker-compose.yml up -d
# MSSQL: localhost:1433 (sa / ChangeMe_Str0ng!)
# smtp4dev: http://localhost:5050 — mail test için web UI
```

## Rolleri denemek

1. **Müşteri olarak**: `/kayit` → e-posta doğrulama (smtp4dev yoksa DB'den token: `SELECT TokenHash FROM EmailVerificationTokens` ve DB'de direkt `EmailConfirmed=1` yapabilirsiniz dev için) → `/giris` → `/rezervasyon`
2. **Admin olarak**: `/giris` → `admin@pickme.local` / `Admin123!Change` → `/admin` → şoför ekle → rezervasyon ata
3. **Şoför olarak**: Admin tarafından oluşturulan hesapla giriş → `MustChangePassword` nedeniyle `/driver/sifre-degistir` → sonra `/driver` görev listesi

## Testler

```bash
pnpm run backend:test      # .NET: 61 unit + 3 arch + 51 integration
pnpm test                  # Frontend: Vitest 12 test
pnpm test:e2e              # Playwright E2E (Chromium kurulumu: pnpm -C frontend exec playwright install chromium)
```

## Production deploy (IIS)

Ayrıntılı talimatlar: [`docs/runbook.md`](docs/runbook.md)

```powershell
# Build + deploy tek komutla (admin PowerShell)
.\infra\scripts\deploy.ps1 -ApiPath "C:\inetpub\pickme\api" -WebPath "C:\inetpub\pickme\web"

# Scheduled backup task'larını kur (tek seferlik, admin PS)
.\infra\scripts\install-scheduled-tasks.ps1

# Post-deploy smoke test
.\infra\scripts\smoke-test.ps1 -BaseUrl "https://pickme.example"
```

## Dokümantasyon

- [`docs/runbook.md`](docs/runbook.md) — deploy, backup, restore, rollback, incident
- [`docs/admin-guide.md`](docs/admin-guide.md) — admin panel kullanım
- [`docs/driver-guide.md`](docs/driver-guide.md) — şoför uygulaması kullanım
- [`docs/progress.md`](docs/progress.md) — Faz bazlı tamamlanan işler
- [`docs/architecture.md`](docs/architecture.md) — katman ve veri modeli detayı

## Repo yapısı

```
/
├── backend/        .NET 10 Clean Architecture (5 proje + 3 test)
├── frontend/       React + Vite + Tailwind + shadcn/ui
├── shared/         Zod validation, validation-rules.json, api-types.ts, nswag.json
├── infra/
│   ├── dev/        docker-compose.yml (dev MSSQL + smtp4dev)
│   ├── iis/        web.api.config, web.web.config
│   └── scripts/    deploy, backup, restore, smoke-test, install-scheduled-tasks (PS1)
├── docs/           Runbook + kılavuzlar + ilerleme + mimari
└── .github/        CI workflow
```

## Durum — Faz bazlı

| Faz | İçerik | Durum |
|---|---|---|
| 0 | Monorepo iskeleti | ✅ |
| 1 | DB şeması + 14 entity + migrations | ✅ |
| 2 | Auth (register/login/refresh/forgot/change) | ✅ |
| 3 | Public site + kurumsal sayfalar + auth sayfaları | ✅ |
| 4 | Rezervasyon + rating + customer/driver/admin paneller | ✅ |
| 5 | Admin yönetim modülleri (9 CRUD sayfası) | ✅ |
| 6 | Swagger UI + NSwag pipeline | ✅ |
| 7 | Playwright E2E (9/10+ senaryo) | 🟡 iskelet |
| 8 | IIS + deploy + backup scripts | ✅ |
| 9 | Runbook + kılavuzlar | ✅ |

İlerleme detayı: [`docs/progress.md`](docs/progress.md).
