# Gladiator asset selection and render acceptance

Request: find an appropriate online gladiator model, apply it to Iron Sand Arena, and render the result.

## Candidate provenance checked on 2026-09-14

1. Astarribadebirra / Nando, Gladiator Pack: https://opengameart.org/content/gladiator-pack ; author storefront https://bixer.itch.io/gladiator . Both pages identify CC0. OBJ pack: 15 models and 3 alpha textures. Official attachment: https://opengameart.org/sites/default/files/gladiatorpack.zip . Suitability remains subject to inspecting the actual meshes; not advertised as rigged.
2. BlackScorp, Low poly warrior: https://opengameart.org/content/low-poly-warrior . CC0, explicitly not rigged, removable armor. Official attachment: https://opengameart.org/sites/default/files/basechar_0.zip .
3. tomasz0p0, Retiarius Gladiator: https://sketchfab.com/3d-models/retiarius-gladiator-low-poly-3446255227b14a1f83e56b24d619f8e0 . CC Attribution, 8.2k triangles according to the author page; download requires the site's supported download flow. Do not scrape protected viewer geometry.

No paid purchase, account sign-in bypass, proprietary Capcom content, or scraped preview geometry is authorized by this pipeline. Include source attribution and the applicable license with any assets actually selected.

## Acceptance

- Download real source files through their permitted download links; record their SHA-256 and contents.
- Inspect actual geometry and material/texture dependencies before selection.
- Preserve the existing Combat V2 mechanics, weapon ownership, telemetry, and state-integrity fixes.
- Apply the selected mesh to the visual layer; never replace working gameplay scripts with an old baseline.
- Render the actual mesh in a real 3D renderer; label offline renders separately from Unity gameplay evidence.
- Keep reproducible import/rig/material/render scripts and output manifests.
- Do not claim Unity import, skinning, physics or PlayMode validation without a real Unity execution.

## Status

SELECTION_IN_PROGRESS. Candidate pages have been checked; no successful asset import, integration or render is asserted by this document yet. The assistant container's direct source download failed; a permitted connected execution route is being investigated. Existing U1-U7 gates are unchanged.
