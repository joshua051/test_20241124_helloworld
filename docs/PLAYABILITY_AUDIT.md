# Playability audit and repair scope

Date: 2026-09-14. Baseline inspected: `2c2465b789712e843a524700f1d05188181667e6`, PR #1, `feat/arena-prototype-v0.1.0`.

The user reports that an earlier local checkout starts but appears incomplete. No exact local commit, logs or screenshots were provided. That report is not treated as proof of the full validation matrix, and none of the defects below is claimed to have been reproduced in the user's specific environment.

## Source-confirmed problems and changes

| ID | Severity | Baseline problem | Patch |
|---|---|---|---|
| A01 | P0 | Cylinder scale 14 creates a visible radius of 7, while enemies spawn at radius 10. Its default collider is a capsule, not a flat disc. | Shared `ArenaGeometry` constants; 30-unit diameter; static non-convex MeshCollider with top at y=0. |
| A02 | P1 | Wall long axes follow radial spokes instead of the arena tangent. | Tangential orientation and calculated overlapping segment width. |
| A03 | P1 | Camera player cache is set only by editor-time SetTarget and is not serialized/rebuilt on load. | Awake/OnEnable cache rebuild plus a serialized-target regression test. |
| A04 | P1 | Dodge moves 4.5 units instantly and has no cooldown, allowing repeated invulnerability. | Pure timing model, per-frame travel, recovery gap, partial invulnerability and action gating. |
| A05 | P1 | Esc releases the cursor without a resume path, while combat input remains live. | Central pause/resume/focus handling; actor updates gated while paused. |
| A06 | P1 | No implemented restart flow or enabled scene registration. | R/buttons reload the saved scene; Builder preserves other build entries and enables this scene. |
| A07 | P1 | Enemy Move calls never apply gravity; orbit speed is normalized away. | Gravity in idle/stun/windup as well as movement, and actual orbit velocity. |
| A08 | P1 | Melee sphere is offset above a torso-centered root and queries can hit through scenery. | Torso-centered overlap and world line-of-sight checks for hits/target acquisition. Still not precise weapon hitboxes. |
| A09 | P2 | Final wave index can display 4/3; a new wave spawns inside the last enemy's death callback. | Clamped display and delayed, death-safe encounter transition. |
| A10 | P2 | IMGUI is not an explicit direct dependency; camera lacks AudioListener. | Explicit IMGUI/audio modules and one listener. No claim that the baseline necessarily failed compilation: transitive package resolution was not run. |
| A11 | P1 | Enemy windup exists as a timer but has no player-visible telegraph. | HUD warning/progress and committed attack direction. Production animation remains absent. |
| A12 | P2 | Dropped weapons use torso height plus an upward offset. | Drop at y=0.35 for this flat-floor arena. Arbitrary terrain placement is not implemented. |

## New regression coverage (authored, NOT_RUN in Unity)

`ArenaRegressionTests`: 10 EditMode cases including three frame-rate variants (30/60/144), cooldown/recovery/cancel/invalid-clock checks, floor raycasts across four rings, continuous radial wall coverage, camera serialized-reference rebinding, and solid-world occlusion.

`ArenaSmokeTests`: 2 PlayMode cases loading the actual rebuilt scene: grounded actors/one listener/camera binding, then defeat/restart/health/loadout/score/wave reset. The scene must first be generated and enabled in the active build list. These tests do not prove input feel, animation quality, sustained attack-token fairness, or performance.

## Explicitly unresolved / merge blockers

- No Unity Editor, C# compiler or player build was run in the assistant environment. Unity import/compile/tests/rendering/performance remain NOT_RUN for this patch.
- No formal Unity evidence for the earlier checkout is visible in PR comments at audit time.
- Existing generated scenes must be rebuilt. Preserve user-authored scene edits and local settings; do not blindly reset/clean a local worktree.
- `.meta`, package lock, material project settings, generated scene and actual test artifacts still require a genuine Unity import. Do not fabricate serialized assets or PASS results.
- Core melee remains an immediate sphere query with cooldown. Character/weapon attack animation, hit reactions, audiovisual impacts, weapon throwing, execution, audience throwing rewards, and production AI are not delivered by this repair.
- Camera casts and combat queries allocate; no GC/frame-time budget is proven. Camera obstruction is a basic graybox solver, not a production guarantee.
- New enemy HUD telegraphs improve debug readability only; they do not satisfy final animation/feel acceptance.

## Authoritative engine references

- Unity primitive dimensions and default cylinder collider: https://docs.unity.cn/Manual/PrimitiveObjects.html
- CharacterController.Move does not apply gravity: https://docs.unity3d.com/6000.3/Documentation/ScriptReference/CharacterController.Move.html
- IMGUI module: https://docs.unity3d.com/6000.3/Documentation/Manual/com.unity.modules.imgui.html
- Build scene registration: https://docs.unity3d.com/6000.3/Documentation/ScriptReference/EditorBuildSettings-scenes.html

Keep PR #1 Draft. Source fixes are not release approval.
