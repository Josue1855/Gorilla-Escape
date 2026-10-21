$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$java = Join-Path $root "Runtime\bin\java.exe"
$jar = Join-Path $root "Host\local-server.jar"
$backendJar = Join-Path $root "Backend\backend.jar"
$unity = Join-Path $root "Unity\GorillaEscape.exe"
foreach ($file in @($java, $jar, $backendJar, $unity)) {
    if (-not (Test-Path -LiteralPath $file -PathType Leaf)) {
        throw "Missing mandatory bundle component: $file"
    }
}
# Product menu owns the lifecycle of the Java host (not the legacy IPC probe).\n$env:GORILLA_MAIN_MENU = '1'\n# Required by JavaProbeSupervisor: full absolute paths to exact packaged Java/JAR.
$env:GORILLA_IPC_JAVA = (Resolve-Path -LiteralPath $java).Path
$env:GORILLA_IPC_JAR = (Resolve-Path -LiteralPath $jar).Path

# Backend for human player persistence. Unity owns the IPC Java host lifecycle.
$backendArgs = @(
    '-jar', ('"' + $backendJar + '"'),
    '--server.address=127.0.0.1',
    '--server.port=8090'
)
$backendProcess = $null
try {
    $backendProcess = Start-Process -FilePath $java -ArgumentList $backendArgs -WorkingDirectory $root -WindowStyle Hidden -PassThru
    Write-Host "Backend SQLite PID: $($backendProcess.Id)"
    # Launch as a child and wait for exit. Unity decides when to manage its own Java host.
    $unityProcess = Start-Process -FilePath $unity -WorkingDirectory (Split-Path -Parent $unity) -Wait -PassThru
    if ($null -ne $unityProcess.ExitCode) { exit $unityProcess.ExitCode }
} finally {
    if ($backendProcess -and -not $backendProcess.HasExited) {
        Stop-Process -Id $backendProcess.Id -ErrorAction SilentlyContinue
        Write-Host "Backend SQLite stopped."
    }
}
