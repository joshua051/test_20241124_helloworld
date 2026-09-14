# Iron Sand Arena

Original Unity 6 third-person gladiator-arena combat vertical slice. It is an original spiritual-design study of classic arena action games; it contains no Capcom assets, characters, story, audio, levels or proprietary code/data.

## Licensed 3D gladiator art

The feature branch now includes an adapter for an actual CC0 warrior mesh plus a real gladius and shield, a 16-bone skin, and shared Blender/Unity pose data. The body/armor is by BlackScorp; sword/shield are from Astarribadebirra / Nando's Gladiator Pack. Full credit/license sources are retained in `Assets/Resources/Gladiators/CREDITS.md`.

Runtime data lives in `Assets/Resources/Gladiators/`. Standard FBX/GLB exports, a packed Blender stage and real offline renders are published under `Art/Gladiator/`. Publication evidence: `docs/GLADIATOR_ASSET_RECEIPT.json`. Detailed scope and known limits: `docs/GLADIATOR_RENDER_REPORT.md`.

The render images are **actual Blender Cycles renders, not Unity screenshots**. Unity compilation/PlayMode/standalone and U1-U7 remain **NOT_RUN**. The low-poly skin uses procedural poses, not a final authored animation library; some attack-pose shield/cloth clearances still need refinement. Other weapon types retain their earlier placeholder visuals.

## Combat Vertical Slice 2.0

The Draft branch implements the gameplay path rather than an immediate button-to-damage prototype:

- Startup / Active / Recovery attacks with late-swing/recovery input buffering and three-step chain tuning
- active-frame swept melee volumes with one hit per target per attack
- weapon-specific timing, reach, displacement, damage, Poise, hit stop and camera impulse
- directional guard + short Perfect Guard window + counter-stagger/disarm
- health + Poise + stronger stagger + temporary execution-ready state
- timed contextual execution
- dodge/invulnerability
- pickup/swap/durability/break/drop plus physical weapon throwing
- target lock and combat-facing movement
- procedural locomotion/guard/attack/hit/vulnerable/execution signals, now connected to the licensed skinned visual when available
- generated impact audio, transient impact VFX, hit stop and camera impulse
- Aggressor / Flanker / Brute / Skirmisher enemy roles with bounded attack tokens
- Style / combo / variety / throw / Perfect Guard / execution scoring
- Crowd Favor that queues rewards; the player must Appeal before spectators throw a physical food or weapon gift
- visible generated spectator ring, three waves, pause/resume/restart lifecycle

This remains a **prototype vertical slice**, not a claim of a perfect commercial-game reproduction. Final characters, authored mocap/keyframe animation, final audio/VFX and final gore/art remain production work. Combat V2 separates presentation from combat rules.

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
5. Run **Tools > Iron Sand Arena > Rebuild Prototype Arena** when updating an older generated scene. The scene receives Combat V2 systems and spectator geometry.
6. Run all EditMode tests including `ImportedGladiatorTests`, all PlayMode suites, then `docs/VALIDATION.md`.
7. Confirm the player/enemies contain `Imported_CC0_Gladiator` and `UsesImportedModel` is true. A primitive-fallback warning is not a successful imported-art check.

## Validation policy

GitHub's scene-independent C# harness and offline asset-data checks cannot prove Unity API integration, rendering, physics, frame behavior or combat feel. PR #1 remains Draft until the Unity and standalone gates in `docs/QUALITY_GATES.md` are evidenced.

See `docs/COMBAT_V2.md`, `docs/ARCHITECTURE.md`, `docs/QUALITY_GATES.md`, `docs/VALIDATION.md`, and `docs/GLADIATOR_RENDER_REPORT.md`.

Prototype codename: **Iron Sand Arena**
