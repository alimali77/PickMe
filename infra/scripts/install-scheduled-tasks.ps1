# Pick Me — Scheduled Task kurulumu (admin elevated olarak çalıştırılmalı).
# Brief §18: "günlük otomatik DB backup + aylık restore dry-run".
#
# Oluşturduğu task'lar:
#   - PickMe-BackupFull       Günlük 02:00  → full DB backup
#   - PickMe-BackupLog        Her saat :30  → transaction log backup
#   - PickMe-RestoreDryRun    Ayın 1'i 03:30 → restore doğrulama (dry-run)

[CmdletBinding()]
param(
    [string] $ScriptDir = (Split-Path -Parent $MyInvocation.MyCommand.Path),
    [string] $BackupPath = "D:\Backups\PickMe",
    [string] $SqlInstance = "(localdb)\MSSQLLocalDB",
    [string] $Database = "PickMeDB"
)

function Ensure-Task {
    param([string]$Name, [string]$Command, [string]$ArgumentsStr, $Trigger, $Principal)
    $existing = Get-ScheduledTask -TaskName $Name -ErrorAction SilentlyContinue
    if ($existing) {
        Write-Host "↻ Updating $Name" -ForegroundColor Yellow
        Unregister-ScheduledTask -TaskName $Name -Confirm:$false
    } else {
        Write-Host "➕ Creating $Name" -ForegroundColor Green
    }
    $action = New-ScheduledTaskAction -Execute $Command -Argument $ArgumentsStr
    Register-ScheduledTask -TaskName $Name -Action $action -Trigger $Trigger -Principal $Principal -RunLevel Highest | Out-Null
}

$principal = New-ScheduledTaskPrincipal -UserId "SYSTEM" -LogonType ServiceAccount -RunLevel Highest
$ps = "powershell.exe"

$full  = "-NoProfile -ExecutionPolicy Bypass -File `"$ScriptDir\backup-db.ps1`" -Type Full -SqlInstance `"$SqlInstance`" -Database `"$Database`" -BackupPath `"$BackupPath`""
$log   = "-NoProfile -ExecutionPolicy Bypass -File `"$ScriptDir\backup-db.ps1`" -Type Log -SqlInstance `"$SqlInstance`" -Database `"$Database`" -BackupPath `"$BackupPath`""
$dry   = "-NoProfile -ExecutionPolicy Bypass -Command `"$ScriptDir\restore-db.ps1 -BackupFile (Get-ChildItem '$BackupPath' -Filter '*_Full_*.bak' | Sort-Object LastWriteTime -Descending | Select-Object -First 1).FullName -TargetDatabase '$Database`_RestoreTest' -DropAfter`""

Ensure-Task -Name "PickMe-BackupFull"    -Command $ps -ArgumentsStr $full `
    -Trigger (New-ScheduledTaskTrigger -Daily -At 2:00AM) -Principal $principal

Ensure-Task -Name "PickMe-BackupLog"     -Command $ps -ArgumentsStr $log `
    -Trigger (New-ScheduledTaskTrigger -Once -At (Get-Date).Date.AddMinutes(30) -RepetitionInterval (New-TimeSpan -Hours 1)) -Principal $principal

Ensure-Task -Name "PickMe-RestoreDryRun" -Command $ps -ArgumentsStr $dry `
    -Trigger (New-ScheduledTaskTrigger -Weekly -WeeksInterval 4 -DaysOfWeek Monday -At 3:30AM) -Principal $principal

Write-Host "`n✅ Scheduled tasks installed. Task Scheduler'ı 'PickMe-*' ile filtreleyin." -ForegroundColor Green
