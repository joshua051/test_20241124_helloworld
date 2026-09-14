# Gladiator asset provenance

These are real third-party meshes, not original geometry attributed to this repository.

## Body, helmet, armor and texture ramps
- Title: Low poly warrior
- Author: BlackScorp
- Author page: https://opengameart.org/content/low-poly-warrior
- Source archive: https://opengameart.org/sites/default/files/basechar_0.zip
- License shown by the author page on 2026-09-14: **CC0**.
- Archive SHA-256: `7f6ecd8044093b6c8ab2e594224a3524c2510317ae0b8ec6d2cf742877137c04`.
- Source was unrigged. This project adds a 16-bone linear skin, shared procedural pose presets, game-root alignment and team leather materials. The original mesh, UVs and texture ramps remain recognizable.

## Gladius and shield
- Title: Gladiator Pack
- Author: Astarribadebirra / Nando (bixer)
- Source page: https://opengameart.org/content/gladiator-pack
- Author storefront: https://bixer.itch.io/gladiator
- Source archive: https://opengameart.org/sites/default/files/gladiatorpack.zip
- License shown by both pages and included License.txt: **CC0**.
- Archive SHA-256: `b341a28b9e777588bd218457ace9c37ce7fa2ec97e16d0690c3cca1da4d281ec`.
- Used files: `Gladii.obj` and `Shield.obj`. Adaptations: scale, orientation, material assignments, grip position and attachment to hands.

CC0 reference: https://creativecommons.org/publicdomain/zero/1.0/ . Attribution is retained for provenance even though the authors chose CC0. No Capcom models or protected game content are used. No paid assets were purchased.

## Rebuilding and evidence
`tools/art/inspect_sources.py` obtains permitted official attachments. `build_model_data.py` verifies pinned archive and extracted-file hashes before producing `Gladiator.json`, textures and an asset manifest. `render_gladiator.py` uses that SAME JSON to skin and render with Blender Cycles/CPU and to export FBX/GLB.

Unity reads the checked-in JSON using `ImportedGladiatorVisual` and drives its 16 bones from the existing Combat V2 signals. This is a procedural-skinned prototype, not motion capture, Mecanim retargeting or a full authored animation set. The shield is a visual representation of the existing guard state; this asset change does not add shield collision or new combat rules.

Blender renders and exported asset checks do not establish Unity import/compilation/PlayMode/standalone success. Those gates remain separate. No Unity-generated .meta files or test evidence are fabricated.
