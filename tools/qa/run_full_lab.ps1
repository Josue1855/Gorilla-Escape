# Gorilla Escape — Laboratorio de calidad reproducible (Quality Gate)
#
# Uso:
#   pwsh -File tools/qa/run_full_lab.ps1 [-Suites inventory,compile,pwa,flutter,java,python,unity,...]
#
# Cada ejecución escribe resultados en una carpeta única (no sobrescribe evidencia):
#   Reports/QA/LAB-<yyyyMMdd-HHmmss>/
#
# Principios:
#   * Nunca sustituye hardware físico por mocks; esas suites quedan BLOCKED/NOT RUN.
#   * Separa laboratorios simulados de pruebas públicas reales.
#   * No desactiva validaciones (engine-strict, openssl, licencia) para obtener verde.
#   * Cada suite devuelve PASS / FAIL / BLOCKED / NOT RUN / N-A con un motivo.

[CmdletBinding()]
param(
    [string[]]$Suites = @(),
    [switch]$SkipExpensive
)

$ErrorActionPreference = 'Continue'
$Root = (Resolve-Path "$PSScriptRoot\..\..").Path   # repo root
$LabId = 'LAB-' + (Get-Date -Format 'yyyyMMdd-HHmmss')
$Out = Join-Path $Root "Reports/QA/$LabId"
$Logs = Join-Path $Out 'logs'
New-Item -ItemType Directory -Path $Logs -Force | Out-Null

# --- Entorno detectado (reproducible) ----------------------------------------
$nodeDs = 'C:\Users\equipo\.dsh\dsh-runtimes\dsh-primary-runtime\dependencies\node\bin\node.exe'
$npmCli = 'C:\Program Files\nodejs\node_modules\npm\bin\npm-cli.js'
$gitUsrBin = 'C:\Program Files\Git\usr\bin'
$flutter = 'C:\Users\equipo\AppData\Local\Programs\Flutter\bin\flutter.bat'
$javaHome = $env:JAVA_HOME
$unity = 'C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe'
$python = 'python'

# Los descubrimientos del entorno que hacen verdes las suites correctas:
#  - PWA usa Node 24 (DSH) porque engine-strict=true rechaza Node 26.
#  - Java usa openssl de Git (ausente en PATH nativo de Windows).
#  - Unity se lanza con Start-Process -Wait (Unity.exe es app GUI; `&` no espera).

function Test-Command([string]$cmd, [string]$args) {
    try { $null = & $cmd $args 2>$null; return $true } catch { return $false }
}

$results = New-Object System.Collections.Generic.List[object]

function Add-Result([string]$suite, [string]$check, [string]$status, [string]$detail, [string]$evidence) {
    $results.Add([pscustomobject]@{
        suite = $suite; check = $check; status = $status; detail = $detail;
        evidence = $evidence; at = (Get-Date -Format 'o')
    })
    Write-Output ("[{0}] {1,-10} {2,-40} {3}" -f $status, $suite, $check, $detail)
}

# --- Suites rápidas y deterministas (siempre) --------------------------------
Write-Output "== Inventario y validación documental =="
$mgmtLog = Join-Path $Logs 'validate_management.log'
$mgmt = & $python (Join-Path $Root 'tools/github/validate_management.py') 2>&1
$mgmtCode = $LASTEXITCODE
$mgmt | Out-File $mgmtLog -Encoding utf8
Add-Result 'inventory' 'validate_management.py' $(if ($mgmtCode -eq 0) {'PASS'} else {'FAIL'}) ($mgmt | Select-Object -Last 1) $mgmtLog

$foundLog = Join-Path $Logs 'validate_foundation.log'
$found = & $python (Join-Path $Root 'tools/validate_foundation.py') 2>&1
$foundCode = $LASTEXITCODE
$found | Out-File $foundLog -Encoding utf8
Add-Result 'inventory' 'validate_foundation.py' $(if ($foundCode -eq 0) {'PASS'} else {'FAIL'}) ($found | Select-Object -Last 1) $foundLog

$regLog = Join-Path $Logs 'validate_management_regression.log'
$reg = & $python -m unittest tools.github.test_validate_management -v 2>&1
$regCode = $LASTEXITCODE
$reg | Out-File $regLog -Encoding utf8
Add-Result 'inventory' 'validate_management regression' $(if ($regCode -eq 0) {'PASS'} else {'FAIL'}) ($reg | Select-String 'Ran ') $regLog

