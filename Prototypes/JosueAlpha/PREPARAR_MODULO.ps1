$ErrorActionPreference = 'Stop'
$moduleRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$trackerRoot = Join-Path $moduleRoot 'CameraTracker'
$runtimeRoot = Join-Path $trackerRoot '.runtime\python'
$python = Join-Path $runtimeRoot 'python.exe'
$venvPython = Join-Path $trackerRoot '.venv\Scripts\python.exe'
$downloads = Join-Path $trackerRoot '.downloads'

New-Item -ItemType Directory -Force -Path $downloads | Out-Null

if (-not (Test-Path -LiteralPath $python)) {
    $installer = Join-Path $downloads 'python-3.12.10-amd64.exe'
    if (-not (Test-Path -LiteralPath $installer)) {
        Invoke-WebRequest -UseBasicParsing 'https://www.python.org/ftp/python/3.12.10/python-3.12.10-amd64.exe' -OutFile $installer
    }
    $install = Start-Process -FilePath $installer -ArgumentList @(
        '/quiet', 'InstallAllUsers=0', 'PrependPath=0', 'Include_test=0', 'Include_launcher=0',
        "TargetDir=`"$runtimeRoot`""
    ) -WindowStyle Hidden -PassThru -Wait
    if ($install.ExitCode -ne 0) { throw "Python no se pudo instalar. Código $($install.ExitCode)." }
}

if (-not (Test-Path -LiteralPath $venvPython)) {
    & $python -m venv (Join-Path $trackerRoot '.venv')
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo crear el entorno local de Python.' }
}
& $venvPython -m pip install --disable-pip-version-check --upgrade pip
if ($LASTEXITCODE -ne 0) { throw 'No se pudo actualizar pip.' }
& $venvPython -m pip install --disable-pip-version-check -r (Join-Path $trackerRoot 'requirements.txt')
if ($LASTEXITCODE -ne 0) { throw 'No se pudieron instalar MediaPipe y OpenCV.' }

$cloudflared = Join-Path $moduleRoot 'MotionRelay\cloudflared.exe'
if (-not (Test-Path -LiteralPath $cloudflared)) {
    Invoke-WebRequest -UseBasicParsing 'https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-windows-amd64.exe' -OutFile $cloudflared
}
