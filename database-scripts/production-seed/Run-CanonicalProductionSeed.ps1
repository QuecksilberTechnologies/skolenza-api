#requires -Version 5.1
<#
.SYNOPSIS
Runs the complete canonical production reset/seed sequence and writes execution logs.
.DESCRIPTION
Loads ConnectionStrings:DefaultConnection from the selected API environment,
verifies the exact database name, and runs every canonical SQL stage in the documented
order with ON_ERROR_STOP enabled. The connection string and password are never logged.

Each execution creates one timestamped folder under execution-logs containing a combined
log, one log per SQL stage, and a CSV summary. A failed stage stops the remaining stages.
.EXAMPLE
./Run-CanonicalProductionSeed.ps1 -Environment Development -PsqlPath 'C:\Program Files\PostgreSQL\18\bin\psql.exe'
.EXAMPLE
./Run-CanonicalProductionSeed.ps1 -Environment Development -ValidateOnly
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Development', 'Production', 'Staging')]
    [string] $Environment,

    [string] $PsqlPath = 'psql',

    [string] $SmtpKey = $env:SKOLENZA_SEED_SMTP_KEY,

    [switch] $ValidateOnly
)

$ErrorActionPreference = 'Stop'
$expectedDatabaseName = 'workforcedb_34hi_duis'
$seedRoot = $PSScriptRoot
$repositoryRoot = Split-Path (Split-Path $seedRoot -Parent) -Parent
$apiDirectory = Join-Path $repositoryRoot 'skolenza.api'
$backupPath = Join-Path $repositoryRoot 'DBFullBACKUP/workforcedb_34hi_duis-before-canonical-seed-20260928.dump'

$stages = @(
    [pscustomobject]@{ Order = 1; Name = 'clean'; Script = '01-clean/001-reset-all-data.sql' },
    [pscustomobject]@{ Order = 2; Name = 'parent-master'; Script = '02-parent-master/001-shared-master-data.sql' },
    [pscustomobject]@{ Order = 3; Name = 'country-catalog'; Script = '03-geography/000-iso-country-catalog.sql' },
    [pscustomobject]@{ Order = 4; Name = 'geography'; Script = '03-geography/001-four-country-postal-catalog.sql' },
    [pscustomobject]@{ Order = 5; Name = 'country-regulatory-master'; Script = '03-geography/002-country-regulatory-master.sql' },
    [pscustomobject]@{ Order = 6; Name = 'access-and-host'; Script = '04-access-and-host/001-modules-operations-two-host-admins.sql' },
    [pscustomobject]@{ Order = 7; Name = 'host-institution-master-data'; Script = '../SeedHostInstitutionMasterData.sql' },
    [pscustomobject]@{ Order = 8; Name = 'dependent-master'; Script = '05-dependent-master/001-employee-identity-catalog.sql' },
    [pscustomobject]@{ Order = 9; Name = 'verification'; Script = '99-verification/001-verify-canonical-seed.sql' }
    [pscustomobject]@{ Order = 10; Name = 'host-managed-staff-category-range'; Script = '../migrations/20261017_AllowHostManagedStaffCategories.sql' }
)

function Read-ConnectionSetting([string] $path) {
    if (-not (Test-Path -LiteralPath $path)) {
        return $null
    }

    try {
        $content = (Get-Content -LiteralPath $path |
            Where-Object { $_ -notmatch '^\s*//' }) -join [Environment]::NewLine
        $match = [regex]::Match(
            $content,
            '"DefaultConnection"\s*:\s*"(?<value>(?:\\.|[^"\\])*)"',
            [System.Text.RegularExpressions.RegexOptions]::IgnoreCase
        )

        if (-not $match.Success) {
            return $null
        }

        # Decode JSON string escapes without parsing the complete appsettings file.
        # Development settings may contain // comments that Windows PowerShell 5.1
        # cannot parse through ConvertFrom-Json.
        return ('"' + $match.Groups['value'].Value + '"' | ConvertFrom-Json)
    }
    catch {
        throw "Cannot parse configuration file: $path. Configuration contents were not logged."
    }
}

$connectionString = $null
foreach ($file in @('appsettings.json', "appsettings.$Environment.json")) {
    $candidate = Read-ConnectionSetting (Join-Path $apiDirectory $file)
    if (-not [string]::IsNullOrWhiteSpace($candidate)) {
        $connectionString = $candidate
    }
}

