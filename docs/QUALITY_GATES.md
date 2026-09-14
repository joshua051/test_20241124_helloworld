# Quality Gates

Iron Sand Arena uses evidence-based gates. A later gate cannot erase failure of an earlier gate, and repository content is never treated as proof that Unity executed successfully.

## R0 — Repository integrity

Automated by `.github/workflows/repository-guard.yml`.

Required:
- no tracked Unity generated directories (`Library`, `Temp`, `Obj`, `Logs`, `UserSettings`, `MemoryCaptures`)
- no case-colliding paths
- no merge-conflict markers in tracked text
- no tracked file above 20 MiB without an intentional asset/LFS decision
- required architecture/validation documents present
- Unity Editor version pinned to `6000.3.23f1`

## U1 — Unity import

Required evidence:
- project opens in Unity `6000.3.23f1`
- package resolution completes
- generated `.meta`, `Packages/packages-lock.json`, and material `ProjectSettings` changes are captured for review

Status: **NOT_RUN**

## U2 — Compile

Required evidence:
- zero C# compile errors
- zero package/compiler exceptions
- generated arena scene can be rebuilt from the explicit menu command

Status: **NOT_RUN**

## U3 — Automated tests

Required evidence:
- all EditMode tests pass
- failures are fixed, never waived without written rationale

Status: **NOT_RUN**

## U4 — Play Mode functional gate

Required evidence:
- movement/camera/guard/dodge/light/heavy inputs function
- lock-on acquires/releases valid enemies and camera remains usable
- starter weapon pickup/swap works
- weapon durability decreases only after successful hits and can break to Unarmed
- defeated enemies drop usable weapons
- style combo expires and varied attacks/weapons score better than immediate repetition
- crowd rewards trigger at thresholds
- attack-token concurrency remains bounded
- all three waves reach Victory
- no recurring exceptions over a 10-minute run

Status: **NOT_RUN**

## U5 — Combat-feel gate

This is qualitative but still evidence-driven. Record concrete defects for:
- camera occlusion or nausea
- input latency
- hit readability
- crowding/body blocking
- dodge distance and invulnerability feel
- weapon differentiation
- combo incentive versus repetitive optimal play
- enemy windup readability

Status: **NOT_RUN**

## U6 — Production-content gate

Not applicable to this graybox milestone. Root-motion animation, authored hitboxes, production art/VFX/audio, accessibility, gamepad support, save progression, executions, perfect guard, disarm, throwing, and bosses remain future work.
