# Architecture

## Constraints

1. Runtime combat code does not depend on Editor code.
2. Rules that can be pure are isolated and EditMode-testable.
3. `ArenaDirector` owns encounter lifecycle and attack-token arbitration, not player input.
4. Presentation consumes combat state; presentation does not decide damage rules.
5. Generated arena rebuilding is explicit and save-protected; no domain-load mutation.
6. Repository presence never counts as Unity execution evidence.

## Combat domain

- `AttackProfile` / `AttackLibrary`: weapon/action timing and weight data.
- `AttackTimeline`: pure Startup/Active/Recovery state machine and attack displacement fraction.
- `CombatImpact` / `ImpactResult`: explicit impact contract.
- `PoiseState`: pure stagger resource/recovery rules.
- `GuardState`: pure front-arc and Perfect Guard timing rules.
- `ExecutionState`: pure execution timing and one-shot strike event.
- `Combatant`: health, Poise, invulnerability, hit stun, execution-ready and death lifecycle.
- `WeaponSweep`: active-frame melee geometry.
- `CombatFreezeSystem`: local combat hit-stop clock without hijacking `Time.timeScale`.
- `CombatFeedbackSystem`: camera impulse, transient VFX and generated placeholder impact audio.
- `ProceduralCombatRig`: replaceable graybox animation/presentation bridge.

## Player domain

`PlayerGladiator` owns local input and coordinates locomotion, attack timeline, directional guard, Perfect Guard, dodge, lock-on, execution and weapon actions. It does not own wave or Crowd rules.

`PlayerWeaponController` owns equipped weapon, durability, visible weapon instance, pickup claim, throwing and transition to Unarmed.

## Enemy domain

`EnemyGladiator` owns local approach/orbit/attack behavior and uses the same attack/impact rules as the player. `EnemyRole` adjusts pressure style. `ArenaDirector` remains the authority for concurrent attack tokens.

## Crowd and scoring

```text
combat event
 -> CombatStyleSystem / StyleScoreModel
 -> ArenaDirector.RegisterPlayerHit
 -> CrowdFavorSystem
 -> threshold event
 -> CrowdRewardDirector
 -> physical CrowdGift
 -> player reaches gift
 -> Heal
```

## Encounter flow

```text
PrototypeBuilder
 -> generated geometry + crowd ring + player + camera + systems
ArenaDirector
 -> spawn role/weapon combinations
 -> attack-token coordination
 -> delayed wave transition
 -> Victory
ArenaSession
 -> pause / focus handling / restart
```

## Replaceable prototype debt

- procedural primitive humanoid instead of production rig and authored clips
- procedural attack displacement instead of imported animation root motion
- swept physics volumes instead of authored per-bone hitbox tracks
- direct CharacterController steering instead of NavMesh/path-planning
- legacy Input Manager instead of gamepad-ready Input System actions
- generated placeholder audio/VFX instead of production assets

These are deliberate interfaces, not excuses to bypass validation. Production replacements should preserve the domain contracts and tests.
