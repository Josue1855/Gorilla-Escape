$ErrorActionPreference = 'Continue'
$Root = (Get-Item .).FullName
$UnityExe = 'C:\Program Files\Unity\Hub\Editor\6000.6.0f1\Editor\Unity.exe'
$ProjectPath = Join-Path $Root 'Unity'
$LogFile = Join-Path $Root 'unity_build.log'

Write-Host "Project Path: $ProjectPath"
Write-Host "Log File: $LogFile"

# Ensure no Unity process is locking the project
Get-Process -Name "Unity" -ErrorAction SilentlyContinue | ForEach-Object {
    Write-Host "Stopping running Unity process $($_.Id)..."
    Stop-Process -Id $_.Id -Force
}

# Remove stale UnityLockfile if present
$LockFile = Join-Path $ProjectPath "Temp\UnityLockfile"
if (Test-Path $LockFile) {
    Write-Host "Removing stale UnityLockfile..."
    Remove-Item $LockFile -Force -ErrorAction SilentlyContinue
}

$uArgs = @(
    '-batchmode',
    '-quit',
    '-projectPath', $ProjectPath,
    '-executeMethod', 'GorillaEscape.Editor.ProductPlayMode.BuildWindowsStandalone',
    '-logFile', $LogFile
)

Write-Host "Starting Unity batchmode build..."
$p = Start-Process -FilePath $UnityExe -ArgumentList $uArgs -Wait -PassThru
Write-Host "Unity process exited with code: $($p.ExitCode)"

if (Test-Path $LogFile) {
    Write-Host "=== TAIL OF UNITY BUILD LOG ==="
    Get-Content $LogFile -Tail 40
} else {
    Write-Host "Log file not created!"
}
