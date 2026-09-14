# Licensed gladiator model and actual-render report

Date: 2026-09-14. Feature branch only; PR #1 remains Draft and main is unchanged.

## Selected assets

- Body/helmet/armor/UV ramps: **BlackScorp, Low poly warrior**, CC0. The author's source is unrigged. https://opengameart.org/content/low-poly-warrior
- Gladius and shield: **Astarribadebirra / Nando, Gladiator Pack**, CC0. https://opengameart.org/content/gladiator-pack ; https://bixer.itch.io/gladiator
- Full provenance and archive hashes: `Assets/Resources/Gladiators/CREDITS.md`. No paid purchase or Capcom asset extraction.

## What was actually done

Downloaded the authors' permitted ZIP attachments on an isolated GitHub Actions runner, inspected archive contents and original meshes, and pinned archive/extracted-file SHA-256 values. Retained the real OBJ topology and UV texture ramps, aligned metres/root coordinates, added a 16-bone linear skin, curled the original hand topology into a prototype grip, and aligned real gladius/shield geometry to hand sockets.

`Gladiator.json` is the common mesh/UV/weights/material/pose source for both Blender and Unity. `ImportedGladiatorVisual` constructs an actual `SkinnedMeshRenderer`, not capsule/sprite substitutes. `ProceduralCombatRig` retains all Combat V2 public pose/weapon-socket signals and selects the imported model when the resource is available. Player/enemy leather colors differ without tinting the skin. `WeaponVisualFactory` uses the real gladius for Sword; Axe/Spear/Mace remain the previous placeholder visuals. Combat, durability, throws, execution logic and telemetry were not replaced with an older baseline.

Counts per sword-and-shield character: **16 bones, 2,506 triangles** (body 2,222; sword 120; shield 164). Unity seam-expanded vertex buffers: body 2,270; sword 258; shield 322.

## Real renderer evidence

- Blender **4.0.2**, **Cycles CPU**, 64 samples, denoising disabled because the distro build lacks the optional denoiser.
- Corrected render source: `c63c7d7ff3fb5b2f1bc8fad8de2d9cc7d7150fde`.
- Render run: `34852977089`; artifact: `10351862050` (`gladiator-integrated-assets`).
- Artifact SHA-256: `0418e8a6fed659eccceb52c986f63198fb69d9db748d07bd9283ea8bf175eb16`.
- Common model data SHA-256: `3c2d09fe17dc686f13180f8f33cd5150014f602b05326d90b2b52e5b8eb56ff3`.
- Outputs: three 1000x1200 character views and one 1440x900 two-character arena view; actual FBX/GLB exports and a packed Blender stage.
- Per-file sizes and SHA-256: `Art/Gladiator/render_manifest.json` after publication. Publication receipt is separate: `docs/GLADIATOR_ASSET_RECEIPT.json`.

**The images are offline Blender renders of the actual shared asset. They are not Unity screenshots, gameplay capture, image-generation mockups or proof of engine integration.** The offline presentation ring is separate from the Unity arena geometry.

## Visual self-review and regression

The first posed render was rejected: the source fingers extend below the wrist's Y band, so the first skin assignment incorrectly attached distal fingers to the hips. Source-material bounds also showed that the original sword blade points toward negative Y, not positive Y. The corrected revision fixes finger ownership, grip curl, blade direction and shield orientation. The actual corrected PNGs were retrieved and inspected.

`tools/validate_gladiator_asset.py` checks finite data, indices, normalized weights, texture references, topology and four-key-pose edge deformation. This rejects the severe detached-finger defect; it is not an all-animation/intersection or Unity test. Four additional Unity EditMode cases are authored in `ImportedGladiatorTests`; **NOT_RUN**.

## Remaining limits — do not claim production art approval

- Unity import/compile, skin rendering, hand-weapon timing, gameplay, standalone and U1-U7 are **NOT_RUN**. The assistant did not execute Unity.
- This is a low-poly prototype skin and procedural posing, not a fully authored/mocap animation library or a Mecanim Humanoid retargeting delivery.
- The FBX/GLB action is a short pose demonstration only. Gameplay still uses Combat V2's existing procedural pose signals.
- Shield/cloth clearance in some raised attack poses still needs refinement; no claim is made that all poses are free of self-intersection. The four-pose data checks are not final visual/animation acceptance.
- Final textures/PBR polish, authored locomotion/foot planting, full enemy asset variants and final art for other weapons remain separate work.
- Shader inclusion and active render-pipeline compatibility must be checked in a real standalone build; offline Blender success does not prove this.

## Local run

Preserve local edits, update `feat/arena-prototype-v0.1.0`, and open Unity 6000.3.23f1. When updating an older generated scene, exit Play Mode and run **Tools > Iron Sand Arena > Rebuild Prototype Arena**. Resources must include `Gladiators/Gladiator.json`, `skin.png` and `metal2.png`.

On Play, player/enemies should contain `Imported_CC0_Gladiator` and a 16-bone `SkinnedMeshRenderer`; `ProceduralCombatRig.UsesImportedModel` should be true. A warning about primitive fallback means the real model was not loaded and is a failed art-integration check, not success.

Run all existing suites and `ImportedGladiatorTests`, then test move/guard/light/heavy/hit/throw/swap/execution/restart and camera occlusion with the new model. Record the exact commit and real XML/logs/screenshots. Do not fabricate .meta files, Unity screenshots, test results or PASS gates.
