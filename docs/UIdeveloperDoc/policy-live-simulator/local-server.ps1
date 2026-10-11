$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$port = 4200
$prefix = "http://localhost:$port/"
$listener = [System.Net.HttpListener]::new()
$listener.Prefixes.Add($prefix)

try {
    $listener.Start()
} catch {
    Write-Host "Port $port is already in use. Stop the existing Angular/local server and double-click START-HERE.bat again." -ForegroundColor Red
    exit 1
}

Start-Process $prefix
Write-Host "Skolenza Policy Flow Simulator is running at $prefix" -ForegroundColor Green
Write-Host "Keep this window open. Press Ctrl+C to stop." -ForegroundColor Yellow

$mime = @{
    '.html' = 'text/html; charset=utf-8'
    '.js'   = 'application/javascript; charset=utf-8'
    '.css'  = 'text/css; charset=utf-8'
    '.json' = 'application/json; charset=utf-8'
    '.md'   = 'text/plain; charset=utf-8'
}

try {
    while ($listener.IsListening) {
        $context = $listener.GetContext()
        $relative = [Uri]::UnescapeDataString($context.Request.Url.AbsolutePath.TrimStart('/'))
        if ([string]::IsNullOrWhiteSpace($relative)) { $relative = 'index.html' }
        $candidate = [IO.Path]::GetFullPath((Join-Path $root $relative))
        $rootFull = [IO.Path]::GetFullPath($root) + [IO.Path]::DirectorySeparatorChar
        if (-not $candidate.StartsWith($rootFull, [StringComparison]::OrdinalIgnoreCase) -or -not (Test-Path -LiteralPath $candidate -PathType Leaf)) {
            $context.Response.StatusCode = 404
            $bytes = [Text.Encoding]::UTF8.GetBytes('Not found')
        } else {
            $context.Response.StatusCode = 200
            $extension = [IO.Path]::GetExtension($candidate).ToLowerInvariant()
            $context.Response.ContentType = $mime[$extension]
            if (-not $context.Response.ContentType) { $context.Response.ContentType = 'application/octet-stream' }
            $bytes = [IO.File]::ReadAllBytes($candidate)
        }
        $context.Response.ContentLength64 = $bytes.Length
        $context.Response.OutputStream.Write($bytes, 0, $bytes.Length)
        $context.Response.OutputStream.Close()
    }
} finally {
    $listener.Stop()
    $listener.Close()
}
