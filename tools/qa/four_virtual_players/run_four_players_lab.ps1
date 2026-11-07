# Gorilla Escape 1.0 — Laboratorio Maestro E2E Adversarial
# Punto de entrada propuesto: tools/qa/four_virtual_players/run_four_players_lab.ps1

[CmdletBinding()]
param(
    [string]$Phase = "All",
    [string]$LabId = ""
)

$ErrorActionPreference = "Continue"
$Root = Split-Path -Parent (Split-Path -Parent (Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)))
$RunnerPy = Join-Path $MyInvocation.MyCommand.Path "..\four_players_master_runner.py"

Write-Output "=========================================================================="
Write-Output "=== GORILLA ESCAPE 1.0: LABORATORIO MAESTRO E2E ADVERSARIAL (4 PLAYERS) ==="
Write-Output "=========================================================================="
Write-Output "Repo Root: $Root"
Write-Output "Fase solicitada: $Phase"

$python = "python"
& $python $RunnerPy
$exitCode = $LASTEXITCODE

Write-Output "Laboratorio Maestro finalizado con código de salida: $exitCode"
exit $exitCode
