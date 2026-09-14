# Iron Sand Arena

Original Unity 6 third-person gladiator-arena combat vertical slice. It is a spiritual design study of classic arena action games, built with original code, generated geometry and original graybox presentation. It contains no Capcom assets, characters, story, audio, levels or proprietary data.

## Current branch target: Combat Vertical Slice 2.0

The current Draft PR moves the prototype from immediate button-to-damage queries toward a real combat pipeline:

- data-driven Startup / Active / Recovery attack timelines
- procedural root-motion-equivalent attack displacement
- swept melee volume during Active frames; one hit per target per attack
- weapon-specific cadence, reach, poise damage, hit stop and movement
- directional guard with a short Perfect Guard window
- Perfect Guard counter-stagger and disarm
- health + Poise + stagger + execution-ready state
- contextual execution on vulnerable locked targets
- timed dodge and invulnerability
- weapon pickup / swap / durability / break / enemy drops
- weapon throwing with physical projectile collision and recoverable durability
- target lock and combat-facing movement
- procedural graybox humanoid combat poses, locomotion, guard, hit reaction and execution posing
- hit stop, camera impulse, transient impact VFX and runtime-generated impact audio
- four enemy roles plus central attack-token concurrency
- three encounter waves
- Style / combo / variety / kill / Perfect Guard / throw / execution scoring
- physical Crowd reward throws instead of direct threshold healing
- visible crowd ring around the arena
- pause / resume / restart lifecycle

This remains a **graybox combat vertical slice**, not a production remake. The procedural rigs and generated audio are placeholders designed to validate timing, feedback and system interaction before production character art, authored motion-capture animation and final sound are commissioned.

## Engine

- Unity `6000.3.23f1`
- legacy Input Manager for this branch (`Input Manager (Old)` or `Both`)
- Unity Test Framework
- no paid assets

## Controls

| Input | Action |
|---|---|
| WASD / Shift | Move / sprint |
| Mouse | Orbit camera |
| LMB | Light attack |
| RMB | Heavy attack |
| Q | Directional guard; newly pressed guard can Perfect Guard |
| Space + direction | Dodge |
| Tab | Lock / unlock target |
| E | Pick up / swap nearest visible weapon |
| G | Throw equipped weapon |
| F | Execute a vulnerable locked target |
| Esc | Pause / resume |
| R | Restart while paused / defeated / victorious |

## First run after pulling this revision

1. Preserve local edits before pulling the Draft branch `feat/arena-prototype-v0.1.0`.
2. Open the repository with Unity `6000.3.23f1` and let packages/scripts import.
3. Set **Active Input Handling** to `Input Manager (Old)` or `Both` if required, then restart the Editor.
4. Exit Play Mode.
5. Run **Tools > Iron Sand Arena > Rebuild Prototype Arena**. This is mandatory because the generated scene receives new runtime systems and crowd geometry.
6. Run all EditMode tests.
7. Run the PlayMode `ArenaSmokeTests`.
8. Execute `docs/VALIDATION.md` and return real Console/Test Runner/Play Mode/standalone evidence before merge.

## Combat loop

```text
enter arena
 -> read enemy pressure and windups
 -> lock / reposition
 -> light or heavy attack through startup/active/recovery
 -> hit stop + reaction + poise damage
 -> guard / Perfect Guard -> stagger + disarm
 -> weapon breaks, swap or throw it
 -> vary actions to build style and Crowd Favor
 -> spectators physically throw recovery gifts
 -> poise-break weakened enemy -> execution window
 -> execute or continue pressure
 -> clear waves -> victory
```

## Validation policy

Repository code is not proof that Unity executed it. PR #1 stays Draft until `docs/QUALITY_GATES.md` is evidenced. Any item not actually run in Unity is `NOT_RUN`, never implied PASS.

See:
- `docs/ARCHITECTURE.md`
- `docs/COMBAT_V2.md`
- `docs/QUALITY_GATES.md`
- `docs/VALIDATION.md`

Prototype codename: **Iron Sand Arena**
