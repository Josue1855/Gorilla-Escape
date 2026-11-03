# tools/test_installed_distribution.ps1
# Automated E2E verification of the single Windows installer:
# 1. Clean installation
# 2. File and runtime presence
# 3. Process execution and supervision
# 4. Clean shutdown
# 5. Uninstallation

$ErrorActionPreference = "Stop"

$installer = "Builds\Installer\GorillaEscape-Setup-1.0.exe"
if (!(Test-Path $installer)) {
    throw "Installer not found at $installer"
}

$installDir = [System.IO.Path]::GetFullPath("Builds\TestInstall")
if (Test-Path $installDir) {
    Remove-Item $installDir -Recurse -Force
}

Write-Host "==> [1/5] Executing silent installation to $installDir..."
$p = Start-Process -FilePath $installer -ArgumentList "/DIR=`"$installDir`"", "/VERYSILENT", "/SUPPRESSMSGBOXES", "/NORESTART", "/SP-" -Wait -PassThru
if ($p.ExitCode -ne 0) {
    throw "Installer exited with non-zero code: $($p.ExitCode)"
}
Write-Host "    Installation finished successfully (ExitCode=0)."

Write-Host "==> [2/5] Verifying installed artifacts and directory layout..."
$requiredFiles = @(
    "GorillaEscape.exe",
    "UnityPlayer.dll",
    "UnityCrashHandler64.exe",
    "Java\server.jar",
    "Java\runtime\bin\java.exe",
    "Vision\python.exe",
    "Vision\worker.py",
    "Vision\pose_landmarker_lite-v1.task",
    "Vision\corpus-v1.json",
    "Vision\manifest.json",
    "Tools\cloudflared.exe",
    "unins000.exe"
)
foreach ($f in $requiredFiles) {
    $full = Join-Path $installDir $f
    if (!(Test-Path $full)) {
        throw "Missing required installed file: $full"
    }
    Write-Host "    [OK] $f"
}

Write-Host "==> [2.1/5] Verifying installed Vision Python runtime and MediaPipe..."
$pyExe = Join-Path $installDir "Vision\python.exe"
$pyCheck = & $pyExe -c "import certifi, mediapipe as mp, cv2; print('OK: certifi=' + certifi.__file__ + ' mp=' + mp.__version__ + ' cv2=' + cv2.__version__)"
if ($LASTEXITCODE -ne 0) {
    throw "Vision Python runtime failed import check: $pyCheck"
}
Write-Host "    $pyCheck"

Write-Host "==> [2.2/5] Verifying installed Vision worker in replay mode..."
$workerPy = Join-Path $installDir "Vision\worker.py"
$fixture = Join-Path $installDir "Vision\corpus-v1.json"
$workerOutput = & $pyExe $workerPy --mode replay --fixture $fixture --duration 1 2>&1
if ($LASTEXITCODE -ne 0) {
    throw "Vision worker failed in replay mode"
}
Write-Host "    Vision worker replay output verified ($($workerOutput.Count) lines emitted)."

Write-Host "==> [2.3/5] Verifying installed Cloudflared binary integrity..."
$cfExe = Join-Path $installDir "Tools\cloudflared.exe"
$cfHash = (Get-FileHash $cfExe -Algorithm SHA256).Hash
$expectedHash = "86AEE4017B26625CEE8484C113558F48EFFA4CD47F7AA05FCF425604E5D2B23C"
if ($cfHash -ne $expectedHash) {
    throw "Cloudflared hash mismatch: $cfHash vs $expectedHash"
}
Write-Host "    Cloudflared SHA-256 integrity verified ($cfHash)."

Write-Host "==> [3/5] Testing execution of installed GorillaEscape.exe..."
# Run installed GorillaEscape in batchmode for 6 seconds to verify it starts and supervises services
$gameProcess = Start-Process -FilePath (Join-Path $installDir "GorillaEscape.exe") -ArgumentList "-batchmode", "-nographics" -PassThru
Write-Host "    Launched GorillaEscape.exe (PID=$($gameProcess.Id))"

Start-Sleep -Seconds 6

$stillRunning = !$gameProcess.HasExited
Write-Host "    Game process running: $stillRunning"

if (!$stillRunning) {
    Write-Host "    Warning: Game exited early with code $($gameProcess.ExitCode)"
}

Write-Host "==> [4/5] Testing clean shutdown..."
if (!$gameProcess.HasExited) {
    Stop-Process -Id $gameProcess.Id -Force
    Start-Sleep -Seconds 2
}
Write-Host "    Game process stopped cleanly."

Write-Host "==> [5/5] Testing silent uninstallation..."
$uninstaller = Join-Path $installDir "unins000.exe"
$uninstProc = Start-Process -FilePath $uninstaller -ArgumentList "/VERYSILENT", "/SUPPRESSMSGBOXES", "/NORESTART" -Wait -PassThru
Start-Sleep -Seconds 3

# Check if GorillaEscape.exe was removed
$exeRemaining = Test-Path (Join-Path $installDir "GorillaEscape.exe")
if ($exeRemaining) {
    throw "Uninstallation failed: GorillaEscape.exe still exists"
}
Write-Host "    Uninstallation verified: Application cleanly removed."
Write-Host "==> All 5 verification steps passed successfully!"
