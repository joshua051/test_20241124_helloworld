# Iron Sand Arena — Development Progress Log

This log is the durable human-readable development timeline for the current Combat Vertical Slice. Machine-readable progress is stored in `docs/PROJECT_STATUS.json`; validation truth is tracked by `docs/QUALITY_GATES.md`, `docs/VALIDATION.md`, PR #1, and Issue #2.

Rules:
- Do not mark Unity/standalone behavior PASS without real execution evidence.
- Engineering maturity percentages are estimates, not acceptance results.
- Record the exact commit for every major milestone, defect repair, validation change, or telemetry/database change.
- Keep PR #1 Draft until U1–U7 are evidenced and no P0/P1 blockers remain.

## 2026-09-14

### `77c3bf9` — Telemetry + local learning database
- Added machine-readable ten-dimension progress and R0/U1–U7 status snapshot.
- Added Unity append-only JSONL combat telemetry under `Application.persistentDataPath/IronSandTelemetry/`.
- Added local SQLite schema and CLI importer/query tool.
- Data model covers project snapshots, progress, gates, versioned balance parameters, combat sessions/events, test runs, defects, feel ratings, artifacts, and balance experiments.
- Runtime capture includes impact/Poise, death, wave lifecycle, attack-token lifecycle, weapon pickup/durability/break/throw, style/crowd events, Appeal and crowd gift launches.
- CI now creates SQLite, imports current status and queries latest progress/gates.
- GitHub Repository Guard + pure CombatRulesHarness + SQLite telemetry pipeline: PASS.
- Unity U1–U7: NOT_RUN.

### `45f9c28` — Reproducible local Unity validation workpack
- Added macOS/Linux and Windows batch validation entry points.
- Added explicit generated-scene rebuild, EditMode tests, PlayMode tests and desktop Development standalone build.
- Added evidence capture into `ValidationArtifacts/`.
- This automated the validation procedure but did not itself execute Unity in the assistant environment.

### `22a3d8ce` and follow-up Combat V2 commits — Combat Vertical Slice 2.0
- Replaced immediate cooldown melee with Startup / Active / Recovery attack timing.
- Added swept active-frame melee, input buffering and three-step chain tuning.
- Added Health + Poise, stagger, directional guard, Perfect Guard, counter-stagger and disarm.
- Added weapon throwing, contextual execution, hit stop, camera impulse, transient VFX and generated placeholder impact audio.
- Added procedural original graybox humanoid combat posing.
- Added Aggressor / Flanker / Brute / Skirmisher pressure roles with bounded attack tokens.
- Added Crowd reward queue + player `C` Appeal + physical food/weapon gifts.
- Pure C# combat-rule CI reached PASS after fixing harness compilation issues.

### `3389e15` — Playability/lifecycle repair
- Corrected arena floor dimensions/collision and wall alignment.
- Rebound camera state after scene reload and added basic obstruction handling.
- Reworked dodge from instant displacement into timed travel/recovery.
- Added pause/resume, focus-loss pause, restart and build-scene registration.
- Added enemy gravity, windup telegraphing, delayed wave transitions and regression coverage.
- Kept Unity execution status NOT_RUN pending real local evidence.

## Current status

- Active branch: `feat/arena-prototype-v0.1.0`
- Draft PR: #1 — Combat Vertical Slice 2.0
- Validation tracker: Issue #2 — U1–U7 local Unity validation
- Current source head at time of this log update: `77c3bf9f61f3eb0171047c68703cafa69f61031f`
- GitHub CI source/pure-rules/database pipeline: PASS
- U1 Unity import: NOT_RUN
- U2 Unity compile/scene rebuild: NOT_RUN
- U3 EditMode/PlayMode: NOT_RUN
- U4 mechanics validation: NOT_RUN
- U5 AI/Crowd/encounter validation: NOT_RUN
- U6 combat feel/camera/readability: NOT_RUN
- U7 standalone/soak: NOT_RUN

## Reporting policy

Future development updates should update at least one of the following in the same GitHub change set:
1. `docs/PROGRESS_LOG.md` for human-readable history.
2. `docs/PROJECT_STATUS.json` for chart/database ingestion.
3. PR #1 body/comment for review-facing milestone summaries.
4. Issue #2 for validation evidence and blockers.

A progress update that exists only in chat is not considered durable project documentation.
