#Requires -Modules WebAdministration
<#
.SYNOPSIS
    Publishes HAMS.WebHost and deploys it into an existing IIS site, run by the CD job's
    "deploy" step on the self-hosted runner (which lives on the same box as IIS/SQL Server, so
    every step here is a local operation - no remote copy/PSRemoting needed).

.DESCRIPTION
    1. Validates the secrets this script needs are actually present (an empty JWT signing key
       would let the app start up "successfully" with a silently broken auth system - fail loud
       instead).
    2. dotnet publish (Release) to a scratch folder.
    3. Stops the app pool and waits for it to actually reach Stopped (a reported "Stopped" state
       doesn't guarantee w3wp.exe has released its file locks yet).
    4. Copies the publish output into the site's physical path - NOT a mirror/purge copy, so
       anything already living in that folder that isn't part of the published app (future
       Platform.Documents local storage, ad hoc logs) is left alone; appsettings.Production.json
       specifically is never part of the publish output at all (it's git-ignored) so it's
       naturally preserved rather than needing special-case exclusion - but this script always
       regenerates it fresh from secrets anyway, so it can never go stale.
    5. Writes appsettings.Production.json from the secrets passed in as environment variables.
    6. Starts the app pool and polls /health until the app actually answers.

.NOTES
    Run this ON the IIS box (as the self-hosted runner does). Requires the IIS PowerShell module
    (installed with the Web-Scripting-Tools Windows feature) and an app pool/site that already
    exist - see deploy/SETUP.md for the one-time server preparation steps.
#>

[CmdletBinding()]
param(
    [string]$SiteName = "HAMS",
    [string]$AppPoolName = "HAMS",
    [string]$SitePhysicalPath = "C:\inetpub\wwwroot\hams",
    [string]$ProjectPath = "src\Host\HAMS.WebHost\HAMS.WebHost.csproj",
    [string]$PublishOutputPath = "$PSScriptRoot\_work\publish",
    [string]$HealthCheckUrl = "http://localhost:8081/health",
    [int]$AppPoolStopTimeoutSeconds = 30,
    [int]$HealthCheckTimeoutSeconds = 60
)

$ErrorActionPreference = "Stop"
Import-Module WebAdministration

function Assert-EnvVar {
    param([string]$Name)
    $value = [System.Environment]::GetEnvironmentVariable($Name)
    if ([string]::IsNullOrWhiteSpace($value)) {
        throw "Required environment variable '$Name' is not set (or empty) - refusing to deploy with a missing secret. Check the workflow's 'env:' block and the repository secret of the same name."
    }
    return $value
}

Write-Host "== 1. Validating required secrets =="
$jwtSigningKey = Assert-EnvVar "HAMS_JWT_SIGNING_KEY"
$msgowlApiKey  = [System.Environment]::GetEnvironmentVariable("HAMS_MSGOWL_API_KEY")
$msgowlSender  = [System.Environment]::GetEnvironmentVariable("HAMS_MSGOWL_SENDER_ID")
$msgowlEnabled = [System.Environment]::GetEnvironmentVariable("HAMS_MSGOWL_ENABLED")
if ([string]::IsNullOrWhiteSpace($msgowlEnabled)) { $msgowlEnabled = "false" }

Write-Host "== 2. dotnet publish (Release) =="
if (Test-Path $PublishOutputPath) {
    Remove-Item $PublishOutputPath -Recurse -Force
}
dotnet publish $ProjectPath -c Release -o $PublishOutputPath
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

Write-Host "== 3. Stopping app pool '$AppPoolName' =="
if ((Get-WebAppPoolState -Name $AppPoolName).Value -ne "Stopped") {
    Stop-WebAppPool -Name $AppPoolName
}
$deadline = (Get-Date).AddSeconds($AppPoolStopTimeoutSeconds)
while ((Get-WebAppPoolState -Name $AppPoolName).Value -ne "Stopped") {
    if ((Get-Date) -gt $deadline) {
        throw "App pool '$AppPoolName' did not reach Stopped within $AppPoolStopTimeoutSeconds seconds."
    }
    Start-Sleep -Seconds 1
}
# The app pool reporting Stopped doesn't guarantee w3wp.exe has exited yet - give the runtime a
# moment to actually release the previous deployment's file locks.
Start-Sleep -Seconds 3

Write-Host "== 4. Copying publish output to '$SitePhysicalPath' =="
if (-not (Test-Path $SitePhysicalPath)) {
    New-Item -ItemType Directory -Path $SitePhysicalPath -Force | Out-Null
}
robocopy $PublishOutputPath $SitePhysicalPath /E /NFL /NDL /NP /R:3 /W:2
# Robocopy's own exit codes: 0-7 are all "succeeded to some degree" (7 = files copied + some
# already-current + some mismatched, still success); only 8+ is a real failure.
if ($LASTEXITCODE -ge 8) {
    throw "robocopy failed with exit code $LASTEXITCODE."
}

Write-Host "== 5. Writing appsettings.Production.json from secrets =="
$productionSettings = @{
    ConnectionStrings = @{
        DefaultConnection = "Server=.;Database=HAMS;Trusted_Connection=True;TrustServerCertificate=True;"
    }
    Jwt = @{
        Issuer                     = "HAMS"
        Audience                  = "HAMS.Clients"
        SigningKey                = $jwtSigningKey
        AccessTokenLifetimeMinutes = 15
        RefreshTokenLifetimeDays   = 30
    }
    Msgowl = @{
        Enabled  = [System.Convert]::ToBoolean($msgowlEnabled)
        ApiKey   = $msgowlApiKey
        SenderId = $msgowlSender
        BaseUrl  = "https://rest.msgowl.com"
    }
}
$productionSettings | ConvertTo-Json -Depth 5 | Set-Content -Path (Join-Path $SitePhysicalPath "appsettings.Production.json") -Encoding utf8

Write-Host "== 6. Starting app pool '$AppPoolName' =="
Start-WebAppPool -Name $AppPoolName

Write-Host "== 7. Waiting for /health =="
$deadline = (Get-Date).AddSeconds($HealthCheckTimeoutSeconds)
$healthy = $false
while ((Get-Date) -lt $deadline) {
    try {
        $response = Invoke-WebRequest -Uri $HealthCheckUrl -UseBasicParsing -TimeoutSec 5
        if ($response.StatusCode -eq 200) {
            $healthy = $true
            break
        }
    }
    catch {
        # Not up yet (or mid-migration on a fresh database) - keep polling until the timeout.
    }
    Start-Sleep -Seconds 2
}

if (-not $healthy) {
    throw "HAMS did not report healthy at $HealthCheckUrl within $HealthCheckTimeoutSeconds seconds after starting the app pool. Check the Windows Event Log / IIS stdout log for the actual startup error."
}

Write-Host "== Deploy succeeded - $SiteName is healthy at $HealthCheckUrl =="
