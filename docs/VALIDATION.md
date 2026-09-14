# Validation Gate - playability repair

Repository files and source review do not prove Unity import, compilation or gameplay. Preserve the gate hierarchy in `docs/QUALITY_GATES.md`; use `docs/PLAYABILITY_AUDIT.md` as the current defect ledger.

## Import and scene preparation

- [ ] Record exact Git commit, Unity 6000.3.23f1, OS and hardware.
- [ ] Preserve local edits before pulling; do not use destructive reset/clean.
- [ ] Package resolution and compilation finish with zero errors.
- [ ] Active Input Handling is Input Manager (Old) or Both; apply requested Editor restart.
- [ ] Exit Play Mode and explicitly rebuild ArenaPrototype using the menu. Preserve a copy of hand-edited generated scenes.
- [ ] Scene contains ArenaSession, one enabled AudioListener, a 30-unit diameter floor with MeshCollider and tangential walls.
- [ ] ArenaPrototype is enabled in the active build scene list / Build Profile override.
- [ ] Capture genuine `.meta`, package lock, generated scene and relevant ProjectSettings changes.

## Automated Unity tests

- [ ] Existing CrowdFavorSystemTests, WeaponCatalogTests and StyleScoreModelTests pass.
- [ ] All 10 ArenaRegressionTests cases pass (including parameterized variants).
- [ ] Both ArenaSmokeTests PlayMode cases pass against the rebuilt scene.
- [ ] Save original Test Runner XML and Editor log with exact tested commit. No failures or unexpected exception logs.

## Manual controls and collision

- [ ] Player and all enemies stand on the same flat floor; no floating, sliding off a capsule surface or falling at spawn.
- [ ] Walk/sprint around the entire perimeter; walls have no escape gaps.
- [ ] Mouse orbit works; Tab locks/unlocks a valid target, strafing faces it, camera follows after scene reload.
- [ ] Camera avoids walls/pillars from several angles without severe clipping or jitter.
- [ ] Esc pauses combat and frees the cursor. Esc/Resume recaptures it; a resume click does not attack.
- [ ] Focus loss pauses. Alt-tab back allows explicit resume without a stuck cursor.
- [ ] WASD/Shift work in both Editor and standalone build.

## Combat and weapons

- [ ] Light/heavy attacks cause damage, heavy has a longer recovery, both stop at solid scenery.
- [ ] Guard reduces damage; it cannot be combined with an attack or active dodge.
- [ ] Dodge travels over time, has a real recovery gap, and repeated Space cannot chain uninterrupted invulnerability.
- [ ] At 30/60/144 FPS, dodge distance and mouse sensitivity remain comparable.
- [ ] HUD enemy windup warnings are visible and stepping out of the committed direction avoids the strike.
- [ ] No more than two committed enemy attacks occur at once throughout a full run, including interrupts/deaths.
- [ ] Player starts with Gladius; Axe/Spear/Mace pickups exist. E selects the nearest usable pickup.
- [ ] Swaps drop the old usable weapon near the floor; enemy drops are reachable.
- [ ] Hits consume durability once per attack, heavy consumes more, whiffs consume none, break changes to Unarmed.
- [ ] Weapon tuning changes damage/range/cadence. Current static weapon geometry is not mistaken for a finished swing animation.

## Score, crowd and lifecycle

- [ ] Hit/variety/kill scoring, combo timeout, ranks and crowd threshold healing work.
- [ ] Final kill transitions only after the intermission; display never becomes 4/3.
- [ ] Target clears on death/out-of-range. All three waves can be cleared.
- [ ] After defeat AND victory, R and the Restart button work without leaving Play Mode.
- [ ] Repeat restart three times: HP, wave, weapons, score, enemies, camera binding and time scale reset; no duplicate listeners/systems.
- [ ] Repeating pause/resume/restart produces no recurring exceptions in a 10-minute session.
- [ ] A local standalone build starts, pauses, resumes, completes/loses and restarts using the included scene.

## Evidence to return

Exact commit + Unity version; full Editor/Console log; EditMode and PlayMode XML; Game view screenshot; short capture showing lock, dodge recovery, hit, pickup, intermission and restart; generated Unity assets/settings; standalone build result and observed defects. Redact machine-specific usernames/paths before posting public logs.

User feedback that an earlier version launched is recorded, but it is not a substitute for these tests.

## Gate status

**NOT_RUN for this repair revision in Unity.** Source checks and numerical geometry checks must be reported separately from Unity compilation, Test Runner and actual gameplay. Keep PR #1 Draft; do not mark merge-ready without the evidence above.
