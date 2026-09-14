# Validation Gate — v0.1.0

This document is intentionally strict: repository presence is not proof that Unity imported, compiled, or played the project successfully.

## Required local evidence before merge

- [ ] Opened with Unity `6000.3.23f1`
- [ ] Package resolution completed without errors
- [ ] Script compilation completed with **0 errors**
- [ ] `Assets/Scenes/ArenaPrototype.unity` generated successfully
- [ ] EditMode tests passed
- [ ] Entered Play Mode successfully
- [ ] Player moves with WASD
- [ ] Camera orbits with mouse
- [ ] Light attack damages enemies
- [ ] Heavy attack damages enemies
- [ ] Guard reduces incoming damage
- [ ] Dodge grants a visible avoidance opportunity
- [ ] No more than two enemies commit attacks simultaneously
- [ ] Wave progression reaches final victory state
- [ ] Crowd Favor increases from player hits
- [ ] Crowd reward heals the player at threshold
- [ ] Console has no recurring exceptions during a 10-minute play session

## Evidence to return

Please return all of the following after local validation:

1. Unity Editor version from `Help > About Unity` or Editor log.
2. Screenshot of Console with zero errors after entering Play Mode.
3. Screenshot of Game view during wave 2 or later.
4. EditMode test result summary.
5. Any generated or changed `.meta`, `Packages/packages-lock.json`, and relevant `ProjectSettings` files.
6. A short note listing any gameplay defect, camera issue, collision issue, or balance issue observed.

## Gate status

**NOT_RUN** — ChatGPT authored repository content but cannot claim Unity execution evidence until it is run in a real Unity Editor environment.
