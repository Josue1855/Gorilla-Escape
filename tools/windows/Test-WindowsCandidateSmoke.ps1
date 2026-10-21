param(
    [Parameter(Mandatory = $true)]
    [string] $PackageRoot,
    [int] $StartupSeconds = 75
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath $PackageRoot).Path
$java = Join-Path $root 'Runtime/bin/java.exe'
$hostJar = Join-Path $root 'Host/local-server.jar'
$backendJar = Join-Path $root 'Backend/backend.jar'
$unity = Join-Path $root 'Unity/GorillaEscape.exe'
foreach ($path in @($java, $hostJar, $backendJar, $unity)) {
    if (-not (Test-Path -LiteralPath $path)) { throw "Missing package component: $path" }
}
$logs = Join-Path $PWD 'candidate-smoke-evidence'
New-Item -ItemType Directory -Force $logs | Out-Null
$unityLog = Join-Path $logs 'unity.log'
$backendOutput = Join-Path $logs 'backend.out.log'
$backendError = Join-Path $logs 'backend.err.log'
$env:GORILLA_MAIN_MENU = '1'
$env:GORILLA_MOBILE_LAB = '1'
$env:GORILLA_IPC_JAVA = $java
$env:GORILLA_IPC_JAR = $hostJar
$env:GORILLA_QA_MENU = '1'
$env:GORILLA_QA_AUTOSTART_TESTS = '0'
$env:GORILLA_QA_AUTOSTART_PHONE = '0'
$backendData = Join-Path $logs 'sqlite'
$backendProcess = $null
$unityProcess = $null
$record = @{
    test = 'Real Windows Unity Player -> owned Java IPC -> embedded PWA HTTP + backend SQLite'
    unity_commit = $env:GITHUB_SHA
    scope = 'Windows hosted CI; no physical mobile device; no HTTPS/LAN/WebRTC certification'
    passed = $false
}
try {
    $backendArgs = '-jar "' + $backendJar + '" --server.address=127.0.0.1 --server.port=8090 --gorilla.storage.directory="' + $backendData + '"'
    $backendProcess = Start-Process -FilePath $java -ArgumentList $backendArgs -WorkingDirectory $root -PassThru -WindowStyle Hidden -RedirectStandardOutput $backendOutput -RedirectStandardError $backendError
    $deadline = (Get-Date).AddSeconds($StartupSeconds)
    $backendReady = $false
    while ((Get-Date) -lt $deadline) {
        if ($backendProcess.HasExited) { throw 'SQLite Backend exited during smoke' }
        try {
            $players = Invoke-RestMethod 'http://127.0.0.1:8090/api/players' -TimeoutSec 3
            if ($null -ne $players.items) { $backendReady = $true; break }
        } catch { Start-Sleep -Milliseconds 500 }
    }
    if (-not $backendReady) { throw 'SQLite Backend HTTP readiness not confirmed' }
    $record.backend_sqlite_http = 'GET 200'

    $args = '-batchmode -nographics -logFile "' + $unityLog + '"'
    $unityProcess = Start-Process -FilePath $unity -ArgumentList $args -PassThru -WorkingDirectory (Split-Path -Parent $unity) -WindowStyle Hidden
    $ready = $false
    $httpPort = $null
    $deadline = (Get-Date).AddSeconds($StartupSeconds)
    while ((Get-Date) -lt $deadline) {
        if (Test-Path $unityLog) {
            $log = Get-Content -LiteralPath $unityLog -Raw -ErrorAction SilentlyContinue
            if ($log -and $log.Contains('"event":"FIRST_PONG"')) { $ready = $true }
            if ($log -and $log -match 'QA_LAB_HTTP_READY\s+(\d{2,5})') {
                $httpPort = [int]$Matches[1]
            }
            if ($ready -and $httpPort) { break }
        }
        if ($unityProcess.HasExited) { throw "Unity Player exited (code $($unityProcess.ExitCode)) before IPC ready" }
        Start-Sleep -Seconds 1
    }
    if (-not $ready) { throw 'Unity/Java authenticated IPC FIRST_PONG was not observed' }
    if (-not $httpPort) { throw 'Unity did not publish Java host HTTP port' }
    $record.ipc_first_pong = $true
    $record.http_port = $httpPort

    $response = Invoke-WebRequest ("http://127.0.0.1:" + $httpPort + "/") -TimeoutSec 8 -UseBasicParsing
    if ($response.StatusCode -ne 200 -or $response.Content -notmatch '<html|<!doctype') {
        throw "Embedded PWA shell was not served from the managed Java host"
    }
    $record.embedded_pwa_http = 'GET 200 / HTML from managed Java host'
    $record.passed = $true
} finally {
    if ($unityProcess -and -not $unityProcess.HasExited) {
        Stop-Process -Id $unityProcess.Id -Force -ErrorAction SilentlyContinue
    }
    if ($backendProcess -and -not $backendProcess.HasExited) {
        Stop-Process -Id $backendProcess.Id -Force -ErrorAction SilentlyContinue
    }
    $record | ConvertTo-Json -Depth 5 | Set-Content (Join-Path $logs 'results.json') -Encoding utf8
}
if (-not $record.passed) { throw 'Smoke failed' }
Write-Host 'Unity -> Java managed IPC -> PWA shell and Backend SQLite: PASS (Windows CI)'
