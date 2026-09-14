# Combat Vertical Slice 2.0

## Purpose

Iron Sand Arena uses an original implementation to target the systemic strengths of classic gladiator arena action: weighted melee, weapon improvisation, coordinated enemy pressure, spectacle and audience interaction. It does not copy commercial-game assets or proprietary code.

## Attack pipeline

```text
input
 -> AttackInputBuffer during late Active / Recovery
 -> AttackLibrary selects weapon + chain-step profile
 -> AttackTimeline: Startup
 -> procedural pose + forward displacement
 -> Active
 -> WeaponSweep overlap volume
 -> CombatImpact
 -> health + Poise
 -> reaction / stagger / execution-ready
 -> hit stop + camera impulse + VFX + generated SFX
 -> Recovery
 -> buffered next attack or return to locomotion
```

A target can be hit at most once per melee attack instance. A buffered chain advances through three tuning stages; the third stage is a stronger finisher profile. Weapon data is snapshotted when an attack starts, so a break or other state mutation cannot change an in-flight swing.

## Defense

Guard is directional. Rear attacks bypass it. A fresh guard press inside the Perfect Guard window negates the incoming impact, counter-staggers the attacker and disarms an armed enemy in current graybox tuning.

## Poise and execution

Poise is independent from health. A Poise break produces a stronger reaction. Breaking Poise while the enemy is below the execution-health threshold creates a temporary execution window. `F` starts a timed execution against a visible locked target in range; its strike event fires once and the post-strike sequence completes before control returns.

## Weapons

Sword, axe, spear and mace have distinct timing, reach and Poise profiles. Landed melee attacks consume durability at most once per attack instance. Broken weapons transition to Unarmed. `G` throws the current weapon as a Rigidbody projectile; surviving durability becomes a floor pickup. Pickups are claim-once and cannot be selected through solid scenery.

## Crowd

Style awards feed Crowd Favor. Crossing a reward threshold queues a Crowd reward rather than healing automatically. The HUD shows pending rewards. Press `C` to Appeal; spectator-side logic then physically throws the next reward into the arena. Rewards alternate between recovery food and arena weapons. Food heals only when the player reaches it; weapon gifts resolve into normal pickup objects.

## AI

`ArenaDirector` caps concurrent committed attackers. Enemies cycle Aggressor, Flanker, Brute and Skirmisher roles. They share attack timing/impact rules with the player, release attack tokens when stunned/vulnerable/dead, always close outside acquisition range and use basic tangential steering when scenery blocks direct pressure.

## Presentation

`ProceduralCombatRig` builds an original primitive humanoid and exposes locomotion, guard, attack, hit, vulnerable and execution poses from real combat state. Combat hit stop freezes pose clocks while camera impulse and transient impact feedback remain readable. Runtime-generated impact clips provide placeholder audio without bundled external assets.

The procedural presentation is intentionally replaceable. Production skeletal meshes, authored animation clips and final sound should consume the same combat state contracts rather than rewrite the combat model.
