# Iron Sand Arena

Original Unity 6 third-person gladiator-arena graybox. No Capcom assets, characters, story, audio, level data, or proprietary code are included.

**This is an unvalidated prototype, not a finished action game.** The player and enemies are primitive placeholders. Melee currently uses an immediate overlap query plus cooldown, not a humanoid attack animation or an animation-driven weapon sweep. Crowd rewards are threshold healing, not animated spectators throwing items.

## Current source scope

Movement/sprint, light/heavy damage, non-directional guard, timed dodge with cooldown, target lock, weapon pickups/swap/durability/drops, style/combo scoring, crowd favor, three waves, graybox world, debug HUD. The playability patch also adds flat arena collision, runtime camera rebinding and basic world obstruction, enemy gravity and HUD windup markers, pause/resume, focus-loss pause and scene restart.

These are **implemented in source, awaiting Unity validation**. See `docs/PLAYABILITY_AUDIT.md` for defects found in the preceding revision and exact patch scope.

## First run / update

1. Use branch `feat/arena-prototype-v0.1.0` of this repository while PR #1 is Draft. Preserve local edits before pulling; do not reset/clean them away.
2. Open the repository root in Unity **6000.3.23f1**. Let packages resolve and scripts compile.
3. Check **Edit > Project Settings > Player > Active Input Handling**: this graybox requires **Input Manager (Old)** or **Both**. Restart the Editor when requested. This is not a migration to the new Input System.
4. Exit Play Mode. Run **Tools > Iron Sand Arena > Rebuild Prototype Arena**. Save a copy first if you hand-edited the old generated scene. Rebuilding is mandatory for this patch: scripts alone do not repair an already-saved floor collider or add the new session component.
5. The builder saves `Assets/Scenes/ArenaPrototype.unity` and enables it in the Editor build scene list. If a custom Build Profile overrides that list, enable this scene there as well.
6. Run all EditMode tests, then the PlayMode `ArenaSmokeTests`, then the manual checklist in `docs/VALIDATION.md`.
7. Enter Play Mode, click Game view, and test the round. The updated HUD says `GRAYBOX / UNVALIDATED`.

No scene mutation is performed on domain load. A batch-only builder entry point is available as `IronSand.Editor.PrototypeBuilder.BuildForValidation`; run it only in a disposable validation checkout because it replaces the generated scene.

## Controls

| Input | Action |
|---|---|
| WASD / Shift | Move / sprint |
| Mouse | Orbit; adjust pitch when target-locked |
| Left / right mouse | Immediate prototype light / heavy hit |
| Q | Hold guard (not directional/perfect guard) |
| Space + direction | Timed dodge; no direction dodges backward |
| Tab | Lock / unlock a visible target |
| E | Swap nearest weapon |
| Esc | Pause / resume; restore cursor capture |
| R | Restart while paused, defeated or victorious |

Pause, Resume and Restart buttons are also available in the overlay. Attack, guard, pickup and dodge are mutually gated; spamming Space does not bypass the cooldown. The HUD `ATTACK INCOMING` marker is a debug telegraph, not a substitute for production animation.

## Not implemented

Humanoid rigs, root-motion/attack/reaction animation, animation-event weapon sweeps, directional/perfect guard, shields, disarm, thrown weapons, executions, audience actors/audio/item throws, production UI/VFX/art, NavMesh obstacle routing, authored enemy archetypes/bosses, gamepad, save/progression/career, full accessibility settings.

World occlusion and steering remain prototype implementations, not a production camera/AI solution. Broad-phase physics queries allocate; performance targets and long-session stability remain unverified.

## Validation

Repository Guard is a source-hygiene check, not a C# compiler or Unity test. No Unity PASS is asserted for this patch. Merge remains blocked on actual import/compile, EditMode, PlayMode, manual controls and standalone-build evidence.

Return the exact tested commit, Unity version, Console/Editor logs, Test Runner XML, short gameplay capture, and real Unity-generated `.meta`, `Packages/packages-lock.json`, relevant `ProjectSettings`, and the generated scene. Never invent those files or test results.

Architecture: `docs/ARCHITECTURE.md`; policy: `docs/QUALITY_GATES.md`; current defects/fixes: `docs/PLAYABILITY_AUDIT.md`; acceptance: `docs/VALIDATION.md`.
