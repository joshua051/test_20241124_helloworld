# Iron Sand Arena

**Iron Sand Arena** is an original Unity 6 third-person gladiator-arena action prototype inspired by the design spirit of classic arena combat games: weighty melee exchanges, crowd favor, multi-enemy pressure, and escalating waves.

This project intentionally contains **no Capcom assets, characters, story, audio, level data, or proprietary code**. The prototype uses original systems and generated placeholder geometry so its combat loop can be tested before production art begins.

## Engine

- Unity `6000.3.23f1`
- No paid assets
- Unity Test Framework for EditMode tests
- Keyboard + mouse first; controller support is planned

## Implemented in v0.1.0 branch

- Third-person movement and orbit camera
- Sprint
- Light / heavy prototype melee attacks
- Guard with damage reduction
- Dodge with a short invulnerability window
- Hit-stun and knockback
- Multi-enemy arena AI
- Central attack-token arbitration so enemies do not all commit attacks simultaneously
- Three escalating arena waves
- Crowd Favor meter
- Crowd healing rewards at favor thresholds
- Generated Roman-inspired graybox arena
- Minimal debug HUD
- EditMode tests for core Crowd Favor behavior
- Explicit validation checklist with Gate status

## Not implemented yet

- Target lock-on
- Weapon pickups / swapping / durability / throwing
- Score-combo variety system
- Root-motion combat animation
- Per-weapon hitboxes and animation-event attack windows
- Perfect guard / disarm / executions
- Production UI, VFX, audio, gamepad support, save progression

These are planned gates, not claimed functionality.

## Controls

| Input | Action |
|---|---|
| `WASD` | Move |
| Mouse | Orbit camera |
| `Left Shift` | Sprint |
| `Left Mouse` | Light attack |
| `Right Mouse` | Heavy attack |
| `Space` | Dodge |
| `Q` | Guard |
| `Esc` | Release cursor |

## First launch

1. Clone this repository and switch to branch `feat/arena-prototype-v0.1.0` while the PR is still under validation.
2. Open the repository root in **Unity 6000.3.23f1**.
3. Allow package resolution and script compilation to finish.
4. Confirm the Console has zero compile errors.
5. Run **Tools > Iron Sand Arena > Rebuild Prototype Arena**.
6. Open `Assets/Scenes/ArenaPrototype.unity` if it is not already open.
7. Run the EditMode tests.
8. Press **Play** and execute the checklist in `docs/VALIDATION.md`.

The Builder deliberately requires an explicit menu action. It does **not** create or replace scenes as a domain-load side effect.

## Prototype goal

The first milestone is deliberately narrow: prove the combat pressure loop before investing in production art.

`enter arena -> survive pressure -> vary light/heavy/guard/dodge decisions -> gain crowd favor -> receive crowd reward -> clear wave -> final victory`

## Architecture

```text
Assets/
  Editor/
    PrototypeBuilder.cs
  Scripts/
    Arena/
      ArenaDirector.cs
      CrowdFavorSystem.cs
    Combat/
      Combatant.cs
    Enemy/
      EnemyGladiator.cs
    Player/
      PlayerGladiator.cs
      ThirdPersonArenaCamera.cs
    UI/
      PrototypeHUD.cs
  Tests/
    EditMode/
      CrowdFavorSystemTests.cs
```

See `docs/ARCHITECTURE.md` for responsibilities and accepted prototype debt.

## Validation policy

Repository content is **not** treated as proof of Unity execution. Until a real Unity `6000.3.23f1` session returns compile, Test Runner, Play Mode, and Console evidence, the validation Gate remains **NOT_RUN**.

See `docs/VALIDATION.md` for the exact evidence required before merge.

## What this is not

This is not a remake, ROM recreation, asset extraction project, or reverse-engineered copy of *Shadow of Rome*. It is an original prototype built around broadly usable game-design ideas such as arena combat, crowd scoring, melee hit reactions, and encounter direction.

## Next production gates

1. Pass v0.1.0 local Unity validation with zero compile/runtime errors.
2. Replace prototype overlap attacks with authored attack windows and weapon hitboxes.
3. Add target lock-on and combat-facing locomotion.
4. Add original weapon pickup, durability, throwing, and archetype data.
5. Add shield / perfect guard / disarm mechanics.
6. Add executions and contextual arena interactions.
7. Replace capsule combatants with humanoid rigs + root-motion animation.
8. Add authored enemy archetypes, bosses, accessibility, and gamepad support.
9. Replace graybox arena with original Roman-inspired production art.

---

Prototype codename: **Iron Sand Arena**
