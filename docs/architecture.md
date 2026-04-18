# Pick Me — Mimari

## Katmanlar (Clean Architecture)

```
┌──────────────────────────────────────────┐
│          Api (Controllers)               │  ← HTTP
│  + Middleware, Auth, Swagger, Hangfire   │
└────────────┬─────────────────────────────┘
             │
┌────────────▼─────────────────────────────┐
│      Application (Services + DTOs)       │  ← use cases
│  + FluentValidation + IApplicationDbContext│
└────────────┬─────────────────────────────┘
             │
┌────────────▼─────────────────────────────┐
│            Domain (Entities)             │  ← business rules
│  + Reservation state machine             │
│  + Aggregates (User, Customer, ...)      │
└──────────────────────────────────────────┘

         ▲ (interface dependencies)
         │
┌────────┴─────────────────────────────────┐
│       Infrastructure (adapters)          │
│  EF Core DbContext, BCrypt, JWT, MailKit │
│  Hangfire job runner, FrontendUrlProvider│
└──────────────────────────────────────────┘
```

**Bağımlılık kuralı (architecture testleri enforce eder):**
- Domain hiçbir katmana bağlı değildir.
- Application yalnızca Domain'e bağlıdır.
- Infrastructure Domain + Application'a bağlıdır, Api'ye değil.
- Api hepsine bağlıdır.

## Veri modeli

14 tablo. Tümü `datetime2(3)` UTC, e-posta unique, `Reservation.RowVersion` concurrency için, `Driver` soft-delete query filter.

| Tablo | Önemli alanlar | İlişkiler |
|---|---|---|
| `Users` | Email, PasswordHash, Role (1/2/3), EmailConfirmed, FailedLoginAttempts, LockedUntil | — |
| `Customers` | UserId (FK unique), FirstName, LastName, Phone, KvkkAccepted | Users 1-1 |
| `Drivers` | UserId (FK unique), FirstName, Status, AverageRating(3,2), TotalTrips, MustChangePassword, IsDeleted | Users 1-1 |
| `Admins` | UserId (FK unique), FullName | Users 1-1 |
| `Reservations` | CustomerId, DriverId?, ServiceType, ReservationDateTimeUtc, Address, Lat, Lng, Note, Status, RowVersion | Customer N-1, Driver N-1 |
| `Ratings` | ReservationId (unique), Score (1-5), Comment, IsFlagged, FlaggedReason | Reservation 1-1 |
| `EmailVerificationTokens` | UserId, TokenHash (SHA-256), ExpiresAt, UsedAt | User N-1 |
| `PasswordResetTokens` | Aynı pattern | User N-1 |
| `RefreshTokens` | UserId, TokenHash, ExpiresAt, RevokedAt, ReplacedByTokenId | User N-1 |
| `AdminNotificationRecipients` | Email (unique), IsActive | — |
| `SystemSettings` | Key (unique), Value, IsSensitive | — |
| `Faqs` | Question, Answer, DisplayOrder, IsActive | — |
| `ContactMessages` | FirstName, Email, Phone, Subject, Message, IsRead, ReadAtUtc | — |
| `EmailLogs` | ToEmail, TemplateKey, Status, AttemptCount, LastError | — |

İndex listesi:
- `Users.Email` UNIQUE
- `Customers.UserId` UNIQUE
- `Drivers.UserId` UNIQUE
- `Admins.UserId` UNIQUE
- `Reservations (Status, ReservationDateTimeUtc)` composite
- `Reservations.CustomerId` + `Reservations.DriverId`
- `Ratings.ReservationId` UNIQUE (aynı rezervasyona 2 puan engeli)
- `EmailVerificationTokens.TokenHash` UNIQUE
- `PasswordResetTokens.TokenHash` UNIQUE
- `RefreshTokens.TokenHash` UNIQUE
- `AdminNotificationRecipients.Email` UNIQUE
- `SystemSettings.Key` UNIQUE
- `Faqs (IsActive, DisplayOrder)` composite
- `ContactMessages.IsRead`
- `EmailLogs.Status`

## Reservation State Machine (domain'de enforce)

```
       ┌──────────┐   AssignDriver    ┌──────────┐   StartTrip    ┌──────────┐  CompleteTrip   ┌───────────┐
       │ Pending  │ ────────────────▶ │ Assigned │ ─────────────▶ │ OnTheWay │ ──────────────▶ │ Completed │
       └─────┬────┘  (reassignable)   └────┬─────┘                └────┬─────┘                 └───────────┘
             │                             │                           │
 CancelBy    ▼   AdminCancel               │   AdminCancel             │  AdminCancel
 Customer → Cancelled ◀─────────────────── ┘ ───────────────────────── ┘
 (yalnızca
  Pending'de)
```

