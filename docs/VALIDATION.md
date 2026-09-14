# Validation Gate — v0.1.0 combat slice

This document is intentionally strict: repository presence is not proof that Unity imported, compiled, tested, or played the project successfully.

See `docs/QUALITY_GATES.md` for the gate hierarchy.

## Required local evidence before merge

### Import and compile
- [ ] Opened with Unity `6000.3.23f1`
- [ ] Package resolution completed without errors
- [ ] Script compilation completed with **0 errors**
- [ ] `Assets/Scenes/ArenaPrototype.unity` rebuilt successfully from **Tools > Iron Sand Arena > Rebuild Prototype Arena**
- [ ] Generated `.meta`, `Packages/packages-lock.json`, and material `ProjectSettings` changes captured for review

### EditMode tests
- [ ] All `CrowdFavorSystemTests` pass
- [ ] All `WeaponCatalogTests` pass
- [ ] All `StyleScoreModelTests` pass

### Controls and camera
- [ ] WASD movement works
- [ ] Mouse orbit works when unlocked from target lock
- [ ] Shift sprint works
- [ ] Tab acquires a nearby valid enemy
- [ ] Tab releases the current target
- [ ] Lock-on keeps the player facing the target while strafing
- [ ] Lock-on camera remains readable and does not produce obvious snapping/nausea
- [ ] Esc releases the cursor

### Combat
- [ ] Light attack damages enemies
- [ ] Heavy attack damages enemies and is perceptibly slower/stronger
- [ ] Guard reduces incoming damage
- [ ] Dodge grants a usable avoidance opportunity
- [ ] No more than two enemies commit attacks simultaneously
- [ ] Enemy windup is readable enough to react to in graybox form

### Weapons
- [ ] Player starts with Gladius
- [ ] Axe, Spear, and Mace starter pickups exist after scene rebuild
- [ ] E swaps to the nearest pickup
- [ ] Previous usable weapon is dropped when swapping
- [ ] Different weapon types visibly alter reach/damage/cadence in gameplay
- [ ] Durability decreases only when an attack actually hits
- [ ] Heavy hits consume more durability than light hits
- [ ] A broken weapon transitions to Unarmed
- [ ] Defeated enemies drop a usable weapon pickup

### Style and crowd
- [ ] Successful hits increase Style Score
- [ ] Combo count increases during chained hits
- [ ] Combo expires after the configured window
- [ ] Varying weapon or attack choice rewards more style than immediate repetition
- [ ] Kill bonus is visible in score progression
- [ ] Crowd Favor increases from style awards
- [ ] Crowd reward heals the player at thresholds

### Encounter lifecycle
- [ ] Wave progression reaches final Victory state
- [ ] Lock target clears safely when its enemy dies or leaves lock range
- [ ] No recurring exceptions during a 10-minute Play Mode session

## Evidence to return

1. Unity Editor version from `Help > About Unity` or Editor log.
2. Screenshot of Console with zero errors after entering Play Mode.
3. Screenshot of Game view during wave 2 or later showing HUD weapon/style/crowd state.
4. EditMode Test Runner result summary.
5. Generated/changed `.meta`, `Packages/packages-lock.json`, and relevant `ProjectSettings` files.
6. Short notes for any gameplay defect, camera issue, collision issue, input issue, balance issue, or weapon/style exploit observed.
7. If possible, a 30–60 second capture showing lock-on, one weapon swap, one weapon break/drop, and a wave transition.

## Gate status

**NOT_RUN** — repository changes are authored and statically reviewed, but no Unity execution evidence has been supplied yet.
