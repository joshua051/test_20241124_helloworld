# Quality Gates

Iron Sand Arena uses evidence-based gates. Later functionality cannot erase a failure at an earlier gate.

## R0 — Repository integrity

Required:
- no generated Unity directories tracked
- no case-colliding paths or merge markers
- Unity version pinned to `6000.3.23f1`
- required docs/tests present
- no unexplained oversized tracked files

## U1 — Unity import

Evidence: project opens in Unity `6000.3.23f1`, packages resolve, genuine `.meta`, `Packages/packages-lock.json`, generated scene and relevant `ProjectSettings` changes are captured.

Status: **NOT_RUN** — real engine evidence required.

## U2 — Compile

Evidence: zero C# compile errors, zero package/compiler exceptions, arena rebuild succeeds.

Status: **NOT_RUN** — real engine evidence required.

## U3 — Automated rules and EditMode

All existing and new EditMode tests must pass, including ImportedGladiatorTests and Crowd Favor, WeaponCatalog/state integrity, StyleScoreModel, DodgeState, AttackTimeline, GuardState, PoiseState, ExecutionState and arena geometry/camera regression tests.

Status: **NOT_RUN** — real engine evidence required.

## U4 — PlayMode lifecycle

Run ArenaSmokeTests, ArenaStateIntegrityTests and ArenaIntegrationTests. All waits must have real-time deadlines; a paused simulation must not hang the runner.

Required: generated scene loads; player/enemies grounded; procedural combat rigs exist; enemy pressure reaches a committed attack without deadlock; attack-token count remains bounded; Crowd threshold queues a reward and explicit Appeal launches the physical gift; defeat/restart resets state; no recurring exceptions over 10 minutes.

Status: **NOT_RUN** — real engine evidence required.

## U5 — Combat mechanics

Required manual evidence:
- Light/Heavy show Startup/Active/Recovery and cannot be infinitely cancelled
- melee hits only during Active frames and no repeat hit per target/attack
- weapon types differ in timing/reach/Poise
- hit stop, reaction, camera impulse and impact sound are readable
- directional guard fails against rear attacks
- fresh guard can Perfect Guard
- Perfect Guard counters and disarms
- Poise break is readable
- low-health Poise break exposes execution
- `F` execution kills once and returns control cleanly
- `G` throws weapon, can damage and leaves player Unarmed
- pickup/swap/durability/break/drop remains consistent

Status: **NOT_RUN** — real engine evidence required.

## U6 — Arena / Crowd / AI feel

Required: four enemy roles show distinct pressure patterns; attack concurrency stays fair; physical Crowd gifts are collectible; style rewards varied actions/throw/Perfect Guard/execution; three waves complete without soft-lock; camera remains readable.

Status: **NOT_RUN** — real engine evidence required.

## U7 — Standalone build

Required: clean standalone build; launch/restart outside Editor; 20-minute soak; frame-time/GC observations recorded.

Status: **NOT_RUN**

## Production fidelity gate

Combat V2 becomes a completed **graybox vertical slice** only after U1–U7 evidence. Production humanoid art, authored animation clips, final audio/VFX and gore/dismemberment presentation remain a separate content-production gate.

## Evidence scope

The isolated validation coordinator produces automatic stage receipts only. A successful build does not mark U7 PASS without graphical launch/restart and soak evidence. Model fallback does not satisfy asset readiness. Source CI and coordinator-fixture tests never mark Unity gates PASS. See `docs/INTEGRATION_PREFLIGHT_01.md`.
