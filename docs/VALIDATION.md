# Validation Gate — Combat Vertical Slice 2.0

Test the exact Git commit and record it. Do not report PASS from source review alone.

## Import / compile
- [ ] Unity `6000.3.23f1`
- [ ] packages resolved
- [ ] Console has 0 compile errors
- [ ] **Tools > Iron Sand Arena > Rebuild Prototype Arena** succeeds
- [ ] generated scene, `.meta`, `Packages/packages-lock.json` and relevant `ProjectSettings` captured

## Automated
- [ ] Repository Guard passes
- [ ] pure `CombatRulesHarness` passes
- [ ] all EditMode tests pass, including `CombatCoreTests` and `StateIntegrityTests`
- [ ] all PlayMode suites pass, including `ArenaSmokeTests`, `ArenaStateIntegrityTests`, `ArenaIntegrationTests`
- [ ] all `ImportedGladiatorTests` pass; runtime material is generated and referenced
- [ ] resume does not dispatch a same-frame attack; restart reloads its own arena scene

## Attack pipeline / combo
- [ ] LMB has readable Startup / Active / Recovery
- [ ] RMB is slower/heavier than LMB
- [ ] input during late Active / Recovery buffers the next attack
- [ ] three buffered attacks progress through chain stages and finisher tuning
- [ ] player cannot bypass Recovery by button spam
- [ ] active sweep hits each target at most once per attack
- [ ] attacks do not damage through solid scenery
- [ ] Sword/Axe/Spear/Mace differ in timing, reach and Poise effect

## Defense / reactions
- [ ] Q blocks only front-arc attacks
- [ ] rear attack bypasses guard
- [ ] fresh Q can Perfect Guard; held late Q gives ordinary block
- [ ] Perfect Guard counter-staggers and disarms armed enemy
- [ ] light/heavy hits produce readable reactions
- [ ] Poise break produces stronger stagger
- [ ] hit stop freezes combat pose while impact VFX/camera/audio remain readable

## Execution
- [ ] weaken enemy below threshold and break Poise
- [ ] HUD shows `EXECUTE [F]`
- [ ] F requires range and line-of-sight
- [ ] strike fires exactly once
- [ ] target dies once
- [ ] post-strike execution pose completes before control returns

## Weapons
- [ ] Gladius start; Axe/Spear/Mace starter pickups
- [ ] E cannot pick through walls and previous usable weapon drops
- [ ] durability consumes only after landed melee attack and at most once per attack instance
- [ ] break -> Unarmed
- [ ] G throws current weapon and leaves player Unarmed
- [ ] thrown weapon ignores its owner, can damage enemy, and surviving durability can return as pickup
- [ ] defeated armed enemies drop weapons

## Crowd / style
- [ ] Throw, Perfect Guard and Execution produce style awards
- [ ] threshold crossing queues a reward but does not heal or auto-launch it
- [ ] HUD shows pending reward
- [ ] C Appeal launches physical reward
- [ ] first reward is food and heals only when reached
- [ ] alternating reward includes a physical weapon gift resolving into a normal pickup

## AI / encounter
- [ ] Aggressor presses more often
- [ ] Flanker orbits more
- [ ] Brute is slower/heavier
- [ ] Skirmisher prefers more space
- [ ] enemy outside acquisition range always closes distance
- [ ] blocked direct pressure produces basic tangential obstacle steering
- [ ] no more than two attackers hold tokens
- [ ] stunned/execution-ready/dead enemies release tokens
- [ ] all three waves reach Victory

## Lifecycle / soak
- [ ] Esc pause/resume and focus-loss pause
- [ ] R restart after pause/defeat/victory
- [ ] restart resets time scale, health, weapon, score, crowd, wave and camera binding
- [ ] 10-minute Editor soak has no recurring exception/soft-lock
- [ ] standalone build launches/restarts
- [ ] 20-minute standalone soak logged

## Evidence package
1. exact commit SHA
2. Unity version / Editor log
3. zero-error Console evidence
4. GitHub Actions + EditMode + PlayMode test summaries/XML
5. 60–120 second capture: combo chain, Perfect Guard/disarm, throw, Poise break, execution, Crowd Appeal/food/weapon gift
6. genuine generated Unity metadata/lock/scene/settings files
7. defects with reproduction steps

## Gate status

Numbering is authoritative in `docs/QUALITY_GATES.md`. `tools/run_unity_validation.py` runs an isolated committed snapshot and rejects incomplete test results; its own fixture tests are not engine tests.

**NOT_RUN** — Unity-specific Combat V2 validation remains pending until real engine evidence is returned.
