# Integration preflight 01

Date: 2026-09-14. Scope: model startup, session/restart lifecycle, reproducible evidence. No new arena art or gameplay rules are claimed.

## Findings -> changes

| Finding at the reviewed baseline | Change | Verification limit |
|---|---|---|
| Runtime materials are created through Shader.Find with no explicit retained material reference. | Explicit Builder creates `Resources/Gladiators/RuntimeMaterial.mat`; runtime clones it; build preflight rejects missing/wrong pipeline material. | Needs graphical Unity and standalone shader validation. |
| Session enables gameplay without checking imported player/enemy visuals. | Readiness coroutine checks player, director, real imported rigs and own-scene camera before enabling input. | Three new PlayMode cases authored, NOT_RUN. |
| Restart uses the active scene, which can be an unrelated additive scene. | Reload the session's own scene, one restart in flight. | Authored owned-scene restart regression. |
| Any old session's OnDestroy can restore time/cursor after replacement has started. | Explicit clock ownership prevents old instances resetting a newer owner. | Pure flow assertions do not execute Unity callback ordering. |
| Resume UI input may be interpreted as combat input in the same frame. | Block gameplay on the resume frame. | Authored pause/resume case. |
| Batch script reuses output paths and only checks process exits; Builder runs in the user's original project. | Fresh timestamp/UUID evidence + isolated committed snapshot; verify all required NUnit cases/suites and build receipt; timeouts. | 23 fixture tests ran locally; no actual engine was available. |
| Validation standalone can start in another enabled scene. | Build only the arena into an absolute path under the isolated project's Assets parent. | Real BuildReport/launch still required. |
| Progress labels diverged between files; old percentages looked current. | Use QUALITY_GATES labels; mark percentages historical and R0 as CHECK_ACTIONS. | Current CI receipts remain authoritative. |

## Source preservation

Source was retrieved through a read-only Actions committed-source artifact for PR head `22063f22bfac7fd52e492a313291717253e89b19`; archive checkout is PR merge `0e0533f45342a94e69e9a609e276e5203fc06f80`. Subsequent publication must use the current feature parent without force and preserve unrelated paths. No local user source was reset or overwritten.

## Tests and evidence

- Local Python coordinator suite: 23 tests PASS. They use fake XML/build fixtures, not Unity output.
- Local offline gladiator data validator: PASS on model SHA-256 `3c2d09fe17dc686f13180f8f33cd5150014f602b05326d90b2b52e5b8eb56ff3`.
- Existing CombatRulesHarness extended with 18 pure SessionFlow assertions; rely on actual resulting CI logs for execution status.
- `ArenaIntegrationTests`: 3 authored Unity cases (pause/resume/material retention; repeated own-scene restart; scripted three-wave lifecycle). Scripted enemy removal is NOT combat feel testing.
- Existing smoke tests now use readiness predicates and real-time deadlines instead of arbitrary frame counts/scaled-time-only waits.
- Assistant coordinator execution: **BLOCKED (exit 2): Unity executable unavailable**. U1-U7 remain NOT_RUN; no fabricated .meta, .mat, package lock, scene or screenshot is committed.

## Local handoff

Pull the feature branch while preserving local edits. The existing generated scene needs an explicit Builder pass to create the referenced runtime material. Prefer the isolated coordinator described in `LOCAL_VALIDATION_WORKPACK.md`; it retains genuine Unity-generated artifacts for review. Do not overwrite an edited original scene with that snapshot without reviewing its diff.

## Engine references

- Unity Shader.Find: https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Shader.Find.html (name-only runtime lookup does not guarantee build inclusion).
- Unity Test Framework command line: https://docs.unity3d.com/Packages/com.unity.test-framework@1.4/manual/reference-command-line.html (test runner lifecycle and result files).

Known gaps: actual Unity compilation, new material import, all skin deformations, collision/timing, controller input, camera/AI feel, standalone launch and long-session performance. Keep PR Draft.
