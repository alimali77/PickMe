# Pick Me — Production deploy script (Windows + IIS).
# Kullanım:  .\deploy.ps1 -ApiPath "C:\inetpub\pickme\api" -WebPath "C:\inetpub\pickme\web"
#
# Gerekli:
#   - .NET 10 SDK (build için) + .NET 10 Hosting Bundle (IIS için)
#   - Node.js 22+ ve pnpm 10+
#   - IIS + URL Rewrite Module + ARR (reverse proxy kullanılacaksa)
#
# Bu script:
#   1. Backend'i Release mode build + publish eder.
#   2. Frontend'i build eder.
#   3. IIS app pool'u durdurup dosyaları kopyalar ve pool'u geri başlatır.
#   4. Smoke test'i çalıştırır.
[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string] $ApiPath,
    [Parameter(Mandatory=$true)][string] $WebPath,
    [string] $ApiAppPool = "PickMe.Api",
    [string] $HealthUrl  = "http://localhost:5080/health",
    [switch] $SkipBuild
)

$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
Write-Host "Repo root: $repoRoot" -ForegroundColor Cyan

# ---------------- Build ----------------
if (-not $SkipBuild) {
    Write-Host "▶ Backend publish..." -ForegroundColor Cyan
    Push-Location (Join-Path $repoRoot "backend")
    dotnet publish "src\PickMe.Api\PickMe.Api.csproj" `
        --configuration Release `
        --runtime win-x64 --self-contained false `
        --output "$repoRoot\artifacts\api"
    if ($LASTEXITCODE -ne 0) { Pop-Location; throw "Backend publish failed." }
    Pop-Location

    Write-Host "▶ Frontend build..." -ForegroundColor Cyan
    Push-Location $repoRoot
    pnpm install --frozen-lockfile
    pnpm -C frontend run build
    if ($LASTEXITCODE -ne 0) { Pop-Location; throw "Frontend build failed." }
    Pop-Location
}

# ---------------- IIS stop ----------------
Import-Module WebAdministration -ErrorAction SilentlyContinue
$poolExists = (Get-ChildItem "IIS:\AppPools" -ErrorAction SilentlyContinue | Where-Object Name -eq $ApiAppPool)
if ($poolExists) {
    Write-Host "⏸ Stopping app pool '$ApiAppPool'..." -ForegroundColor Yellow
    Stop-WebAppPool -Name $ApiAppPool -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 3
} else {
    Write-Warning "App pool '$ApiAppPool' not found — skipping stop."
}

# ---------------- Copy API ----------------
Write-Host "📦 Copying API → $ApiPath" -ForegroundColor Cyan
if (-not (Test-Path $ApiPath)) { New-Item -Path $ApiPath -ItemType Directory -Force | Out-Null }

# appsettings.Development.json'u prod'a kopyalama — prod env var'larla çalışmalı
robocopy "$repoRoot\artifacts\api" $ApiPath /E /XO /XF "appsettings.Development.json" /NFL /NDL /NJH /NJS /NC /NS
if ($LASTEXITCODE -ge 8) { throw "Robocopy failed for API." }

# IIS web.config kopyala (yalnızca yoksa — mevcut prod environment variables korunsun)
if (-not (Test-Path "$ApiPath\web.config")) {
    Copy-Item "$repoRoot\infra\iis\web.api.config" "$ApiPath\web.config" -Force
    Write-Host "ℹ Copied web.api.config → $ApiPath\web.config (DÜZELT: env var'ları doldurun)" -ForegroundColor Yellow
}

# ---------------- Copy Web ----------------
Write-Host "📦 Copying Web → $WebPath" -ForegroundColor Cyan
if (-not (Test-Path $WebPath)) { New-Item -Path $WebPath -ItemType Directory -Force | Out-Null }
robocopy "$repoRoot\frontend\dist" $WebPath /E /MIR /NFL /NDL /NJH /NJS /NC /NS
if ($LASTEXITCODE -ge 8) { throw "Robocopy failed for Web." }

Copy-Item "$repoRoot\infra\iis\web.web.config" "$WebPath\web.config" -Force

# ---------------- IIS start ----------------
if ($poolExists) {
    Write-Host "▶ Starting app pool..." -ForegroundColor Cyan
    Start-WebAppPool -Name $ApiAppPool
    Start-Sleep -Seconds 5
}

# ---------------- Smoke test ----------------
Write-Host "🧪 Smoke test: $HealthUrl" -ForegroundColor Cyan
$attempts = 0
$maxAttempts = 12
$ok = $false
while (-not $ok -and $attempts -lt $maxAttempts) {
    try {
        $response = Invoke-RestMethod -Uri $HealthUrl -UseBasicParsing -TimeoutSec 5
        if ($response.status -eq "ok") { $ok = $true; break }
    } catch {
        Start-Sleep -Seconds 3
        $attempts++
    }
}
if ($ok) {
    Write-Host "✅ Deploy OK — health endpoint responding." -ForegroundColor Green
} else {
    throw "Smoke test failed — /health not responding after $maxAttempts attempts. Logs: $ApiPath\logs"
}
