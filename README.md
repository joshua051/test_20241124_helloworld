# Iron Sand Arena

Original Unity 6 third-person gladiator-arena combat vertical slice. It is an original spiritual-design study of classic arena action games; it contains no Capcom assets, characters, story, audio, levels or proprietary code/data.

## Combat Vertical Slice 2.0

The Draft branch now implements the gameplay path rather than an immediate button-to-damage prototype:

- Startup / Active / Recovery attacks with late-swing/recovery input buffering and three-step chain tuning
- active-frame swept melee volumes with one hit per target per attack
- weapon-specific timing, reach, displacement, damage, Poise, hit stop and camera impulse
- directional guard + short Perfect Guard window + counter-stagger/disarm
- health + Poise + stronger stagger + temporary execution-ready state
- timed contextual execution
- dodge/invulnerability
- pickup/swap/durability/break/drop plus physical weapon throwing
- target lock and combat-facing movement
- procedural original graybox humanoid locomotion/guard/attack/hit/vulnerable/execution poses
- generated impact audio, transient impact VFX, hit stop and camera impulse
- Aggressor / Flanker / Brute / Skirmisher enemy roles with bounded attack tokens
- Style / combo / variety / throw / Perfect Guard / execution scoring
- Crowd Favor that queues rewards; the player must Appeal before spectators throw a physical food or weapon gift
- visible generated spectator ring, three waves, pause/resume/restart lifecycle

This remains a **graybox vertical slice**, not a claim of a perfect commercial-game reproduction. Production skeletal characters, authored mocap/keyframe animation clips, final audio/VFX and final gore/art are separate production-content work. Combat V2 is designed so those assets replace the procedural presentation without replacing combat rules.

## Engine

- Unity `6000.3.23f1`
- Active Input Handling: `Input Manager (Old)` or `Both`
- Unity Test Framework
- no paid assets

## Controls

| Input | Action |
|---|---|
| WASD / Shift | Move / sprint |
| Mouse | Orbit camera |
| LMB / RMB | Light / Heavy; press again during late Active/Recovery to buffer the next chain attack |
| Q | Directional guard; fresh timing can Perfect Guard |
| Space + direction | Dodge |
| Tab | Lock / unlock target |
| E | Pick up / swap nearest visible weapon |
| G | Throw equipped weapon |
| F | Execute vulnerable locked target |
| C | Crowd Appeal when a reward is ready |
| Esc | Pause / resume |
| R | Restart while paused / defeated / victorious |

## Required update procedure

1. Preserve local edits and switch to `feat/arena-prototype-v0.1.0` while PR #1 remains Draft.
2. Open with Unity `6000.3.23f1`; allow package resolution/compilation.
3. Confirm Active Input Handling is Old or Both.
4. Exit Play Mode.
5. Run **Tools > Iron Sand Arena > Rebuild Prototype Arena**. This is mandatory after Combat V2 because the generated scene receives new systems and spectator geometry.
6. Run all EditMode tests, all PlayMode `ArenaSmokeTests`, then `docs/VALIDATION.md`.

## Validation policy

The GitHub pure-rules harness can prove compilation/execution of the scene-independent C# combat rules. It cannot prove Unity API integration, rendering, physics, frame behavior or combat feel. PR #1 remains Draft until the Unity and standalone gates in `docs/QUALITY_GATES.md` are evidenced.

See `docs/COMBAT_V2.md`, `docs/ARCHITECTURE.md`, `docs/QUALITY_GATES.md`, and `docs/VALIDATION.md`.

Prototype codename: **Iron Sand Arena**
