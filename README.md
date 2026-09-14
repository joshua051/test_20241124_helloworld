# Iron Sand Arena

**Iron Sand Arena** is an original Unity 6 third-person gladiator-arena action prototype built around weighty melee pressure, improvised weapon swapping, crowd favor, style scoring, and multi-enemy encounter direction.

It intentionally contains **no Capcom assets, characters, story, audio, level data, or proprietary code**. It is not a remake or reverse-engineered copy of *Shadow of Rome*; it is an original work using broadly applicable arena-action design ideas.

## Engine

- Unity `6000.3.23f1`
- Unity Test Framework for EditMode tests
- No paid assets
- Keyboard + mouse graybox controls

## Implemented in code on the current Draft PR

Validation is still pending; "implemented" below means repository code exists, not that Unity execution has been proven.

- Third-person movement, sprint, and orbit camera
- Light / heavy prototype melee attacks
- Guard, hit-stun, knockback, and dodge invulnerability
- Target lock-on with combat-facing strafing and lock camera framing
- Multi-enemy steering with central attack-token arbitration
- Three escalating arena waves
- Sword / Axe / Spear / Mace tuning plus Unarmed fallback
- World weapon pickups, nearest pickup swap, durability, break-to-Unarmed, and enemy weapon drops
- Combo / variety / kill Style Score with rank and timeout
- Crowd Favor fed by style awards and healing rewards at thresholds
- Generated Roman-inspired graybox arena with starter weapon pickups
- Debug HUD for health, weapon, durability, style, combo, crowd, wave, enemy count, and lock target
- EditMode tests for Crowd Favor, weapon tuning, and pure style scoring
- Repository guard workflow and evidence-based quality gates

## Explicitly not implemented yet

- Root-motion combat animation
- Animation-event weapon hitboxes
- Camera obstruction solver
- Directional/perfect guard, shield mechanics, disarm
- Weapon throwing and executions
- NavMesh enemy locomotion / authored combat archetype behavior trees
- Production UI/VFX/audio/art
- Gamepad + Unity Input System migration
- Accessibility, save progression, career/roguelite layer, bosses

## Controls

| Input | Action |
|---|---|
| `WASD` | Move / strafe |
| Mouse | Orbit camera / vertical aim while locked |
| `Left Shift` | Sprint |
| `Left Mouse` | Light attack |
| `Right Mouse` | Heavy attack |
| `Space` | Dodge |
| `Q` | Guard |
| `Tab` | Lock / unlock target |
| `E` | Pick up / swap nearest weapon |
| `Esc` | Release cursor |

## First local run

1. Clone the repository and switch to `feat/arena-prototype-v0.1.0` while PR #1 is still under validation.
2. Open the repository root with Unity `6000.3.23f1`.
3. Allow package resolution and compilation to finish.
4. Confirm the Console has zero compile errors.
5. Run **Tools > Iron Sand Arena > Rebuild Prototype Arena**.
6. Run all EditMode tests.
7. Enter Play Mode and execute `docs/VALIDATION.md`.
8. Return generated `.meta`, `Packages/packages-lock.json`, relevant `ProjectSettings`, Test Runner output, Console evidence, and gameplay observations before merge.

The Builder is explicit by design and will not replace scenes on domain load.

## Core loop under test

```text
enter arena
  -> survive coordinated pressure
  -> lock / guard / dodge / attack
  -> damage or defeat enemy
  -> earn style + crowd favor
  -> weapon durability forces adaptation
  -> swap starter or dropped weapon
  -> crowd reward sustains run
  -> clear waves
  -> victory
```

## Architecture

```text
Assets/
  Editor/PrototypeBuilder.cs
  Scripts/
    Arena/
    Combat/
    Enemy/
    Player/
    Scoring/
    UI/
  Tests/EditMode/
docs/
  ARCHITECTURE.md
  QUALITY_GATES.md
  VALIDATION.md
tools/
  repository_guard.py
```

See `docs/ARCHITECTURE.md` for ownership boundaries and accepted prototype debt.

## Validation policy

The Draft PR remains **NOT_RUN** for Unity execution until a real Unity `6000.3.23f1` session produces import, compile, Test Runner, Play Mode, and Console evidence. See `docs/QUALITY_GATES.md` and `docs/VALIDATION.md`.

Prototype codename: **Iron Sand Arena**
