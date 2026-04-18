# Pick Me — Post-deploy smoke test.
# Brief §18: "smoke-test.ps1 — post-deploy 20 endpoint pingi"
#
# Kullanım:
#   .\smoke-test.ps1 -BaseUrl "https://pickme.example"
#   .\smoke-test.ps1 -BaseUrl "http://localhost:5080" -AdminEmail admin@... -AdminPassword ...
#
# Tüm kritik endpoint'leri test eder:
#   - /health (anonymous 200)
#   - /api/faqs (anonymous 200)
#   - /api/reservations (auth'suz → 401 BEKLENEN)
#   - /api/auth/login (200 + token)
#   - /api/auth/me (auth → 200)
#   - /api/admin/drivers (admin → 200)
#   - /api/admin/reservations (admin → 200)
#   - /api/admin/recipients (admin → 200)
#   - /api/admin/settings (admin → 200)
#
# Her test için beklenen status code ile gerçek karşılaştırılır. 1 bile fail varsa exit code != 0.

[CmdletBinding()]
param(
    [Parameter(Mandatory=$true)][string] $BaseUrl,
    [string] $AdminEmail    = "admin@pickme.local",
    [string] $AdminPassword = "Admin123!Change"
)

$ErrorActionPreference = "Stop"
$failures = @()

function Test-Endpoint {
    param(
        [string]$Name,
        [string]$Url,
        [int]$Expected,
        [string]$Method = "GET",
        [hashtable]$Headers = @{},
        [string]$Body = $null
    )
    try {
        $params = @{ Uri = $Url; Method = $Method; Headers = $Headers; UseBasicParsing = $true; TimeoutSec = 10 }
        if ($Body) {
            $params.Body = $Body
            $params.ContentType = "application/json"
        }
        $resp = Invoke-WebRequest @params -ErrorAction SilentlyContinue -SkipHttpErrorCheck:$true
        $code = if ($resp) { [int]$resp.StatusCode } else { 0 }
    } catch {
        # Older PowerShell without -SkipHttpErrorCheck
        try { $code = [int]$_.Exception.Response.StatusCode } catch { $code = 0 }
    }

    if ($code -eq $Expected) {
        Write-Host ("✅ {0,-35} {1,3}  {2}" -f $Name, $code, $Url) -ForegroundColor Green
        return $true
    } else {
        Write-Host ("❌ {0,-35} {1,3} (beklenen {2}) {3}" -f $Name, $code, $Expected, $Url) -ForegroundColor Red
        return $false
    }
}

Write-Host "`n=== Pick Me Smoke Test: $BaseUrl ===`n" -ForegroundColor Cyan

# Anonymous
if (-not (Test-Endpoint "health" "$BaseUrl/health" 200)) { $failures += "health" }
if (-not (Test-Endpoint "faqs"   "$BaseUrl/api/faqs" 200)) { $failures += "faqs" }
if (-not (Test-Endpoint "reservations(401)" "$BaseUrl/api/reservations" 401)) { $failures += "reservations-auth-guard" }
if (-not (Test-Endpoint "admin/drivers(401)" "$BaseUrl/api/admin/drivers" 401)) { $failures += "admin-auth-guard" }

# Login
Write-Host "`n--- Authenticated smoke tests ---" -ForegroundColor Cyan
try {
    $loginBody = @{ email = $AdminEmail; password = $AdminPassword } | ConvertTo-Json
    $loginResp = Invoke-RestMethod -Uri "$BaseUrl/api/auth/login" -Method Post -Body $loginBody -ContentType "application/json" -TimeoutSec 10
    if ($loginResp.success) {
        $token = $loginResp.data.accessToken
        Write-Host "✅ login                              200  got access token" -ForegroundColor Green
        $headers = @{ Authorization = "Bearer $token" }
        if (-not (Test-Endpoint "auth/me"            "$BaseUrl/api/auth/me" 200 "GET" $headers)) { $failures += "me" }
        if (-not (Test-Endpoint "admin/drivers"      "$BaseUrl/api/admin/drivers" 200 "GET" $headers)) { $failures += "admin-drivers" }
        if (-not (Test-Endpoint "admin/reservations" "$BaseUrl/api/admin/reservations" 200 "GET" $headers)) { $failures += "admin-reservations" }
        if (-not (Test-Endpoint "admin/recipients"   "$BaseUrl/api/admin/recipients" 200 "GET" $headers)) { $failures += "admin-recipients" }
        if (-not (Test-Endpoint "admin/settings"     "$BaseUrl/api/admin/settings" 200 "GET" $headers)) { $failures += "admin-settings" }
        if (-not (Test-Endpoint "admin/customers"    "$BaseUrl/api/admin/customers" 200 "GET" $headers)) { $failures += "admin-customers" }
        if (-not (Test-Endpoint "admin/faqs"         "$BaseUrl/api/admin/faqs" 200 "GET" $headers)) { $failures += "admin-faqs" }
        if (-not (Test-Endpoint "admin/ratings"      "$BaseUrl/api/admin/ratings" 200 "GET" $headers)) { $failures += "admin-ratings" }
        if (-not (Test-Endpoint "admin/admins"       "$BaseUrl/api/admin/admins" 200 "GET" $headers)) { $failures += "admin-admins" }
        if (-not (Test-Endpoint "admin/contact"      "$BaseUrl/api/admin/contact-messages" 200 "GET" $headers)) { $failures += "admin-contact" }
        if (-not (Test-Endpoint "active-drivers"     "$BaseUrl/api/admin/drivers/active" 200 "GET" $headers)) { $failures += "active-drivers" }
    } else {
        Write-Host "❌ login returned success=false" -ForegroundColor Red
        $failures += "login"
    }
} catch {
    Write-Host "❌ login failed: $_" -ForegroundColor Red
    $failures += "login-exception"
}

Write-Host ""
if ($failures.Count -eq 0) {
    Write-Host "🎉 Smoke test PASSED — tüm endpoint'ler beklendiği gibi cevap verdi." -ForegroundColor Green
    exit 0
} else {
    Write-Host "💥 Smoke test FAILED ($($failures.Count) endpoint):" -ForegroundColor Red
    $failures | ForEach-Object { Write-Host "   - $_" -ForegroundColor Red }
    exit 1
}