if ($Environment -eq 'Development' -and
    (Test-Path -LiteralPath (Join-Path $apiDirectory 'skolenza.api.csproj'))) {
    [xml] $project = Get-Content -LiteralPath (Join-Path $apiDirectory 'skolenza.api.csproj') -Raw
    $secretId = $project.SelectSingleNode('//UserSecretsId')
    if ($null -ne $secretId) {
        $isWindowsPlatform = $env:OS -eq 'Windows_NT'
        $secretRoot = if ($isWindowsPlatform) {
            Join-Path ([Environment]::GetFolderPath('ApplicationData')) 'Microsoft/UserSecrets'
        }
        else {
            Join-Path ([Environment]::GetFolderPath('UserProfile')) '.microsoft/usersecrets'
        }

        $candidate = Read-ConnectionSetting (Join-Path $secretRoot "$($secretId.InnerText)/secrets.json")
        if (-not [string]::IsNullOrWhiteSpace($candidate)) {
            $connectionString = $candidate
        }
    }
}

if (-not [string]::IsNullOrWhiteSpace($env:ConnectionStrings__DefaultConnection)) {
    $connectionString = $env:ConnectionStrings__DefaultConnection
}

if ([string]::IsNullOrWhiteSpace($connectionString)) {
    throw 'ConnectionStrings:DefaultConnection is missing for the selected API environment.'
}

try {
    $connection = [System.Data.Common.DbConnectionStringBuilder]::new()
    $connection.set_ConnectionString($connectionString)
}
catch {
    throw 'Invalid database connection string. Its contents were not logged.'
}

function Get-ConnectionValue([string[]] $keys, [string] $fallback = '') {
    foreach ($key in $keys) {
        if ($connection.ContainsKey($key)) {
            return [string] $connection[$key]
        }
    }

    return $fallback
}

$databaseHost = Get-ConnectionValue @('Host', 'Server')
$databaseName = Get-ConnectionValue @('Database', 'Initial Catalog')
$databaseUser = Get-ConnectionValue @('Username', 'User ID', 'UserId', 'User Name')
$databasePort = Get-ConnectionValue @('Port') '5432'
$databasePassword = Get-ConnectionValue @('Password')
$sslMode = Get-ConnectionValue @('SSL Mode', 'SslMode') 'Prefer'
$sslModes = @{
    Disable = 'disable'
    Allow = 'allow'
    Prefer = 'prefer'
    Require = 'require'
    VerifyCA = 'verify-ca'
    VerifyFull = 'verify-full'
}

if (-not $databaseHost -or -not $databaseName -or -not $databaseUser) {
    throw 'Host, database and username are required in the selected connection string.'
}

if ($databaseName -ne $expectedDatabaseName) {
    throw "Database guard rejected '$databaseName'. Expected '$expectedDatabaseName'."
}

if (-not $sslModes.ContainsKey($sslMode)) {
    throw "Unsupported PostgreSQL SSL mode '$sslMode'."
}

if (-not (Test-Path -LiteralPath $backupPath)) {
    throw "Required verified backup is missing: $backupPath"
}

foreach ($stage in $stages) {
    $stagePath = Join-Path $seedRoot $stage.Script
    if (-not (Test-Path -LiteralPath $stagePath)) {
        throw "Canonical SQL stage is missing: $stagePath"
    }
}

Write-Output "Environment: $Environment"
Write-Output "Target: ${databaseHost}:${databasePort}/$databaseName"
Write-Output "Stages: $($stages.Count)"
Write-Output "Backup: $backupPath"

if ($ValidateOnly) {
    Write-Output 'VALIDATION PASSED. No database connection was opened and no SQL was executed.'
    return
}

if ([string]::IsNullOrWhiteSpace($SmtpKey)) {
    throw 'SMTP key is required. Pass -SmtpKey or set SKOLENZA_SEED_SMTP_KEY. The key is never written to logs.'
}

