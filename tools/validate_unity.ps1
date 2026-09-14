param(
    [string]$UnityEditor = $env:UNITY_EDITOR,
    [string]$OutputDirectory = $env:IRON_SAND_VALIDATION_OUT,
    [string]$BuildTarget = $(if ($env:IRON_SAND_BUILD_TARGET) { $env:IRON_SAND_BUILD_TARGET } else { "StandaloneWindows64" }),
    [switch]$AllowDirty
)
$ErrorActionPreference = "Stop"
$Arguments = @((Join-Path $PSScriptRoot "run_unity_validation.py"), "--target", $BuildTarget)
if ($UnityEditor) { $Arguments += @("--unity", $UnityEditor) }
if ($OutputDirectory) { $Arguments += @("--out", $OutputDirectory) }
if ($AllowDirty) { $Arguments += "--allow-dirty" }
# Python is only the coordinator. It never fabricates Unity results.
if (Get-Command py -ErrorAction SilentlyContinue) { & py -3 @Arguments }
elseif (Get-Command python -ErrorAction SilentlyContinue) { & python @Arguments }
else { throw "Install Python 3.10+ to use the isolated validation runner." }
exit $LASTEXITCODE
