# State-integrity fixpack upload reconciliation

Date: 2026-09-14. Target: PR #1 / `feat/arena-prototype-v0.1.0`.
Inspected parent: `efc608aa87586e31b4d6f3953948a88d86ed6ec5`.
Original fixpack baseline: `3389e15db25441239fc2b24789d5dabce68415ce`.

## Why this is a compatibility port

The retry found that the branch had advanced to Combat V2 with Poise, executions, thrown weapons, an animated graybox rig, crowd gift queues and telemetry. Blindly applying the old 16-file proposal would remove new APIs and break callers. This change preserves those implementations and the existing CI/database pipeline. It is NOT an unmodified upload of the old ZIP.

Existing source already includes single-claim pickup handling, center-distance/occlusion checks, zero-durability rejection, visual reuse on normal durability loss, overflow-safe crowd favor with reset-generation checks, and the fatal-state latch before health/death notifications. These existing behaviors are retained, not claimed as new fixes or new engine validation.

## Changes in this commit

- `Assets/Scripts/Player/PlayerWeaponController.cs`: conserve the old weapon when the requested drop path is blocked, using the already-reachable pickup position; reject non-finite pickup inputs and invalid equipment; deactivate obsolete visuals immediately and use edit/runtime-appropriate cleanup. Preserve `TryThrow`, rig visual parenting, telemetry and all public V2 loadout events.
- `Assets/Scripts/Combat/WeaponPickup.cs`: expose availability, reject inactive/consumed/invalid claims, reject invalid weapon IDs and negative durability other than the explicit `-1` fresh sentinel. Keep the V2 `TryClaim` contract.
- `Assets/Scripts/Combat/WeaponVisualFactory.cs`: reject invalid weapon types and clean up transient colliders in both EditMode and PlayMode.
- `Assets/Scripts/Combat/Combatant.cs`: sanitize serialized health/Poise/timing inputs, latch an initially exhausted health state and reject non-finite knockback before damage callbacks. Keep V2 impact, Poise, execution, hit-stop and telemetry behavior.
- `Assets/Tests/PlayMode/ArenaHealthProbe.cs`: test-only damage/death callback counters.
- `Assets/Tests/PlayMode/ArenaStateIntegrityTests.cs`: port the original 12 parameter-expanded cases from `TryConsume` to V2 `TryClaim`; add 6 cases for blocked-drop conservation, invalid weapon/sentinel handling, inactive claims and three non-finite knockback inputs. Total: 18 authored cases.
- `docs/STATE_INTEGRITY_UPLOAD.md`: this reconciliation and verification boundary.

## Disposition of the original proposal

| Original proposal path/group | Upload decision |
|---|---|
| Combatant, WeaponPickup, PlayerWeaponController | Port only compatible missing safeguards; never replace their V2 APIs with the old versions. |
| WeaponVisualFactory | Apply the compatible factory safeguards. |
| ArenaHealthProbe, ArenaStateIntegrityTests | Adapt and expand to 18 PlayMode cases. |
| CrowdFavorSystem | Retain the existing V2-compatible implementation, including its public reward events. |
| CrowdFavorModel, WeaponLoadout | Do not introduce parallel unused state models. Existing runtime adapters remain authoritative. |
| tools/RulesChecks/* (four files) | Do not install the old 26-case runner linked to those unused models. Existing CombatRulesHarness remains unchanged; its coverage is not represented as equivalent to the omitted suite. |
| .github/workflows/repository-guard.yml, .gitignore | Preserve newer CI/telemetry and ignore rules. |
| docs/STATE_INTEGRITY_AUDIT.md | Replace the obsolete proposed-only narrative with this current-head reconciliation. |

## Verification boundary

- Four modified source baselines were reconstructed byte-for-byte and matched their GitHub Git-blob SHA before editing.
- New code was checked for patch whitespace and lexical/delimiter integrity. These are not compiler checks.
- GitHub CI must be read on the resulting commit, not inferred from the earlier green head.
- No .NET SDK or Unity Editor was executed in the assistant container. The 18 PlayMode cases are authored, **NOT_RUN**.
- Existing Repository Guard / CombatRulesHarness / telemetry CI does not compile these Unity adapters or execute their PlayMode tests. A green CI result cannot promote Unity gates.
- U1-U7 remain **NOT_RUN**. No engine-generated .meta, package lock, scene, settings, screenshots or PASS evidence is fabricated.

Run all existing Unity suites plus `ArenaStateIntegrityTests` under Unity `6000.3.23f1`. Follow `docs/LOCAL_VALIDATION_WORKPACK.md` and return the exact tested commit, real XML/logs and manual pickup/drop/throw/execution/restart observations. Preserve local work; no reset/clean/force push. This port changes scripts/tests only; Combat V2's existing generated-scene rebuild requirement remains applicable when updating from an older generated scene.

Known verification gaps remain: complete Unity compilation, physical collision behavior, saved-scene import, post-restart movement, camera/AI/feel, standalone and soak/performance. This compatibility upload is not release or merge approval. Keep PR #1 Draft and main unchanged.
