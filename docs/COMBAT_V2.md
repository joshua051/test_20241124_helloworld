# Combat Vertical Slice 2.0

## Purpose

Move Iron Sand Arena from a systems-only arena graybox into a combat-feel vertical slice. The goal is not to copy Shadow of Rome assets or proprietary implementation. The target is an original arena-action combat loop with comparable pillars: weight, weapon improvisation, enemy pressure, spectacle and audience feedback.

## Attack pipeline

```text
input
 -> AttackLibrary selects weapon/action profile
 -> AttackTimeline: Startup
 -> procedural combat pose + forward displacement
 -> Active
 -> WeaponSweep overlap volume
 -> CombatImpact
 -> health + Poise
 -> hit reaction / stagger / execution-ready
 -> hit stop + camera impulse + VFX + generated impact SFX
 -> Recovery
 -> return to locomotion/guard/dodge
```

Each attack has explicit Startup, Active and Recovery time plus damage multiplier, Poise damage, sweep radius, root displacement, hit-stop time and camera impulse. A target can be hit at most once by one melee attack instance.

## Defense

Guard is directional. An impact outside the guard arc bypasses blocking. A fresh guard press within the configured Perfect Guard window negates the incoming impact, creates feedback, counters enemy Poise and disarms an armed attacker in this graybox tuning.

## Poise and execution

Damage and Poise are separate resources. Poise breaking creates a heavier reaction. If Poise breaks while health is below the execution threshold, the enemy enters a temporary execution-ready state. A locked player within range can press `F` to start a timed execution sequence with a single strike event.

## Weapons

Sword, axe, spear and mace retain data-driven stats but also receive different attack timing/weight profiles. Weapons lose durability only after a melee attack lands. Broken weapons become Unarmed. `G` throws the current weapon as a physical Rigidbody projectile; surviving durability can return to the floor as a pickup.

## Crowd

Crowd Favor still derives from style awards, but threshold rewards no longer call Heal directly. `CrowdRewardDirector` launches a physical food gift from above the arena edge. The player must reach the gift to receive healing. The generated outer crowd ring makes the source visible in graybox form.

## AI

`ArenaDirector` still bounds simultaneous committed attackers. Enemies now have four roles:
- Aggressor: closes and attacks more often.
- Flanker: higher orbit pressure.
- Brute: slower movement, heavier attacks.
- Skirmisher: prefers slightly longer spacing.

All roles share the same attack timeline and CombatImpact rules as the player. The previous approach/attack dead zone is removed: outside attack acquisition range, enemies always close distance.

## Presentation

`ProceduralCombatRig` builds an original primitive humanoid with limbs, buckler and weapon socket. It provides placeholder locomotion, guard, attack, hit-reaction, vulnerable and execution poses driven by the real combat state. `CombatFeedbackSystem` generates simple impact audio at runtime, transient hit flashes, hit stop and camera impulses.

This is intentionally replaceable presentation. Production character assets and authored animation clips should connect to the same combat state contracts rather than rewrite combat rules.

## Explicit non-goals for this milestone

- copied character models, animation clips, sounds, textures or levels from any commercial game
- final gore/dismemberment art
- final motion-capture quality
- production camera tuning for every arena layout
- network multiplayer
- career/save economy

Those are separate production milestones. The V2 gate is combat-system completeness and evidence-backed playability.
