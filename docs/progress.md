# Pick Me — Uygulama İlerleme Durumu

İlk kurulum oturumu — bu dosya her fazda güncellenir ve plan dosyasıyla senkron tutulur.

## Tamamlanan Fazlar

### ✅ Faz 0 — Temel (iskelet)

- Monorepo yapısı (`/backend`, `/frontend`, `/shared`, `/infra`, `/docs`, `/.github`).
- pnpm workspaces + root scriptleri (`dev`, `build`, `test`, `generate:types`, `backend:*`).
- .NET 10 solution (`backend/PickMe.slnx`): `Domain` / `Contracts` / `Application` / `Infrastructure` / `Api` + 3 test projesi.
- Vite + React 18 + TypeScript + Tailwind CSS + shadcn/ui primitif iskelet.
- Shared paket: `validation.ts` (Zod), `validation-rules.json` (tek kaynak), `constants.ts` (enum literal'leri), `api-types.ts` (OpenAPI generate öncesi elle iskelet).
- `.env.example`, `.gitignore`, `.editorconfig`, `.prettierrc`, `Directory.Build.props`.
- Docker Compose: MSSQL 2022 + smtp4dev servisleri lokal dev için.
- GitHub Actions CI workflow: backend (xunit + arch + integration) + frontend (lint + typecheck + vitest + build) + OpenAPI drift + E2E (Playwright).

### ✅ Faz 1 — Veritabanı

- 14 entity (`User`, `Customer`, `Driver`, `Admin`, `Reservation`, `Rating`, `EmailVerificationToken`, `PasswordResetToken`, `RefreshToken`, `AdminNotificationRecipient`, `SystemSetting`, `Faq`, `ContactMessage`, `EmailLog`).
- 5 enum: `UserRole`, `ReservationStatus`, `ServiceType`, `DriverStatus`, `CancelledBy`, `EmailLogStatus`.
- `Reservation` aggregate — durum makinesi domain'de enforce: `Pending → Assigned → OnTheWay → Completed`, `InvalidStateTransitionException` ile geçersiz geçişler reddediliyor.
- EF Core `ApplicationDbContext` + 14 ayrı `IEntityTypeConfiguration<T>`; tüm DateTime'lar `datetime2(3)` UTC; e-posta unique index; `Reservation.Status + ReservationDateTimeUtc` composite index; `Rating.ReservationId` unique; `Driver` soft-delete query filter.
- İlk migration `InitialSchema` üretildi ve derleme doğrulandı (14 tablo).
- Design-time `IDesignTimeDbContextFactory` EF CLI için.
- Unit testler: state machine 18 senaryo (happy + invalid), user lockout 4, rating 8. **33 test yeşil, state machine %100 coverage.**
- Architecture testler: Domain hiçbir katmanla bağımlılık kurmaz; Application sadece Domain + Contracts'a bağlıdır; Infrastructure Api'ye bağlı değildir. **3 test yeşil.**

### ✅ Faz 2 — Kimlik Doğrulama

**Infrastructure:**
- `BcryptPasswordHasher` (enhanced hash, cost env'den).
- `Sha256TokenHasher` — e-posta/şifre/refresh tokenları DB'de hash'li saklanır, plain sadece mailde.
- `OpaqueTokenGenerator` — URL-safe base64 random token üretici.
- `JwtTokenService` — HS256, 60 dk access + rotation refresh, secret min 256-bit zorunlu.
- `FrontendUrlProvider` — verify/reset linkleri için frontend URL'si.
- `MailKitEmailSender` + `BackgroundEmailQueue` (in-memory, Faz 4'te Hangfire'a geçecek).
- `IApplicationDbContext` interface'i Application'da; `ApplicationDbContext` Infrastructure'da implement + DI kaydı.

**Application:**
- `IAuthService` + `AuthService`: Register, VerifyEmail, ResendVerification, Login, Refresh, Logout, ForgotPassword, ResetPassword, ChangePassword, GetCurrentUser, UpdateProfile.
- FluentValidation: 6 validator (`RegisterCustomerValidator`, `LoginValidator`, `ForgotPasswordValidator`, `ResetPasswordValidator`, `ChangePasswordValidator`, `UpdateProfileValidator`) — kural limitleri `shared/validation-rules.json`'dan birebir kopya (aynı mesajlar, aynı regex'ler).
- User enumeration engeli: forgot-password ve resend-verification her durumda aynı generic cevap.
- 5 hatalı girişte 15 dk lockout (`User.RecordFailedLogin`).
- Refresh rotation: `RefreshAsync` eski token'ı revoke eder, yenisini üretir; eski token ikinci kez kullanılamaz (integration testle doğrulandı).
- Şifre değişiminde tüm aktif refresh tokenlar revoke.

**Api:**
- `AuthController` tüm endpoint'ler: `POST /api/auth/{register,verify-email,resend-verification,login,refresh,logout,forgot-password,reset-password}` + `GET/PATCH /api/auth/me` + `PATCH /api/auth/me/password`.
- JWT Bearer authentication (`Microsoft.AspNetCore.Authentication.JwtBearer`).
- Global `ExceptionHandlingMiddleware` — DomainException → 409 envelope.
- `ResultExtensions` — uygulama `Result<T>` → HTTP status + envelope (validation→400, invalid_creds→401, email_not_verified→403, locked→423, invalid_transition→409).
- Security header'ları: X-Content-Type-Options, X-Frame-Options, Referrer-Policy, Permissions-Policy; HSTS prod'da.
- `HttpContextCurrentUser` — JWT claim'lerinden `ICurrentUser` resolve.
- CORS env var'dan.

**Testler:**
- Unit: `BcryptPasswordHasher` round-trip / salt uniqueness / invalid hash fallback (3), `Sha256TokenHasher` / `OpaqueTokenGenerator` (2), `JwtTokenService` sign/validate/short-secret/uniqueness (3), `RegisterCustomerValidator` 14 senaryo (tüm kurallar). **22 yeni test.**
- Integration (EF InMemory): register happy path, duplicate email → 409, login-before-verify blocked, **full register → verify → login → refresh → logout flow**, refresh rotation (eski token artık çalışmıyor), 5 failed login → lockout, forgot-password unknown email silent ok, forgot+reset + old-password-fails, invalid verify token, change password revokes all refresh tokens, wrong current password, GetCurrentUser customer profile, validation error returns shared message. **12 test.**

**Toplam: 61 unit + 12 integration + 3 arch = 76 backend testi yeşil.**

### ✅ Faz 3 (kısmi) — Frontend Temel

- shadcn/ui primitifleri: `Button` (6 variant, loading state), `Input`, `Label`, `Card`, `FormField` (label+error+desc+ARIA bağları).
- Design token'lar Tailwind config'te (HSL CSS variable'lar) + light/dark mode.
- Paylaşılan komponentler: `SEOHead` (title/meta/OG/canonical/JSON-LD), `WhatsAppFab`, `CookieBanner` (KVKK consent + GA4 consent mode), `PublicLayout` (sticky header + responsive nav + footer + mobile drawer).
- Public sayfalar: Home (hero + 4 adım + 2 hizmet kartı + CTA), Services (`/hizmetler/sofor`, `/hizmetler/vale`), About, FAQ (accordion + JSON-LD FAQPage), Contact (form + inline validation + toast), Legal (3 sayfa placeholder), 404.
- Auth sayfaları: Login (next-param redirect + role-based landing), Register (inline validation + KVKK onay + server error field mapping), VerifyEmail, ForgotPassword, ResetPassword.
- `apiClient` (axios + auth store entegrasyonu + 401 refresh interceptor), `AuthStore` (Zustand + persist), `queryClient` (TanStack Query).
- React Router route tree + placeholders for `/rezervasyon` ve `/hesabim` (Faz 4'te tamamlanacak).
- Frontend unit tests: shared Zod validation (11) + cn utility (1). **12 test yeşil.**

**Toplam: 76 backend + 12 frontend = 88 test yeşil.**

## Kalan İş (Brief Bölüm 20 + Plan Faz 4-9)

Plan dosyasındaki yol haritası:

- **Faz 3 (devam)**: vike pre-render entegrasyonu (SSG), Inter font self-host, Lighthouse ≥ 80 validasyon, mobil breakpoint testleri, `vite-plugin-ssg` veya manuel pre-render.
- **Faz 4**: Rezervasyon modülü — `Reservation` aggregate command'ları (Create/Assign/Start/Complete/Cancel×2/Reassign), authorization handler (resource-based), Google Places Autocomplete form, customer `/rezervasyon`, `/hesabim`, admin rezervasyon tablosu + atama modal'ı, driver görev listesi + mobil odaklı detay, Hangfire mail job'ları, CSV export.
- **Faz 5**: Rating servisi (concurrency-safe SQL `AVG`), tüm admin modülleri (drivers, customers, ratings, FAQs, email settings, system settings, admins CRUD).
- **Faz 6**: NSwag OpenAPI → TS generate pipeline, contract testler, validation drift CI job.
- **Faz 7**: Playwright 10+ senaryo + CI sertleştirme (branch protection).
- **Faz 8**: IIS deployment (web.config + WebDeploy + Let's Encrypt + backup scheduled task).
- **Faz 9**: Runbook + admin/driver kılavuzları + UAT + canlıya geçiş.

## Nasıl çalıştırılır

```bash
# Terminal 1: DB + SMTP
docker compose -f infra/dev/docker-compose.yml up -d

# Terminal 2: Migration + Backend (port 5001)
dotnet ef database update --project backend/src/PickMe.Infrastructure --startup-project backend/src/PickMe.Api
dotnet run --project backend/src/PickMe.Api

# Terminal 3: Frontend (port 5173)
pnpm dev
```

Tüm testler:
```bash
dotnet test backend/PickMe.slnx      # 76 test
pnpm -C frontend test -- --run       # 12 test
pnpm -C frontend build               # üretim build'i
```
