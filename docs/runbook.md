# Pick Me — Operations Runbook

Brief §18 ve §19 gereğince deploy, backup, restore, rollback ve incident akışları.

---

## 1. Sunucu ön hazırlık (bir kere)

### 1.1 Windows Server + IIS

1. Rolleri yükle: **Web Server (IIS)**, ASP.NET 4.8 zorunlu değil, .NET Core Hosting değil; **.NET 10 Hosting Bundle'ı** ayrıca indirin.
2. IIS modülleri:
   - **URL Rewrite Module** (https://www.iis.net/downloads/microsoft/url-rewrite)
   - **Application Request Routing (ARR)** — reverse proxy için (frontend → backend proxy kullanılacaksa)
3. .NET 10 Hosting Bundle:
   - https://dotnet.microsoft.com/download/dotnet/10.0 → "Hosting Bundle"
4. MSSQL 2022 (Developer/Standard) — mixed authentication.

### 1.2 SSL sertifika (Let's Encrypt)

- `win-acme` (WACS) kurulumu: https://www.win-acme.com
- Scheduled task otomatik renewal kurar:
  ```powershell
  .\wacs.exe --target iis --host pickme.example --installation iis
  ```

### 1.3 İlk dizinler

```powershell
New-Item -Path "C:\inetpub\pickme\api" -ItemType Directory -Force
New-Item -Path "C:\inetpub\pickme\web" -ItemType Directory -Force
New-Item -Path "C:\inetpub\pickme\logs" -ItemType Directory -Force
New-Item -Path "D:\Backups\PickMe"     -ItemType Directory -Force
```

### 1.4 IIS siteler

Önce **app pool**:
- `PickMe.Api` — .NET CLR: **No Managed Code**, Identity: **ApplicationPoolIdentity** (veya özel servis hesabı)
- `PickMe.Web` — No Managed Code

Sonra **siteler**:
- `api.pickme.example` → `C:\inetpub\pickme\api` (port 443, SSL)
- `pickme.example` → `C:\inetpub\pickme\web` (port 443, SSL)

### 1.5 MSSQL hazırlığı

```sql
CREATE LOGIN pickme_app WITH PASSWORD = 'Strong!Password123';
CREATE DATABASE PickMeDB;
USE PickMeDB;
CREATE USER pickme_app FOR LOGIN pickme_app;
EXEC sp_addrolemember 'db_datareader', 'pickme_app';
EXEC sp_addrolemember 'db_datawriter', 'pickme_app';
GRANT EXECUTE TO pickme_app;
GRANT CREATE TABLE TO pickme_app; -- ilk migration için, sonra REVOKE edilebilir
GO
```

Not: Hangfire kendi tablolarını `HangFire` schema'sında ilk startup'ta oluşturur — user'ın schema create yetkisi olmalı.

### 1.6 Environment variables

`web.api.config` içindeki yorumlanmış örnek env var'lar ya doğrudan dosyada (development) ya da **IIS Config Editor → Encrypted Configuration** ile (production) doldurulur.

Kritik değişkenler:
- `DB_CONNECTION_STRING`
- `JWT_SECRET` (min 256-bit — `openssl rand -base64 48` veya PowerShell `[Convert]::ToBase64String((1..48 | %{Get-Random -Max 256}) -as [byte[]])`)
- `SMTP_*` — prod SMTP sağlayıcısı
- `SEED_ADMIN_EMAIL` + `SEED_ADMIN_PASSWORD` (ilk deploy'da seed edilir; sonra değiştirilir)
- `CORS_ORIGINS` — yalnızca prod frontend domain'i
- `FRONTEND_BASE_URL` — verify/reset e-posta linkleri için
- `HANGFIRE_ENABLED=true`

---

## 2. Deploy

### 2.1 İlk deploy (manuel)

```powershell
# Windows'ta, admin PowerShell
cd C:\code\pickme  # repo'nun kopyalandığı yer
.\infra\scripts\deploy.ps1 `
    -ApiPath "C:\inetpub\pickme\api" `
    -WebPath "C:\inetpub\pickme\web"
```

Bu script:
1. `dotnet publish` (Release, win-x64, framework-dependent)
2. `pnpm -C frontend run build`
3. App pool stop
4. `robocopy` ile dosya kopyalama (appsettings.Development.json hariç)
5. `web.config`'leri yerleştirme
6. App pool start
7. `/health` smoke test (12 deneme, 3sn arayla)

### 2.2 Sonraki deploy'lar

Aynı komut. Script appsettings.Development.json'u kopyalamaz ve mevcut prod `web.config`'i (env var'larla birlikte) korur.

### 2.3 Rollback

1. App pool'u durdur.
2. `C:\inetpub\pickme\api-previous` (robocopy ile önceki versiyonu yedekliyoruz — manuel backup şart).
3. Önceki versiyonu geri kopyala.
4. App pool start.

**Öneri**: Deploy öncesi manuel olarak:
```powershell
robocopy C:\inetpub\pickme\api C:\inetpub\pickme\api-rollback\$(Get-Date -Format yyyyMMdd_HHmmss) /E /NFL /NDL
```

---

## 3. Backup & Restore

### 3.1 Scheduled tasks kurulumu (tek sefer)

```powershell
.\infra\scripts\install-scheduled-tasks.ps1 `
    -SqlInstance "(localdb)\MSSQLLocalDB" `
    -Database "PickMeDB" `
    -BackupPath "D:\Backups\PickMe"
```

Oluşturulan task'lar:
- **PickMe-BackupFull** — her gün 02:00 full backup
- **PickMe-BackupLog** — her saat :30 transaction log backup (Full recovery model varsa)
- **PickMe-RestoreDryRun** — 4 haftada bir Pazartesi 03:30 dry-run restore

### 3.2 Manuel backup

```powershell
.\infra\scripts\backup-db.ps1 -Type Full
.\infra\scripts\backup-db.ps1 -Type Log
```

Retention: 30 gün (env var ile özelleştirilebilir).

### 3.3 Manuel restore (dry-run — brief §18)

```powershell
.\infra\scripts\restore-db.ps1 `
    -BackupFile "D:\Backups\PickMe\PickMeDB_Full_20260418_020000.bak" `
    -TargetDatabase "PickMeDB_RestoreTest" `
    -DropAfter
```

Çıktıda "14 tables detected" görürseniz şema sağlam. `DropAfter` flag'i test DB'yi temizler.

### 3.4 Felaket kurtarma (gerçek restore)

```powershell
# 1. App pool'u durdur
Stop-WebAppPool "PickMe.Api"

# 2. Mevcut DB'yi rename (opsiyonel, yedek olarak tut)
sqlcmd -S "." -Q "ALTER DATABASE PickMeDB MODIFY NAME = PickMeDB_Broken_$(Get-Date -Format yyyyMMdd);"

# 3. Restore (son full + gerekliyse log'lar)
.\infra\scripts\restore-db.ps1 -BackupFile "...Full_LATEST.bak" -TargetDatabase "PickMeDB"

# 4. Connection string'i doğrula ve app pool'u başlat
Start-WebAppPool "PickMe.Api"
```

---

## 4. Monitoring ve log

- **Serilog** backend — `C:\inetpub\pickme\logs\` altında günlük rolling dosyalar.
- **IIS log** — `C:\inetpub\logs\LogFiles\W3SVC*\`
- **Windows Event Viewer** → Application log'u (ASP.NET Core Module fatal error'ları).
- **Hangfire Dashboard** — https://api.pickme.example/hangfire (admin giriş sonrası). Failed job'ları, retry durumlarını gösterir.

---

## 5. Incident checklist

### API yanıt vermiyor
1. App pool çalışıyor mu? `Get-WebAppPoolState "PickMe.Api"`
2. `/health` direkt: `Invoke-RestMethod http://localhost:5080/health`
3. Son log: `Get-ChildItem C:\inetpub\pickme\logs | Sort-Object LastWriteTime -Desc | Select -First 1 | Get-Content -Tail 200`
4. Event Viewer: AspNetCoreModuleV2 fatal error var mı?

### DB bağlantı hatası
1. `DB_CONNECTION_STRING` doğru mu?
2. SQL Server servisi çalışıyor mu? `Get-Service MSSQLSERVER`
3. Login: `sqlcmd -S . -U pickme_app -P "..." -Q "SELECT 1"`

### Mail gitmiyor
1. Hangfire Dashboard'da failed job var mı?
2. `EmailLogs` tablosunda son 1 saat: `SELECT TOP 20 * FROM EmailLogs ORDER BY CreatedAtUtc DESC`
3. `AdminNotificationRecipients`'te aktif kayıt var mı? (yoksa yeni rezervasyon zaten reddedilir)
4. SMTP bilgileri `/admin/ayarlar`'da doğru mu? "Test mail" butonu çalışıyor mu?

### Şoföre mail ulaşmıyor
1. Driver oluşturulduktan hemen sonra EmailLogs kontrol.
2. Spam klasörü kontrol.
3. SMTP sağlayıcısının "From" adresi doğrulanmış mı?

---

## 6. Routine maintenance

- **Haftalık**: Disk kullanımı kontrol (`D:\Backups\PickMe` boyut), failed Hangfire job'ları temizle.
- **Aylık**: Restore dry-run otomatik çalışır, log'larını kontrol et.
- **3 ayda bir**: JWT secret rotation düşün (tüm refresh token'lar invalid olur).
- **6 ayda bir**: .NET hosting bundle güncellenmeli mi kontrol et.
