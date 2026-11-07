# Run once after cloning, from any working directory. No administrator needed.
[CmdletBinding()]
param([switch]$VerifyTunnel)
$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$pin = '86aee4017b26625cee8484c113558f48effa4cd47f7aa05fcf425604e5d2b23c'
$binary = Join-Path $PSScriptRoot 'cloudflared.exe'
Push-Location $repo
try {
    foreach ($command in @('git', 'node', 'npm.cmd', 'java', 'javac')) {
        if (!(Get-Command $command -ErrorAction SilentlyContinue)) {
            throw "Falta $command. Instala Git con LFS, Node 24 y JDK 21 o superior antes de continuar."
        }
    }
    if ((& node --version) -notmatch '^v24\.') { throw 'Se requiere Node 24.' }
    # npm.cmd can otherwise select a different node.exe next to itself, ignoring PATH.
    $npmCli = Join-Path (Split-Path (Get-Command npm.cmd).Source) 'node_modules/npm/bin/npm-cli.js'
    if (!(Test-Path -LiteralPath $npmCli)) { throw 'No se encontro npm-cli.js. Instala la distribucion estandar de Node 24 con npm.' }
    & git lfs version
    if ($LASTEXITCODE -ne 0) { throw 'Instala Git LFS antes de continuar.' }
    & git lfs pull
    if ($LASTEXITCODE -ne 0) { throw 'No se pudieron descargar los recursos LFS.' }
    if (!(Test-Path -LiteralPath $binary) -or (Get-FileHash -LiteralPath $binary -Algorithm SHA256).Hash -ne $pin) {
        $download = Join-Path $PSScriptRoot ('cloudflared-' + [guid]::NewGuid().ToString('N') + '.tmp')
        try {
            Invoke-WebRequest -Uri 'https://github.com/cloudflare/cloudflared/releases/download/2026.10.0/cloudflared-windows-amd64.exe' -OutFile $download
            if ((Get-FileHash -LiteralPath $download -Algorithm SHA256).Hash -ne $pin) { throw 'CLOUDFLARED_SHA_INVALID' }
            Move-Item -LiteralPath $download -Destination $binary -Force
        } finally {
            if (Test-Path -LiteralPath $download) { Remove-Item -LiteralPath $download }
        }
    }
    & $binary --version
    if ($LASTEXITCODE -ne 0) { throw 'Cloudflared no pudo ejecutarse.' }
    & node $npmCli --prefix PWA ci
    if ($LASTEXITCODE -ne 0) { throw 'Fallo instalando la PWA.' }
    & node $npmCli --prefix PWA run build
    if ($LASTEXITCODE -ne 0) { throw 'Fallo compilando la PWA.' }
    & .\Server\mvnw.cmd -B -f Server/pom.xml verify
    if ($LASTEXITCODE -ne 0) { throw 'Fallo compilando/verificando Server.' }
    if ($VerifyTunnel) {
        $saved = @{}
        $names = @('GORILLA_REAL_TUNNEL', 'GORILLA_TUNNEL_TOKEN', 'GORILLA_TUNNEL_ID', 'GORILLA_TUNNEL_CREDENTIALS', 'GORILLA_TUNNEL_DOMAIN')
        foreach ($name in $names) {
            $saved[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
            [Environment]::SetEnvironmentVariable($name, $null, 'Process')
        }
        try {
            $env:GORILLA_REAL_TUNNEL = $binary
            & .\Server\mvnw.cmd -B -f Server/pom.xml '-Dtest=RealQuickTunnelTest' test
            if ($LASTEXITCODE -ne 0) {
                $report = Join-Path $repo 'Server/target/surefire-reports/com.gorillaescape.server.connectivity.RealQuickTunnelTest.txt'
                if ((Test-Path -LiteralPath $report) -and (Select-String -LiteralPath $report -Pattern 'UnresolvedAddressException|UnknownHostException' -Quiet)) {
                    throw 'El DNS de esta red no resolvio el dominio temporal del tunel. Prueba en otra red o revisa el DNS con su administrador. No se verifico el acceso publico.'
                }
                throw 'El tunel HTTPS no paso la prueba externa en esta red. Revisa Server/target/surefire-reports.'
            }
        } finally {
            foreach ($name in $names) { [Environment]::SetEnvironmentVariable($name, $saved[$name], 'Process') }
        }
    }
    Write-Host 'Servidor, PWA, recursos y binario del tunel preparados. Abre Unity/ con 6000.6.0f1 y usa Gorilla Escape > Play Gorilla Escape.'
    Write-Warning 'La camara requiere su runtime Python y dependencias aparte. La prueba HTTPS no verifica webcam, sensores ni WebRTC con un telefono real.'
} finally { Pop-Location }
