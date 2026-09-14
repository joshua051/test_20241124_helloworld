using IronSand.Art;
using UnityEngine;

namespace IronSand.Combat
{
    public sealed class ProceduralCombatRig : MonoBehaviour
    {
        private Transform torso, head, leftArm, rightArm, leftLeg, rightLeg, shield, weaponSocket;
        private ImportedGladiatorVisual imported;
        private bool built, playerTeam, guarding, executionActor;
        private float locomotion, attackProgress, hitReaction, vulnerableAmount, executionProgress, walkPhase;
        private CombatPhase attackPhase;
        private AttackKind attackKind;
        public bool UsesImportedModel => imported != null;
        public Transform WeaponSocket { get { EnsureBuilt(); return weaponSocket; } }
        private void Awake() { EnsureBuilt(); }
        public void ConfigureTeam(bool isPlayer) { playerTeam = isPlayer; EnsureBuilt(); ApplyTeamColor(); }
        public void SetMotion(float amount, bool isGuarding) { locomotion = Mathf.Clamp01(amount); guarding = isGuarding; }
        public void SetAttack(CombatPhase phase, AttackKind kind, float normalized) { attackPhase = phase; attackKind = kind; attackProgress = Mathf.Clamp01(normalized); }
        public void ClearAttack() { attackPhase = CombatPhase.Idle; attackProgress = 0f; }
        public void TriggerHit(bool heavy) { hitReaction = Mathf.Max(hitReaction, heavy ? 1f : 0.55f); }
        public void SetVulnerable(bool value) { vulnerableAmount = Mathf.MoveTowards(vulnerableAmount, value ? 1f : 0f, Time.deltaTime * 5f); }
        public void SetExecution(bool actor, float progress) { executionActor = actor; executionProgress = Mathf.Clamp01(progress); }
        public void ClearExecution() { executionProgress = 0f; }
        private void LateUpdate()
        {
            EnsureBuilt();
            if (!CombatFreezeSystem.IsFrozen)
            {
                hitReaction = Mathf.MoveTowards(hitReaction, 0f, Time.deltaTime * 4.5f);
                walkPhase += Time.deltaTime * 9f;
            }
            if (imported != null)
            {
                imported.ApplyPose(walkPhase, locomotion, guarding, attackPhase, attackProgress,
                    hitReaction, vulnerableAmount, executionProgress, executionActor);
                return;
            }
            // Keep the existing graybox fallback for a missing/invalid asset. Never call it imported art.
            float walk = Mathf.Sin(walkPhase) * 24f * locomotion;
            float torsoYaw = 0f;
            float torsoRoll = -hitReaction * 16f - vulnerableAmount * 12f;
            float rightX = -12f, rightZ = -8f, leftX = -8f, leftZ = 8f;
            if (guarding) { leftX = -50f; leftZ = -42f; rightX = -32f; rightZ = 22f; }
            if (attackPhase != CombatPhase.Idle)
            {
                float signed = attackKind == AttackKind.Heavy ? 1f : -1f;
                float swing = Mathf.Sin(attackProgress * Mathf.PI);
                torsoYaw += Mathf.Lerp(-36f * signed, 42f * signed, attackProgress);
                rightX = Mathf.Lerp(-78f, 34f, attackProgress);
                rightZ = Mathf.Lerp(70f * signed, -65f * signed, attackProgress) * Mathf.Max(0.35f, swing);
            }
            if (executionProgress > 0f)
            {
                if (executionActor)
                {
                    torsoYaw = Mathf.Lerp(-28f, 26f, executionProgress);
                    rightX = executionProgress < 0.5f ? Mathf.Lerp(-25f, -110f, executionProgress * 2f) : Mathf.Lerp(-110f, 42f, (executionProgress - 0.5f) * 2f);
                    rightZ = Mathf.Lerp(40f, -55f, executionProgress);
                }
                else { torsoRoll = Mathf.Lerp(-18f, -48f, executionProgress); leftX = rightX = Mathf.Lerp(-18f, 28f, executionProgress); }
            }
            torso.localRotation = Quaternion.Euler(0f, torsoYaw, torsoRoll);
            head.localRotation = Quaternion.Euler(vulnerableAmount * 12f, -torsoYaw * 0.2f, hitReaction * 8f);
            leftArm.localRotation = Quaternion.Euler(leftX, 0f, leftZ);
            rightArm.localRotation = Quaternion.Euler(rightX, 0f, rightZ);
            leftLeg.localRotation = Quaternion.Euler(walk, 0f, 0f);
            rightLeg.localRotation = Quaternion.Euler(-walk, 0f, 0f);
            if (shield != null) shield.gameObject.SetActive(playerTeam);
        }
        private void EnsureBuilt()
        {
            if (built) return;
            built = true;
            MeshRenderer rootRenderer = GetComponent<MeshRenderer>();
            if (rootRenderer != null) rootRenderer.enabled = false;
            Transform oldVisual = transform.Find("BodyVisual");
            if (oldVisual != null) { Renderer renderer = oldVisual.GetComponent<Renderer>(); if (renderer != null) renderer.enabled = false; }
            if (ImportedGladiatorVisual.TryAttach(transform, playerTeam, out imported))
            {
                weaponSocket = imported.WeaponSocket;
                return;
            }
            Debug.LogWarning("Gladiator mesh resource unavailable; using the primitive fallback.", this);
            torso = CreatePart("Rig_Torso", PrimitiveType.Cube, transform, new Vector3(0f, 0.18f, 0f), new Vector3(0.72f, 0.75f, 0.42f));
            head = CreatePart("Rig_Head", PrimitiveType.Sphere, torso, new Vector3(0f, 0.78f, 0f), new Vector3(0.42f, 0.46f, 0.42f));
            leftArm = CreatePart("Rig_LeftArm", PrimitiveType.Cube, torso, new Vector3(-0.56f, 0.22f, 0f), new Vector3(0.22f, 0.78f, 0.22f));
            rightArm = CreatePart("Rig_RightArm", PrimitiveType.Cube, torso, new Vector3(0.56f, 0.22f, 0f), new Vector3(0.22f, 0.78f, 0.22f));
            leftLeg = CreatePart("Rig_LeftLeg", PrimitiveType.Cube, transform, new Vector3(-0.22f, -0.52f, 0f), new Vector3(0.28f, 0.82f, 0.30f));
            rightLeg = CreatePart("Rig_RightLeg", PrimitiveType.Cube, transform, new Vector3(0.22f, -0.52f, 0f), new Vector3(0.28f, 0.82f, 0.30f));
            weaponSocket = new GameObject("WeaponSocket").transform;
            weaponSocket.SetParent(rightArm, false);
            weaponSocket.localPosition = new Vector3(0f, -0.52f, 0.16f);
            shield = CreatePart("Buckler", PrimitiveType.Cylinder, leftArm, new Vector3(0f, -0.32f, 0.28f), new Vector3(0.46f, 0.08f, 0.46f));
            shield.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ApplyTeamColor();
        }
        private static Transform CreatePart(string name, PrimitiveType primitive, Transform parent, Vector3 localPosition, Vector3 scale)
        {
            GameObject part = GameObject.CreatePrimitive(primitive);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = scale;
            Collider collider = part.GetComponent<Collider>();
            if (collider != null) { collider.enabled = false; if (Application.isPlaying) Destroy(collider); else DestroyImmediate(collider); }
            return part.transform;
        }
        private void ApplyTeamColor()
        {
            if (!built) return;
            if (imported != null) { imported.ConfigureTeam(playerTeam); return; }
            Color color = playerTeam ? new Color(0.28f, 0.44f, 0.62f) : new Color(0.58f, 0.28f, 0.20f);
            foreach (Renderer renderer in GetComponentsInChildren<Renderer>(true))
            {
                if (renderer.transform.name.StartsWith("WeaponVisual_")) continue;
                if (renderer.material != null) renderer.material.color = color;
            }
        }
    }
}