- Geçersiz geçiş → `InvalidStateTransitionException` → HTTP 409
- Admin iptal sebebi zorunlu (`DomainException reservation.cancel_reason_required`)
- Müşteri yalnızca `Pending`'de iptal edebilir
- `Completed` iptal edilemez

Unit test kapsamı **%100** (18 senaryo: 8 valid + 10 invalid geçiş).

## Auth akışı

```
Register → EmailVerificationToken (24h) → /verify-email → EmailConfirmed=true
  ↓
Login (5 yanlış → 15dk lockout, email doğrulanmamış → 403)
  ↓
JWT access (60dk, HS256) + Refresh token (7 gün, SHA-256 hash, rotation)
  ↓
Refresh → eski token revoke + yenisi üret (tek kullanımlık)
  ↓
Logout → token revoke
```

- **Forgot password**: enumeration engeli — e-posta var/yok her durumda aynı generic 200 cevabı.
- **Şifre değişiminde** tüm aktif refresh token'lar revoke (diğer cihazlardan çıkış).

## Validation — tek kaynak

`shared/validation-rules.json` her kural için limit/regex/mesaj tutar.
- **Frontend**: `shared/validation.ts` Zod şemaları bu JSON'u import eder.
- **Backend**: `PickMe.Application/Auth/ValidationRules.cs` aynı limitleri C# const olarak tutar (elle senkron; Faz 6 drift check'i bunu CI'da doğrular).

Hata mesajları **iki tarafta birebir aynı** (brief §16-D şartı).

## Mail akışı (Hangfire)

```
AuthService / ReservationService / ...
  └─ IEmailQueue.EnqueueAsync(EmailMessage)
         └─ HangfireEmailQueue → IBackgroundJobClient.Enqueue<EmailJobRunner>()
              └─ SQL tablosuna persist (HangFire schema)
                   └─ Worker (backend process içinde) job'u alır
                        └─ EmailJobRunner.SendAsync
                             ├─ MailKit SMTP → Sent (EmailLog.MarkSent)
                             └─ Exception → [AutomaticRetry(3, delays 5dk/30dk/2h)]
                                             → 3 fail sonra EmailLog.MarkFailed
```

- Admin `/hangfire` dashboard'undan failed job'ları manuel requeue edebilir.
- `EmailLogs` tablosu her gönderim denemesini audit'ler.

## Frontend rotalar

```
/                               (public)
/hizmetler/sofor, /vale         (public)
/hakkimizda, /sss, /iletisim    (public)
/kvkk, /gizlilik, /kullanim-sartlari
/giris, /kayit, /eposta-dogrula
/sifremi-unuttum, /sifre-sifirla

/rezervasyon                    (Customer only)
/hesabim
  /rezervasyonlar               (liste)
  /rezervasyonlar/:id           (detay + iptal + rating)
  /profil                       (profil + şifre)

/driver                         (Driver only)
  /gorevler/:id                 (detay + start/complete)
  /sifre-degistir               (zorunlu ilk girişte)

/admin                          (Admin only)
  /rezervasyonlar, /rezervasyonlar/:id
  /soforler, /musteriler
  /degerlendirmeler, /mesajlar
  /sss, /bildirim-alicilari
  /yoneticiler, /ayarlar
```

`RequireAuth` komponent role-based guard + `MustChangePassword=true` şoförleri otomatik şifre değiştirme sayfasına yönlendirir.

## API envelope

Tüm response'lar:

```json
{
  "success": true | false,
  "data": ...,
  "message": "insan okuyabilir mesaj",
  "errors": { "fieldName": ["hata 1", "hata 2"] },
  "code": "auth.invalid_credentials | validation | reservation.invalid_transition | ..."
}
```

HTTP status code'ları `ResultExtensions.ToActionResult`:
- 200 / 201 — success
- 400 — validation
- 401 — invalid credentials, invalid/expired token
- 403 — email not verified, role mismatch
- 404 — not found (IDOR için de 404)
- 409 — duplicate, invalid state transition, concurrency
- 423 — account locked

## Güvenlik

- JWT HS256, min 256-bit secret, 30sn clock skew tolerance
- BCrypt enhanced hash, cost 12 (prod)
- SHA-256 opaque token hash (email verify / password reset / refresh)
- Security headers: CSP, HSTS, X-Frame-Options DENY, X-Content-Type-Options, Referrer-Policy, Permissions-Policy
- CORS env var'dan (prod'da yalnızca frontend origin)
- Rate limiting — TODO: Faz 2'de tanımlı ama backend'de henüz aktif edilmedi (`AspNetCoreRateLimit` paketi yüklü, Program.cs'e wiring gerekli)
- Mail template'lerde kullanıcı input'u `HtmlEncoder` ile escape (XSS engeli)
- Hangfire dashboard `AdminOnlyHangfireFilter` — JWT rol kontrolü
