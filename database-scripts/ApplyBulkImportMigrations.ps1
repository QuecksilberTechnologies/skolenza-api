#requires -Version 7.0
<#
.SYNOPSIS
Applies only the bulk-import migrations to the selected API environment database.
.DESCRIPTION
Uses appsettings.json, appsettings.<Environment>.json, Development user secrets,
then ConnectionStrings__DefaultConnection. Never prints the connection string/password.
Use -ValidateOnly to check configuration and script discovery without connecting.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Development', 'Production', 'Staging')]
    [string] $Environment,
    [string] $PsqlPath = 'psql',
    [switch] $HostBulkOnly,
    [switch] $PolicyBulkOnly,
    [switch] $ValidateOnly
)
$ErrorActionPreference = 'Stop'
$releaseRoot = Split-Path $PSScriptRoot -Parent
$apiDirectory = Join-Path $releaseRoot 'skolenza.api'
if (-not (Test-Path -LiteralPath (Join-Path $apiDirectory 'appsettings.json'))) {
    # Published releases keep this script in database-scripts alongside the API root.
    $apiDirectory = $releaseRoot
}
$connectionString = $null
function Read-ConnectionSetting([string] $path) {
    if (Test-Path -LiteralPath $path) {
        try {
            $settings = Get-Content -LiteralPath $path -Raw | ConvertFrom-Json -AsHashtable
            return $settings.ConnectionStrings.DefaultConnection
        }
        catch {
            throw "Cannot parse configuration file: $path. Configuration contents were not logged."
        }
    }
}
foreach ($file in @('appsettings.json', "appsettings.$Environment.json")) {
    $candidate = Read-ConnectionSetting (Join-Path $apiDirectory $file)
    if (-not [string]::IsNullOrWhiteSpace($candidate)) { $connectionString = $candidate }
}
if ($Environment -eq 'Development' -and (Test-Path -LiteralPath (Join-Path $apiDirectory 'skolenza.api.csproj'))) {
    [xml] $project = Get-Content -LiteralPath (Join-Path $apiDirectory 'skolenza.api.csproj') -Raw
    $secretId = $project.SelectSingleNode('//UserSecretsId')
    if ($null -ne $secretId) {
        $secretRoot = if ($IsWindows) { Join-Path $env:APPDATA 'Microsoft/UserSecrets' }
            else { Join-Path ([Environment]::GetFolderPath('UserProfile')) '.microsoft/usersecrets' }
        $candidate = Read-ConnectionSetting (Join-Path $secretRoot "$($secretId.InnerText)/secrets.json")
        if (-not [string]::IsNullOrWhiteSpace($candidate)) { $connectionString = $candidate }
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
catch { throw 'Invalid database connection-string format; its contents were not logged.' }
function Get-ConnectionValue([string[]] $keys, [string] $fallback = '') {
    foreach ($key in $keys) {
        if ($connection.ContainsKey($key)) { return [string] $connection[$key] }
    }
    return $fallback
}
$databaseHost = Get-ConnectionValue @('Host', 'Server')
$databaseName = Get-ConnectionValue @('Database', 'Initial Catalog')
$databaseUser = Get-ConnectionValue @('Username', 'User ID', 'UserId', 'User Name')
$databasePort = Get-ConnectionValue @('Port') '5432'
if (-not $databaseHost -or -not $databaseName -or -not $databaseUser) {
    throw 'Host, Database and Username are required. Supply the same connection override to API and migration command.'
}
$sslModes = @{ Disable='disable'; Allow='allow'; Prefer='prefer'; Require='require'; VerifyCA='verify-ca'; VerifyFull='verify-full' }
$sslMode = Get-ConnectionValue @('SSL Mode', 'SslMode') 'Prefer'
if (-not $sslModes.ContainsKey($sslMode)) { throw 'Unsupported SSL Mode; configure an explicit supported PostgreSQL SSL mode.' }
$scripts = @('EnforceDesignationDepartmentScope.sql', 'AddDurableMasterBulkImport.sql',
    'AddTenantEmployeeTypes.sql', 'SeedTenantEmployeeTypeModule.sql', 'AddEmployeeBulkImport.sql',
    'production-seed/05-dependent-master/001-employee-identity-catalog.sql')
if ($HostBulkOnly) {
    # Existing Employee bulk deployments need only the additive Host upgrade and menu catalogue.
    $scripts = @('AddHostBulkImport.sql', 'SeedHostBulkImportModules.sql')
}
if ($PolicyBulkOnly) {
    $scripts = @('AddPolicyBulkImport.sql')
}
foreach ($script in $scripts) {
    if (-not (Test-Path -LiteralPath (Join-Path $PSScriptRoot $script))) { throw "Missing migration: $script" }
}
Write-Output "Environment: $Environment; database target: ${databaseHost}:${databasePort}/$databaseName"
if ($ValidateOnly) {
    Write-Output 'Configuration and migration paths validated. No database connection or migration was made.'
    return
}
if (-not (Get-Command $PsqlPath -ErrorAction SilentlyContinue)) { throw 'psql was not found; pass -PsqlPath with its installed path.' }
$pgSettings = @{
    PGHOST=$databaseHost; PGPORT=$databasePort; PGDATABASE=$databaseName; PGUSER=$databaseUser
    PGPASSWORD=(Get-ConnectionValue @('Password')); PGSSLMODE=$sslModes[$sslMode]
    PGCONNECT_TIMEOUT=(Get-ConnectionValue @('Timeout') '15')
    PGSSLROOTCERT=(Get-ConnectionValue @('Root Certificate', 'RootCertificate'))
}
$savedSettings = @{}
try {
    foreach ($key in $pgSettings.Keys) {
        $savedSettings[$key] = [Environment]::GetEnvironmentVariable($key, 'Process')
        [Environment]::SetEnvironmentVariable($key, $pgSettings[$key], 'Process')
    }
    foreach ($script in $scripts) {
        Write-Output "Applying $script"
        & $PsqlPath --no-psqlrc --no-password --set ON_ERROR_STOP=1 --file (Join-Path $PSScriptRoot $script)
        if ($LASTEXITCODE -ne 0) { throw "Migration failed: $script. Later scripts were not run; fix the reported data/schema problem before retrying." }
    }
    if ($PolicyBulkOnly) {
        Write-Output 'Policy bulk migration complete. Restart the API and worker, then run authenticated lifecycle acceptance.'
    }
    elseif ($HostBulkOnly) {
        Write-Output 'Host bulk migrations complete. Start/restart the API and worker; grant View/Import through the existing Host role permission flow.'
    }
    else {
        Write-Output 'Bulk migrations complete. Start/restart this environment API; synchronize existing tenant entitlements and grant EmployeeType access through existing flows.'
    }
}
finally {
    foreach ($key in $savedSettings.Keys) {
        [Environment]::SetEnvironmentVariable($key, $savedSettings[$key], 'Process')
    }
}