# --- PWA (Node 24 para respetar engine-strict) -------------------------------
Write-Output "== PWA (Node 24) =="
if (Test-Path $nodeDs) {
    $pwaLog = Join-Path $Logs 'pwa.log'
    $oldPath = $env:PATH
    $env:PATH = (Split-Path $nodeDs) + ';' + $oldPath
    Push-Location (Join-Path $Root 'PWA')
    $pwaOut = & $nodeDs $npmCli ci --no-audit --no-fund 2>&1; $ci = $LASTEXITCODE
    $pwaOut += & $nodeDs $npmCli test 2>&1; $t = $LASTEXITCODE
    $pwaOut += & $nodeDs $npmCli run build 2>&1; $b = $LASTEXITCODE
    Pop-Location
    $env:PATH = $oldPath
    $pwaOut | Out-File $pwaLog -Encoding utf8
    $pwaOk = ($ci -eq 0 -and $t -eq 0 -and $b -eq 0)
    Add-Result 'pwa' 'npm ci+test+build' $(if ($pwaOk) {'PASS'} else {'FAIL'}) "ci=$ci test=$t build=$b" $pwaLog
} else {
    Add-Result 'pwa' 'npm ci+test+build' 'BLOCKED' 'Node 24 no disponible (DSH)' ''
}

# --- Flutter ----------------------------------------------------------------
Write-Output "== Flutter =="
if (Test-Path $flutter) {
    $flLog = Join-Path $Logs 'flutter.log'
    Push-Location (Join-Path $Root 'Companion')
    $flOut = & $flutter analyze 2>&1; $fa = $LASTEXITCODE
    $flOut += & $flutter test 2>&1; $ft = $LASTEXITCODE
    Pop-Location
    $flOut | Out-File $flLog -Encoding utf8
    Add-Result 'flutter' 'analyze + test' $(if ($fa -eq 0 -and $ft -eq 0) {'PASS'} else {'FAIL'}) "analyze=$fa test=$ft" $flLog
} else {
    Add-Result 'flutter' 'analyze + test' 'BLOCKED' 'Flutter no disponible' ''
}

# --- Java (openssl de Git para los tests TLS) --------------------------------
Write-Output "== Java (Server) =="
$mvnw = Join-Path $Root 'Server\mvnw.cmd'
if (Test-Path $mvnw) {
    $jLog = Join-Path $Logs 'server_mvn_verify.log'
    $oldPath = $env:PATH
    $env:PATH = $gitUsrBin + ';' + $oldPath
    if ($javaHome) { $env:JAVA_HOME = $javaHome }
    Push-Location (Join-Path $Root 'Server')
    $jOut = & $mvnw -B -f pom.xml verify 2>&1; $jc = $LASTEXITCODE
    Pop-Location
    $env:PATH = $oldPath
    $jOut | Out-File $jLog -Encoding utf8
    Add-Result 'java' 'mvnw verify' $(if ($jc -eq 0) {'PASS'} else {'FAIL'}) "exit=$jc" $jLog
} else {
    Add-Result 'java' 'mvnw verify' 'BLOCKED' 'mvnw.cmd ausente' ''
}

# --- Unity (build limpia, batchmode, con espera real) ------------------------
Write-Output "== Unity (build Windows) =="
if ((Test-Path $unity) -and (-not $SkipExpensive)) {
    $uLog = Join-Path $Logs 'unity_build.log'
    $uArgs = @('-batchmode','-quit','-projectPath',(Join-Path $Root 'Unity'),
        '-executeMethod','GorillaEscape.Editor.ProductPlayMode.BuildWindowsStandalone','-logFile',$uLog)
    $p = Start-Process -FilePath $unity -ArgumentList $uArgs -Wait -PassThru
    $uc = $p.ExitCode
    $ok = $uc -eq 0 -and (Select-String -Path $uLog -Pattern 'BUILD_WINDOWS_SUCCESS' -Quiet)
    Add-Result 'unity' 'build Windows standalone' $(if ($ok) {'PASS'} else {'FAIL'}) "exit=$uc" $uLog
} elseif ($SkipExpensive) {
    Add-Result 'unity' 'build Windows standalone' 'NOT RUN' 'skip-expensive' ''
} else {
    Add-Result 'unity' 'build Windows standalone' 'BLOCKED' 'Unity Editor 6000.6.0f1 no localizado' ''
}

# --- Cámara / MediaPipe (requiere hardware; nunca se simula) -----------------
Add-Result 'camera' 'MediaPipe pipeline' 'BLOCKED' 'Requiere cámara física y runtime Python 3.12 portátil; no se sustituye por replay' ''

# --- Cloudflare público (requiere credenciales/túnel real; nunca se simula) --
Add-Result 'cloudflare' 'túnel público + RoomRegistry' 'BLOCKED' 'Requiere túnel autenticado y credenciales de anfitrión; no se simula DNS' ''

# --- WebRTC multi-dispositivo / 4 jugadores físicos --------------------------
Add-Result 'webrtc' '4 clientes físicos' 'BLOCKED' 'Requiere 4 teléfonos físicos + personas reales' ''

# --- Persistir resultados ----------------------------------------------------
$summary = $results | Group-Object status | ForEach-Object { "$($_.Name)=$($_.Count)" }
$results | ConvertTo-Json -Depth 4 | Out-File (Join-Path $Out 'results.json') -Encoding utf8
Write-Output "=============================================="
Write-Output "LAB: $LabId  -> $Out"
Write-Output "RESUMEN: $($summary -join ', ')"
Write-Output "=============================================="
