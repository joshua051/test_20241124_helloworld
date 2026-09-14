# Validation Gate — Combat Vertical Slice 2.0

Test exact commit and record it. Do not report PASS from source review alone.

## Import / compile
- [ ] Unity `6000.3.23f1`
- [ ] packages resolved
- [ ] Console has 0 compile errors
- [ ] **Tools > Iron Sand Arena > Rebuild Prototype Arena** succeeds
- [ ] generated scene, `.meta`, `Packages/packages-lock.json` and relevant `ProjectSettings` captured

## Automated
- [ ] all EditMode tests pass
- [ ] `CombatCoreTests` pass
- [ ] `StateIntegrityTests` pass
- [ ] all pre-existing regression tests pass
- [ ] all PlayMode `ArenaSmokeTests` pass

## Attack pipeline
- [ ] LMB has visible startup, active swing and recovery
- [ ] RMB is slower/heavier than LMB
- [ ] player cannot spam through recovery
- [ ] active swing hits each target at most once per attack
- [ ] attacks do not damage through solid scenery
- [ ] Sword/Axe/Spear/Mace differ in timing, reach and Poise effect

## Defense / reactions
- [ ] Q blocks only front-arc attacks
- [ ] rear attack bypasses guard
- [ ] newly pressed Q can Perfect Guard
- [ ] held Q outside perfect window gives ordinary block
- [ ] Perfect Guard counter-staggers and disarms armed enemy
- [ ] light/heavy hits produce readable reactions
- [ ] Poise break produces stronger stagger
- [ ] hit stop/camera/audio/VFX are synchronized enough to read the hit

## Execution
- [ ] weaken enemy below execution threshold and break Poise
- [ ] HUD displays `EXECUTE [F]`
- [ ] F starts only inside range/line-of-sight
- [ ] execution strike occurs once
- [ ] target dies once and state cleans safely
- [ ] player regains control

## Weapons
- [ ] starts with Gladius
- [ ] Axe/Spear/Mace starter pickups exist
- [ ] E swaps nearest visible pickup; cannot pick through wall
- [ ] previous usable weapon drops
- [ ] durability consumes only after landed melee attack, once per attack
- [ ] break -> Unarmed
- [ ] G throws current weapon and leaves player Unarmed
- [ ] thrown collision can damage enemy
- [ ] surviving thrown durability can return as pickup
- [ ] defeated armed enemy drops weapon

## Crowd / style
- [ ] varied actions score effectively
- [ ] Throw, Perfect Guard and Execution add style
- [ ] Crowd threshold launches a visible physical gift
- [ ] gift traverses arena physics and heals only when reached
- [ ] no direct invisible threshold Heal remains

## AI / encounter
- [ ] Aggressor presses more often
- [ ] Flanker orbits more
- [ ] Brute is slower/heavier
- [ ] Skirmisher prefers more space
- [ ] enemy outside acquisition range always closes distance
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
1. exact Git commit SHA
2. Unity version/Editor log
3. zero-error Console evidence
4. EditMode + PlayMode Test Runner summaries/XML
5. 60–120 second capture: attack phases, Perfect Guard/disarm, throw, Poise break, execution and Crowd gift
6. genuine generated Unity metadata/lock/scene/settings files
7. defects with reproduction steps

## Gate status

**NOT_RUN** — Combat V2 source is authored/static-reviewed only until real Unity evidence is returned.