if (-not (Get-Command $PsqlPath -ErrorAction SilentlyContinue)) {
    if ($PsqlPath -eq 'psql' -and $env:ProgramFiles) {
        $detectedPsql = Get-ChildItem `
            -Path (Join-Path $env:ProgramFiles 'PostgreSQL') `
            -Filter 'psql.exe' `
            -File `
            -Recurse `
            -ErrorAction SilentlyContinue |
            Where-Object { $_.FullName -notmatch 'pgAdmin' } |
            Sort-Object FullName -Descending |
            Select-Object -First 1

        if ($null -ne $detectedPsql) {
            $PsqlPath = $detectedPsql.FullName
        }
    }

    if (-not (Get-Command $PsqlPath -ErrorAction SilentlyContinue)) {
        throw 'psql was not found automatically. Pass -PsqlPath with the installed psql executable path.'
    }
}

$runId = Get-Date -Format 'yyyyMMdd-HHmmss'
$logDirectory = Join-Path $seedRoot "execution-logs/$runId"
$combinedLog = Join-Path $logDirectory 'canonical-seed-combined.log'
$summaryPath = Join-Path $logDirectory 'canonical-seed-summary.csv'
New-Item -ItemType Directory -Path $logDirectory -Force | Out-Null

$summary = [System.Collections.Generic.List[object]]::new()
$pgSettings = @{
    PGHOST = $databaseHost
    PGPORT = $databasePort
    PGDATABASE = $databaseName
    PGUSER = $databaseUser
    PGPASSWORD = $databasePassword
    PGSSLMODE = $sslModes[$sslMode]
    PGCONNECT_TIMEOUT = Get-ConnectionValue @('Timeout') '15'
    PGSSLROOTCERT = Get-ConnectionValue @('Root Certificate', 'RootCertificate')
}
$savedSettings = @{}
$runStatus = 'PASSED'

try {
    foreach ($key in $pgSettings.Keys) {
        $savedSettings[$key] = [Environment]::GetEnvironmentVariable($key, 'Process')
        [Environment]::SetEnvironmentVariable($key, $pgSettings[$key], 'Process')
    }

    @(
        "Canonical production seed run: $runId"
        "Environment: $Environment"
        "Target database: $databaseName"
        "Stage count: $($stages.Count)"
        'Connection credentials: REDACTED'
        ''
    ) | Set-Content -LiteralPath $combinedLog -Encoding utf8

    foreach ($stage in $stages) {
        $startedAt = Get-Date
        $stageLabel = '{0:D2}-{1}' -f $stage.Order, $stage.Name
        $stageLog = Join-Path $logDirectory "$stageLabel.log"
        $stagePath = Join-Path $seedRoot $stage.Script

        @(
            "[$($startedAt.ToString('o'))] START $stageLabel"
            "Script: $($stage.Script)"
        ) | Tee-Object -FilePath $stageLog | Add-Content -LiteralPath $combinedLog -Encoding utf8

        # Windows PowerShell 5.1 wraps native stderr (including harmless PostgreSQL
        # NOTICE messages) as non-terminating error records. Let psql's exit code and
        # ON_ERROR_STOP determine success while still capturing stderr in the logs.
        $previousErrorActionPreference = $ErrorActionPreference
        $ErrorActionPreference = 'Continue'
        try {
            & $PsqlPath `
                --no-psqlrc `
                --no-password `
                --set ON_ERROR_STOP=1 `
                --set canonical_smtp_key=$SmtpKey `
                --file $stagePath 2>&1 |
                Tee-Object -FilePath $stageLog -Append |
                Add-Content -LiteralPath $combinedLog -Encoding utf8

            $exitCode = $LASTEXITCODE
        }
        finally {
            $ErrorActionPreference = $previousErrorActionPreference
        }
        $completedAt = Get-Date
        $status = if ($exitCode -eq 0) { 'PASSED' } else { 'FAILED' }

        $summary.Add([pscustomobject]@{
            Order = $stage.Order
            Stage = $stage.Name
            Script = $stage.Script
            Status = $status
            ExitCode = $exitCode
            StartedAt = $startedAt.ToString('o')
            CompletedAt = $completedAt.ToString('o')
            DurationSeconds = [math]::Round(($completedAt - $startedAt).TotalSeconds, 3)
            LogFile = Split-Path $stageLog -Leaf
        })
        $summary | Export-Csv -LiteralPath $summaryPath -NoTypeInformation -Encoding utf8

        "[$($completedAt.ToString('o'))] END $stageLabel - $status (exit $exitCode)" |
            Tee-Object -FilePath $stageLog -Append |
            Add-Content -LiteralPath $combinedLog -Encoding utf8

        if ($exitCode -ne 0) {
            $runStatus = 'FAILED'
            throw "Canonical seed failed at $($stage.Script). Later stages were not run. See $stageLog"
        }
    }
}
catch {
    $runStatus = 'FAILED'
    throw
}
finally {
    $finishedAt = Get-Date
    if (Test-Path -LiteralPath $combinedLog) {
        @(
            ''
            "Run status: $runStatus"
            "Finished at: $($finishedAt.ToString('o'))"
            "Summary: $summaryPath"
        ) | Add-Content -LiteralPath $combinedLog -Encoding utf8
    }

    foreach ($key in $savedSettings.Keys) {
        [Environment]::SetEnvironmentVariable($key, $savedSettings[$key], 'Process')
    }
}

Write-Output "CANONICAL SEED PASSED. Logs: $logDirectory"
