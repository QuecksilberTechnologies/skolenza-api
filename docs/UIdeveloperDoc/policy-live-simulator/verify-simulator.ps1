$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$required = @('index.html','styles.css','policy-constants.js','policy-simulator.js','START-HERE.bat','local-server.ps1','README.md')
foreach ($name in $required) {
    if (-not (Test-Path -LiteralPath (Join-Path $root $name) -PathType Leaf)) { throw "Missing required file: $name" }
}

$constants = Get-Content -LiteralPath (Join-Path $root 'policy-constants.js') -Raw
$script = Get-Content -LiteralPath (Join-Path $root 'policy-simulator.js') -Raw
$operationCount = ([regex]::Matches($constants, '(?m)^\s*\["[a-z][a-z-]+","(?:GET|POST|PUT|PATCH|DELETE)"')).Count
if ($operationCount -ne 37) { throw "Expected 37 API operations; found $operationCount" }

$ruleCodes = @('ELIGIBILITY','ENTITLEMENT','ACCRUAL','CARRY_FORWARD','SANDWICH','LIMIT','ATTENDANCE_CHANNEL','LATE_PENALTY','OVERTIME','APPROVAL','REIMBURSEMENT','CALENDAR','CUSTOM')
foreach ($code in $ruleCodes) {
    if ($constants -notmatch "(?m)^\s*$code\s*:") { throw "Missing rule template: $code" }
}

foreach ($table in @('Policy','PolicyVersion','PolicyRule','PolicyApplicability','PolicyAssignment','PolicyException','PolicyDocument','PolicyApprovalStage','PolicyApprovalHistory','PolicyAcknowledgement','PolicyChangeAudit','BulkImportJob')) {
    if ($constants -notmatch [regex]::Escape($table)) { throw "Missing table mapping: $table" }
}

if ($script -match '(?i)[\{,]\s*(token|authorization)\s*:') { throw 'Token/authorization property appears in a persisted/exported object.' }
if ($script -notmatch 'Navigation/my-menu' -and $constants -notmatch 'Navigation/my-menu') { throw 'Dynamic permission discovery route is missing.' }

Write-Host "PASS: $operationCount endpoints, $($ruleCodes.Count) rule families, required files and table mappings verified." -ForegroundColor Green
Write-Host 'PASS: no token/authorization persistence pattern found.' -ForegroundColor Green
