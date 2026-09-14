# Iron Sand Arena

**Iron Sand Arena** is an original Unity 6 third-person gladiator-arena action prototype inspired by the *design spirit* of classic arena combat games: weighty melee exchanges, improvised weapons, crowd favor, multi-enemy pressure, and escalating waves.

This project intentionally contains **no Capcom assets, characters, story, audio, level data, or proprietary code**. The prototype uses original systems and generated placeholder geometry so its combat loop can be tested immediately.

## Engine

- Unity `6000.3.23f1`
- No paid assets
- No external packages required for the prototype
- Keyboard + mouse first; controller support is planned

## Current playable vertical slice

- Third-person movement and camera
- Sprint and combat-facing movement
- Light / heavy melee attacks
- Guard with damage reduction
- Dodge with a short invulnerability window
- Hit-stun and knockback
- Multi-enemy arena AI
- Attack-slot/token logic so enemies do not all attack at once
- Arena waves
- Crowd Favor meter
- Crowd rewards (healing at favor thresholds)
- Pick-up / swap weapon archetypes
- Score and combo tracking
- Generated Roman-inspired graybox arena
- Minimal HUD and restart loop

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
| `E` | Pick up nearby weapon |
| `Tab` | Lock / unlock nearest target |
| `R` | Restart after defeat / victory |
| `Esc` | Release cursor |

## First launch

1. Clone this repository.
2. Open the repository root in **Unity 6000.3.23f1**.
3. Wait for script compilation.
4. The editor bootstrap creates `Assets/Scenes/ArenaPrototype.unity` automatically if it does not exist.
5. Open `Assets/Scenes/ArenaPrototype.unity` if Unity did not open it automatically.
6. Press **Play**.

You can also rebuild the graybox scene from **Tools > Iron Sand Arena > Rebuild Prototype Arena**.

## Prototype goal

The first milestone is deliberately narrow: prove that this loop is fun before investing in production art.

`enter arena -> survive pressure -> vary attacks -> gain crowd favor -> improvise weapons -> receive crowd reward -> clear wave -> champion wave`

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
      WeaponDefinition.cs
    Enemy/
      EnemyGladiator.cs
    Player/
      PlayerGladiator.cs
      ThirdPersonArenaCamera.cs
    UI/
      PrototypeHUD.cs
```

## What this is not

This is not a remake, ROM recreation, asset extraction project, or reverse-engineered copy of *Shadow of Rome*. It is an original prototype built around broadly usable game-design ideas such as arena combat, weapon improvisation, crowd scoring, melee hit reactions, and encounter direction.

## Next production gates

1. Replace capsule combatants with humanoid rigs + root-motion animation.
2. Add precise weapon hitboxes and animation-event attack windows.
3. Add shield / perfect-guard / disarm mechanics.
4. Add weapon durability and thrown weapons.
5. Add executions and contextual arena interactions.
6. Add authored enemy archetypes and boss behavior.
7. Add gamepad + Input System support.
8. Replace graybox arena with original Roman-inspired production art.

---

Prototype codename: **Iron Sand Arena**
