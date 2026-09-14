# Local Unity Validation Workpack

Use this after checking out the exact Draft-PR commit. Inspect `git status` first because the arena builder intentionally replaces the generated `Assets/Scenes/ArenaPrototype.unity`.

## macOS

```bash
git status
git rev-parse HEAD
export UNITY_EDITOR=/Applications/Unity/Hub/Editor/6000.3.23f1/Unity.app/Contents/MacOS/Unity
./tools/validate_unity.sh
```

The script selects `StandaloneOSX` explicitly for the Development build.

## Linux

```bash
export UNITY_EDITOR=/path/to/6000.3.23f1/Editor/Unity
./tools/validate_unity.sh
```

The script selects `StandaloneLinux64`. Override with `IRON_SAND_BUILD_TARGET` only when intentionally validating another installed desktop module.

## Windows PowerShell

```powershell
git status
git rev-parse HEAD
$env:UNITY_EDITOR = "C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe"
.\tools\validate_unity.ps1
```

The PowerShell default is `StandaloneWindows64`; `-BuildTarget` can override it intentionally.

## What a zero exit code proves

1. Unity can import enough of the project to execute the explicit arena builder.
2. The generated arena rebuild succeeds in batch mode.
3. EditMode tests pass.
4. PlayMode smoke tests pass.
5. Unity creates a Development standalone build for the explicitly selected desktop target.

Evidence is written to `ValidationArtifacts/`: exact commit, Unity path, build target, builder log, EditMode XML/log, PlayMode XML/log, standalone-build log, generated `.meta` list, package lock when present and pinned ProjectVersion.

## What it does not prove

The batch does not prove combat feel, camera comfort, input latency, animation readability, long-session stability or subjective SoR-level fidelity. Complete the manual matrix in `docs/VALIDATION.md`, including gameplay capture and standalone soak.

If any command exits non-zero, preserve the exact log/result and fix that gate rather than blindly rerunning it.
