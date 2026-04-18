# Pick Me — MSSQL otomatik yedekleme scripti (Windows Scheduled Task için).
#
# Çalıştırma örneği (task scheduler):
#   .\backup-db.ps1 -SqlInstance "localhost" -Database "PickMeDB" -BackupPath "D:\Backups\PickMe"
#
# Brief §18: günlük otomatik DB backup, 30 gün saklanır.
#
# 02:00'de full backup + saat başı transaction log backup (full recovery model ise).
# Bu script tek bir çalışmada full backup alır; tx log için ayrı scheduled task.

[CmdletBinding()]
param(
    [string] $SqlInstance = "(localdb)\MSSQLLocalDB",
    [string] $Database    = "PickMeDB",
    [string] $BackupPath  = "D:\Backups\PickMe",
    [int]    $RetentionDays = 30,
    [ValidateSet("Full","Log")][string] $Type = "Full"
)

$ErrorActionPreference = "Stop"
$timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
if (-not (Test-Path $BackupPath)) {
    New-Item -Path $BackupPath -ItemType Directory -Force | Out-Null
}

$extension = if ($Type -eq "Log") { "trn" } else { "bak" }
$file = Join-Path $BackupPath "$Database`_$Type`_$timestamp.$extension"

Write-Host "🗄  $Type backup → $file" -ForegroundColor Cyan

if ($Type -eq "Full") {
    $query = "BACKUP DATABASE [$Database] TO DISK = N'$file' WITH FORMAT, INIT, COMPRESSION, NAME = 'PickMe Full Backup', STATS = 10;"
} else {
    $query = "BACKUP LOG [$Database] TO DISK = N'$file' WITH FORMAT, INIT, COMPRESSION, NAME = 'PickMe Log Backup';"
}

sqlcmd -S $SqlInstance -Q $query -b
if ($LASTEXITCODE -ne 0) { throw "Backup failed (exit $LASTEXITCODE)." }

# Retention: eski dosyaları sil
$threshold = (Get-Date).AddDays(-$RetentionDays)
$removed = Get-ChildItem $BackupPath -Filter "$Database`_$Type`_*.$extension" |
    Where-Object { $_.LastWriteTime -lt $threshold } |
    ForEach-Object { Remove-Item $_.FullName -Force; $_.Name }
if ($removed) {
    Write-Host "🧹 Removed old backups (>$RetentionDays days):" -ForegroundColor Gray
    $removed | ForEach-Object { Write-Host "   $_" -ForegroundColor DarkGray }
}

Write-Host "✅ Backup complete: $file" -ForegroundColor Green
