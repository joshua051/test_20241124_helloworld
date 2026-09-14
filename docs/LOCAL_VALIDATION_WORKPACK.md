# Isolated local Unity validation workpack

Requires Git, Python 3.10+ and an installed/licensed Unity `6000.3.23f1` with the desired desktop build module. No editor/license installation or credentials are automated. The coordinator never connects to a remote computer by itself.

## Windows

```powershell
$env:UNITY_EDITOR = "C:\Program Files\Unity\Hub\Editor\6000.3.23f1\Editor\Unity.exe"
.\tools\validate_unity.ps1
```

## macOS

```bash
export UNITY_EDITOR=/Applications/Unity/Hub/Editor/6000.3.23f1/Unity.app/Contents/MacOS/Unity
bash tools/validate_unity.sh
```

## Linux

```bash
export UNITY_EDITOR=/path/to/6000.3.23f1/Editor/Unity
bash tools/validate_unity.sh
```

Equivalent cross-platform command: `python tools/run_unity_validation.py --unity <editor> --target StandaloneWindows64`. Supported targets also include `StandaloneOSX` and `StandaloneLinux64`; a module not installed in Unity is a blocker, not a reason to silently change the target.

## Preservation and reproducibility

The runner records HEAD and worktree status. By default local changes block the run. Preserve/commit them first, or explicitly use `--allow-dirty` (PowerShell `-AllowDirty`) to acknowledge that ONLY committed HEAD is tested, not uncommitted work.

Every invocation has a NEW timestamp/UUID directory under `ValidationArtifacts/`. Git archives committed source into `source.zip`, then extracts a fresh `Project/` there. The Unity builder only replaces the generated arena in that disposable snapshot. The original scene, project settings, branches and untracked files are not reset, cleaned or overwritten.

Generated scenes, `.meta`, package lock, RuntimeMaterial and settings stay in that actual Unity-run snapshot for review. They are not manufactured or automatically pushed back. Keep the snapshot until evidence is reviewed; Library/Build output can be large.

## Stage contract

1. Require an existing editor executable and verify the actual `-version` output.
2. Run explicit scene/material preparation. Require the generated arena, runtime material and package lock.
3. Run EditMode without prematurely quitting. Parse its actual fresh NUnit XML; reject zero tests, failed/skipped/inconclusive cases, missing mandatory suites and count discrepancies.
4. Run PlayMode under the same requirements, including `ArenaSmokeTests`, `ArenaStateIntegrityTests` and `ArenaIntegrationTests`.
5. Build the ARENA scene (not unrelated enabled scenes) for the explicit target. Verify the real BuildReport receipt and nonempty output in the disposable project.

Every child has a timeout. Nonzero exits, timeouts and incomplete evidence fail the batch. Process logs and `summary.json` remain available even on failure. No old XML can be reused because each run has a fresh directory.

## Meaning of results

- `BLOCKED`: preflight did not permit a Unity batch, e.g. no editor, wrong version or dirty source. Read `summary.json`.
- `FAILED`: one or more started stages did not meet the evidence contract.
- `AUTOMATED_BATCH_PASS_MANUAL_NOT_RUN`: the automatic stages completed. This is NOT U5/U6 combat/feel acceptance or U7 standalone launch/soak acceptance.

`-nographics` verifies neither visible frame rendering nor shader appearance. Run the rebuilt snapshot in a graphical Unity session, confirm actual imported models rather than fallback, then complete `docs/VALIDATION.md`. U1-U7 numbering is defined ONLY by `docs/QUALITY_GATES.md`.

The coordinator's Python tests use synthetic XML/build fixtures and short Python subprocesses. Their PASS proves coordinator behavior, not Unity behavior. Pure C# SessionFlow checks likewise do not execute Unity APIs.
