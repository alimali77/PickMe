# Pick Me — Restore script (yedekten geri yükleme veya aylık dry-run doğrulama).
# Brief §18: aylık 1 kez otomatik restore dry-run (farklı DB adına).
#
# Kullanım:
#   .\restore-db.ps1 -BackupFile "D:\Backups\PickMe\PickMeDB_Full_20260418.bak" `
#                    -TargetDatabase "PickMeDB_RestoreTest"

[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string] $BackupFile,
    [string] $SqlInstance = "(localdb)\MSSQLLocalDB",
    [Parameter(Mandatory=$true)][string] $TargetDatabase,
    [switch] $DropAfter  # Dry-run sonrası hedef DB'yi tekrar kaldır
)

$ErrorActionPreference = "Stop"
if (-not (Test-Path $BackupFile)) { throw "Backup dosyası bulunamadı: $BackupFile" }

Write-Host "📂 Reading backup file list..." -ForegroundColor Cyan
$fileListQuery = "RESTORE FILELISTONLY FROM DISK = N'$BackupFile';"
$files = sqlcmd -S $SqlInstance -Q $fileListQuery -h -1 -W -s "|"
$files | ForEach-Object { Write-Host "   $_" -ForegroundColor DarkGray }

Write-Host "🔄 Restoring as [$TargetDatabase]..." -ForegroundColor Cyan
$query = @"
RESTORE DATABASE [$TargetDatabase] FROM DISK = N'$BackupFile'
WITH REPLACE, RECOVERY, STATS = 10;
"@
sqlcmd -S $SqlInstance -Q $query -b
if ($LASTEXITCODE -ne 0) { throw "Restore failed." }

# Şemayı doğrula (14 tablo + __EFMigrationsHistory olmalı)
$verifyQuery = "USE [$TargetDatabase]; SELECT COUNT(*) AS TableCount FROM sys.tables;"
$count = sqlcmd -S $SqlInstance -Q $verifyQuery -h -1 -W
Write-Host "✅ Restore OK — $count tables detected." -ForegroundColor Green

if ($DropAfter) {
    Write-Host "🗑 Dropping dry-run DB..." -ForegroundColor Yellow
    sqlcmd -S $SqlInstance -Q "ALTER DATABASE [$TargetDatabase] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [$TargetDatabase];" -b
    Write-Host "✅ Dry-run cleanup complete." -ForegroundColor Green
}
