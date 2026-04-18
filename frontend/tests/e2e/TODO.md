# Playwright E2E — Kalan Senaryolar

Brief §16-E minimum 10 senaryo talep ediyor. Şu an yazılan:

## ✅ Mevcut
1. Ana sayfa hero + CTA render
2. WhatsApp FAB her public sayfada
3. SSS accordion
4. İletişim formu validation
5. 404 sayfası
6. Kayıt formu Zod validation
7. Giriş sayfası render
8. Admin login + dashboard
9. Yetkisiz /admin erişimi → /giris redirect

## ⏳ Yazılacak (brief listesi + kalan)

10. **Customer register → verify → login** (smtp4dev HTTP API'den token çekme)
11. **Reservation creation** (LocationPicker'dan preset seç, form submit, başarı ekranı)
12. **Admin assign driver** → mail kontrol (smtp4dev API)
13. **Driver start → complete** (ikinci browser context)
14. **Rating flow** (Complete sonrası rating modal, yıldız + yorum, avg güncelleme)
15. **Customer cancel** (Pending OK, Assigned 403)
16. **Admin cancel + reassign**
17. **Forgot password** (token'ı smtp4dev'den yakala, reset)
18. **Cross-customer IDOR** (A'nın rezervasyonuna B erişemiyor)
19. **Driver first-login must-change-password redirect**

## Ortam

Bu senaryolar tam çalışmak için şunlar gerekir:
- **MSSQL LocalDB** veya Docker MSSQL — `dotnet ef database update` çalışmış
- **Backend** — `http://localhost:5080` (seed admin aktif)
- **smtp4dev** (mail doğrulama token'ı çekmek için) — HTTP API `http://localhost:5050/api/Messages`
- **Google Maps stub** — Frontend'deki LocationPicker preset listesi kullanılır (API key yoksa)

Docker Compose ile kolayca: `docker compose -f infra/dev/docker-compose.yml up -d`

## CI entegrasyonu

`.github/workflows/ci.yml` → `e2e` job'ı mssql + smtp4dev service container'larıyla
ayaklandırır; chromium'u indirir; Playwright'ı çalıştırır. Lokal CI konfigürasyonu
hazır ama self-hosted runner'lar Windows tabanlı olduğu için production CI'da
dockerize edilmiş mssql yerine LocalDB tercih edilecek.

## Helper functions yazılabilir

- `fixtures/seed.ts` — her test öncesi DB'yi reset + customer/driver seed et (HTTP fetch ile backend admin endpoint'lerini kullanarak)
- `fixtures/mail.ts` — smtp4dev API'den son mail'i çekip token parse et
- `fixtures/auth.ts` — roleBasedLogin helper (customer/driver/admin)
