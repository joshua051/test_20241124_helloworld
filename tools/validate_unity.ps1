param(
    [string]$UnityEditor = $env:UNITY_EDITOR,
    [string]$OutputDirectory = $env:IRON_SAND_VALIDATION_OUT,
    [string]$BuildTarget = $(if ($env:IRON_SAND_BUILD_TARGET) { $env:IRON_SAND_BUILD_TARGET } else { "StandaloneWindows64" })
)
$ErrorActionPreference = "Stop"
$Root = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
if ([string]::IsNullOrWhiteSpace($UnityEditor)) {
    throw "Set UNITY_EDITOR or pass -UnityEditor with the Unity 6000.3.23f1 executable path."
}
if (-not (Test-Path $UnityEditor)) { throw "Unity executable not found: $UnityEditor" }
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) { $OutputDirectory = Join-Path $Root "ValidationArtifacts" }
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
try { $Commit = (git -C $Root rev-parse HEAD).Trim() } catch { $Commit = "unknown" }
Set-Content -Path (Join-Path $OutputDirectory "commit.txt") -Value $Commit
Set-Content -Path (Join-Path $OutputDirectory "unity-editor-path.txt") -Value $UnityEditor
Set-Content -Path (Join-Path $OutputDirectory "build-target.txt") -Value $BuildTarget

function Invoke-Unity([string]$Label, [string[]]$Arguments) {
    Write-Host "== $Label =="
    & $UnityEditor -batchmode -nographics -projectPath $Root @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Label failed with exit code $LASTEXITCODE" }
}

Invoke-Unity "Rebuild generated arena" @(
    "-executeMethod", "IronSand.Editor.PrototypeBuilder.BuildForValidation",
    "-logFile", (Join-Path $OutputDirectory "01-scene-build.log"), "-quit"
)
Invoke-Unity "EditMode tests" @(
    "-runTests", "-testPlatform", "EditMode",
    "-testResults", (Join-Path $OutputDirectory "02-editmode-results.xml"),
    "-logFile", (Join-Path $OutputDirectory "02-editmode.log")
)
Invoke-Unity "PlayMode tests" @(
    "-runTests", "-testPlatform", "PlayMode",
    "-testResults", (Join-Path $OutputDirectory "03-playmode-results.xml"),
    "-logFile", (Join-Path $OutputDirectory "03-playmode.log")
)
Invoke-Unity "Development standalone build ($BuildTarget)" @(
    "-buildTarget", $BuildTarget,
    "-executeMethod", "IronSand.Editor.ValidationBuild.BuildCurrentTargetForValidation",
    "-logFile", (Join-Path $OutputDirectory "04-standalone-build.log"), "-quit"
)
Get-ChildItem -Path (Join-Path $Root "Assets") -Filter *.meta -Recurse | ForEach-Object FullName | Sort-Object | Set-Content (Join-Path $OutputDirectory "generated-meta-files.txt")
$PackageLock = Join-Path $Root "Packages/packages-lock.json"
if (Test-Path $PackageLock) { Copy-Item $PackageLock (Join-Path $OutputDirectory "packages-lock.json") -Force }
Copy-Item (Join-Path $Root "ProjectSettings/ProjectVersion.txt") (Join-Path $OutputDirectory "ProjectVersion.txt") -Force
Write-Host "Validation batch completed: $OutputDirectory"
Write-Host "Manual combat-feel and standalone soak gates remain; see docs/VALIDATION.md."
