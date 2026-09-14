using System.Collections.Generic;
using IronSand.Arena;
using IronSand.Combat;
using IronSand.Enemy;
using UnityEngine;

namespace IronSand.Player
{
    [RequireComponent(typeof(CharacterController), typeof(PlayerWeaponController))]
    public sealed class PlayerGladiator : Combatant
    {
        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 5f;
        [SerializeField, Min(0f)] private float sprintSpeed = 7.5f;
        [SerializeField, Min(0f)] private float rotationSharpness = 14f;
        [SerializeField] private float gravity = -25f;
        [Header("Combat")]
        [SerializeField, Min(0.1f)] private float lightDamage = 24f;
        [SerializeField, Min(0.1f)] private float heavyDamage = 42f;
        [SerializeField, Range(0f, 1f)] private float guardDamageMultiplier = 0.25f;
        [SerializeField, Min(0.05f)] private float perfectGuardWindow = 0.16f;
        [SerializeField, Range(30f, 180f)] private float guardArcDegrees = 118f;
        [SerializeField, Min(0f)] private float dodgeDistance = 3.2f;
        [SerializeField, Min(0.05f)] private float dodgeDuration = 0.3f;
        [SerializeField, Min(0.1f)] private float dodgeCooldown = 0.8f;
        [SerializeField, Min(0f)] private float dodgeInvulnerability = 0.22f;
        [SerializeField, Min(1f)] private float lockOnRange = 14f;
        [SerializeField, Min(0.5f)] private float pickupRadius = 2.1f;
        [SerializeField, Min(0.5f)] private float executionRange = 2.35f;

        private CharacterController controller;
        private PlayerWeaponController weaponController;
        private Camera gameplayCamera;
        private ArenaDirector arenaDirector;
        private ArenaSession session;
        private ProceduralCombatRig rig;
        private GuardState guard;
        private readonly DodgeState dodge = new();
        private readonly AttackTimeline attack = new();
        private readonly ExecutionState execution = new();
        private readonly HashSet<EnemyGladiator> struckThisAttack = new();
        private Vector3 dodgeDirection;
        private float verticalVelocity;
        private EnemyGladiator lockTarget;
        private EnemyGladiator executionTarget;
        private AttackKind currentAttackKind;
        private AttackProfile currentAttackProfile;
        private WeaponArchetype currentAttackWeapon;
        private WeaponStats currentAttackStats;
        private bool durabilityConsumedThisAttack;
        private bool executionStrikeApplied;

        public bool Guarding => guard != null && guard.IsGuarding;
        public bool IsDodging => dodge.IsActive;
        public bool IsAttacking => attack.IsRunning;
        public bool IsExecuting => execution.IsActive;
        public float DodgeCooldownRemaining => dodge.CooldownRemaining;
        public EnemyGladiator LockTarget => lockTarget;
        public PlayerWeaponController WeaponController => weaponController;

        protected override void Awake()
        {
            base.Awake();
            controller = GetComponent<CharacterController>();
            weaponController = GetComponent<PlayerWeaponController>();
            guard = new GuardState(Mathf.Max(0.05f, perfectGuardWindow), Mathf.Clamp(guardArcDegrees, 30f, 180f));
        }

        private void Start()
        {
            gameplayCamera = Camera.main;
            arenaDirector = FindFirstObjectByType<ArenaDirector>();
            session = FindFirstObjectByType<ArenaSession>();
            rig = GetComponent<ProceduralCombatRig>() ?? gameObject.AddComponent<ProceduralCombatRig>();
            rig.ConfigureTeam(true);
            weaponController.SetVisualParent(rig.WeaponSocket);
        }

        protected override void Update()
        {
            base.Update();
            if (IsDead || Time.deltaTime <= 0f || (session != null && !session.GameplayActive))
            {
                guard?.End();
                rig?.SetMotion(0f, false);
                return;
            }
            if (CombatFreezeSystem.IsFrozen) return;
            if (transform.position.y < -5f)
            {
                ApplyDamage(MaxHealth + 1f, Vector3.zero);
                return;
            }
#if ENABLE_LEGACY_INPUT_MANAGER
            ValidateLockTarget();
            if (Input.GetKeyDown(KeyCode.Tab)) ToggleLockOn();
            Vector3 move = ReadMoveDirection();
            if (execution.IsActive)
            {
                UpdateExecution();
                return;
            }

            bool wasAttacking = attack.IsRunning;
            AttackProfile movementAttackProfile = currentAttackProfile;
            float rootMotionDelta = wasAttacking ? TickAttack() : 0f;
            bool wasDodging = dodge.IsActive;
            float dodgeSeconds = dodge.Tick(Time.deltaTime);
            bool busy = IsStunned || wasAttacking || wasDodging;

            if (!busy)
            {
                if (Input.GetKeyDown(KeyCode.Q)) guard.Begin(Time.time);
                else if (Input.GetKey(KeyCode.Q) && !Guarding) guard.Begin(Time.time - perfectGuardWindow - 0.01f);
                else if (!Input.GetKey(KeyCode.Q)) guard.End();
            }
            else guard.End();

            bool free = !IsStunned && !wasAttacking && !attack.IsRunning && !dodge.IsActive && !Guarding;
            if (free)
            {
                if (Input.GetKeyDown(KeyCode.F) && TryStartExecution())
                {
                    UpdateExecution();
                    return;
                }
                if (Input.GetKeyDown(KeyCode.G)) TryThrowWeapon();
                else if (controller.isGrounded && Input.GetKeyDown(KeyCode.Space)) TryStartDodge(move, ref wasDodging);
                else if (Input.GetKeyDown(KeyCode.E)) TryPickupWeapon();
                else if (Input.GetMouseButtonDown(0)) StartAttack(AttackKind.Light);
                else if (Input.GetMouseButtonDown(1)) StartAttack(AttackKind.Heavy);
            }

            Move(move, dodgeSeconds, wasDodging, wasAttacking, rootMotionDelta, movementAttackProfile);
            rig?.SetMotion(move.magnitude, Guarding);
#endif
        }

        public override ImpactResult ReceiveImpact(CombatImpact impact)
        {
            if (execution.IsActive && !impact.Execution) return ImpactResult.Ignored;
            if (Guarding && !impact.Unblockable && impact.Source != null)
            {
                Vector3 toSource = impact.Source.transform.position - transform.position;
                GuardResolution resolution = guard.Resolve(transform.forward, toSource, Time.time);
                if (resolution == GuardResolution.Perfect)
                {
                    if (impact.Source is EnemyGladiator enemy) enemy.ApplyPerfectGuardCounter(this);
                    arenaDirector?.RegisterPlayerHit(AttackKind.PerfectGuard, weaponController.CurrentWeapon, false, 2);
                    CombatFeedbackSystem.EmitPerfectGuard(transform.position + Vector3.up * 0.45f + transform.forward * 0.35f);
                    return ImpactResult.PerfectBlocked;
                }
                if (resolution == GuardResolution.Block)
                {
                    ImpactResult blocked = base.ReceiveImpact(impact.Scale(guardDamageMultiplier, 0.32f, 0.24f, 0.32f));
                    if (!IsDead) guard.Begin(Time.time - perfectGuardWindow - 0.01f);
                    return blocked.AsGuarded();
                }
            }
            return base.ReceiveImpact(impact);
        }

        private bool StartAttack(AttackKind kind)
        {
            currentAttackKind = kind;
            currentAttackWeapon = weaponController.CurrentWeapon;
            currentAttackStats = weaponController.CurrentStats;
            currentAttackProfile = AttackLibrary.Get(currentAttackWeapon, kind);
            if (!attack.TryStart(currentAttackProfile)) return false;
            struckThisAttack.Clear();
            durabilityConsumedThisAttack = false;
            FaceLockTargetImmediately();
            rig?.SetAttack(attack.Phase, kind, attack.Normalized);
            return true;
        }

        private float TickAttack()
        {
            float rootDelta = attack.Tick(Time.deltaTime);
            if (attack.IsRunning)
            {
                rig?.SetAttack(attack.Phase, currentAttackKind, attack.Normalized);
                if (attack.Phase == CombatPhase.Active) ResolveWeaponSweep();
            }
            else rig?.ClearAttack();
            return rootDelta;
        }

        private void ResolveWeaponSweep()
        {
            WeaponArchetype weapon = currentAttackWeapon;
            WeaponStats stats = currentAttackStats;
            WeaponSweep.GetSegment(transform, weapon, currentAttackKind, attack.PhaseProgress, out Vector3 grip, out Vector3 tip);
            Collider[] hits = Physics.OverlapCapsule(grip, tip, currentAttackProfile.SweepRadius, ~0, QueryTriggerInteraction.Ignore);
            bool heavy = currentAttackKind == AttackKind.Heavy;
            foreach (Collider hit in hits)
            {
                EnemyGladiator enemy = hit.GetComponentInParent<EnemyGladiator>();
                if (enemy == null || enemy.IsDead || !struckThisAttack.Add(enemy)) continue;
                if (CombatQueries.WorldBlocks(transform.position, enemy.transform.position)) continue;
                Vector3 toEnemy = enemy.transform.position - transform.position;
                toEnemy.y = 0f;
                if (toEnemy.sqrMagnitude < 0.001f) continue;
                float baseDamage = heavy ? heavyDamage : lightDamage;
                float weaponDamage = heavy ? stats.HeavyDamageMultiplier : stats.LightDamageMultiplier;
                CombatImpact impact = new(this,
                    baseDamage * weaponDamage * currentAttackProfile.DamageMultiplier,
                    currentAttackProfile.PoiseDamage * stats.StunMultiplier,
                    toEnemy.normalized * (heavy ? 3.5f : 2.1f),
                    stats.StunMultiplier * (heavy ? 1.45f : 1f));
                ImpactResult result = enemy.ReceiveImpact(impact);
                if (!result.Accepted) continue;
                arenaDirector?.RegisterPlayerHit(currentAttackKind, weapon, result.Killed, stats.FavorBonus);
                CombatFeedbackSystem.EmitImpact(enemy.transform.position + Vector3.up * 0.38f,
                    currentAttackProfile.HitStopSeconds, currentAttackProfile.CameraShake, heavy);
                if (!durabilityConsumedThisAttack)
                {
                    durabilityConsumedThisAttack = true;
                    weaponController.ConsumeDurability(heavy ? 2 : 1);
                }
            }
        }

        private bool TryStartExecution()
        {
            if (lockTarget == null || lockTarget.IsDead || !lockTarget.ExecutionReady) return false;
            Vector3 delta = lockTarget.transform.position - transform.position;
            delta.y = 0f;
            if (delta.sqrMagnitude > executionRange * executionRange || CombatQueries.WorldBlocks(transform.position, lockTarget.transform.position)) return false;
            if (!execution.TryStart(1.10f, 0.56f)) return false;
            executionTarget = lockTarget;
            executionStrikeApplied = false;
            attack.Cancel();
            dodge.CancelActive();
            guard.End();
            rig?.ClearAttack();
            return true;
        }

        private void UpdateExecution()
        {
            if (executionTarget == null || (executionTarget.IsDead && !executionStrikeApplied))
            {
                AbortExecution();
                return;
            }

            Vector3 delta = executionTarget.transform.position - transform.position;
            delta.y = 0f;
            if (delta.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(delta.normalized), 1f - Mathf.Exp(-18f * Time.deltaTime));
                if (!executionStrikeApplied && delta.magnitude > 1.25f && controller.enabled)
                    controller.Move(delta.normalized * Mathf.Min(2.5f * Time.deltaTime, delta.magnitude - 1.25f));
            }

            ExecutionTick tick = execution.Tick(Time.deltaTime);
            rig?.SetExecution(true, tick.Progress);
            executionTarget.SetExecutionPose(tick.Progress);
            ApplyGravityOnly();

            if (tick.StrikeNow && !executionStrikeApplied && !executionTarget.IsDead)
            {
                WeaponArchetype weapon = weaponController.CurrentWeapon;
                ImpactResult result = executionTarget.ReceiveImpact(new CombatImpact(this,
                    executionTarget.MaxHealth + 999f,
                    executionTarget.MaxPoise + 999f,
                    transform.forward * 2f,
                    3f,
                    true,
                    true));
                executionStrikeApplied = result.Killed;
                if (result.Killed)
                {
                    arenaDirector?.RegisterPlayerHit(AttackKind.Execution, weapon, true, 4);
                    CombatFeedbackSystem.EmitExecution(executionTarget.transform.position + Vector3.up * 0.35f);
                }
            }

            if (tick.Completed)
                CompleteExecution();
        }

        private void CompleteExecution()
        {
            rig?.ClearExecution();
            executionTarget?.ClearExecutionPose();
            executionTarget = null;
            executionStrikeApplied = false;
        }

        private void AbortExecution()
        {
            execution.Cancel();
            CompleteExecution();
        }

        private void TryStartDodge(Vector3 move, ref bool wasDodging)
        {
            float duration = Mathf.Max(0.05f, dodgeDuration);
            if (!dodge.TryStart(duration, Mathf.Max(duration + 0.1f, dodgeCooldown))) return;
            dodgeDirection = move.sqrMagnitude > 0.01f ? move.normalized : -transform.forward;
            GrantInvulnerability(Mathf.Min(dodgeInvulnerability, duration * 0.8f));
            guard.End();
            wasDodging = true;
        }

        private void TryThrowWeapon()
        {
            Vector3 direction = transform.forward;
            if (lockTarget != null)
                direction = lockTarget.transform.position + Vector3.up * 0.25f - (transform.position + Vector3.up * 0.35f);
            Vector3 origin = transform.position + Vector3.up * 0.38f + transform.forward * 0.72f;
            weaponController.TryThrow(origin, direction, this);
        }

        private Vector3 ReadMoveDirection()
        {
            float x = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
            float y = (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);
            Vector3 forward = gameplayCamera != null ? gameplayCamera.transform.forward : Vector3.forward;
            forward.y = 0f;
            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            return Vector3.ClampMagnitude(forward * y + right * x, 1f);
        }

        private void Move(Vector3 move, float dodgeSeconds, bool wasDodging, bool wasAttacking, float rootMotionDelta, AttackProfile movementAttackProfile)
        {
            Vector3 displacement = Vector3.zero;
            if (!IsStunned)
            {
                Vector3 facing = lockTarget != null ? lockTarget.transform.position - transform.position : move;
                facing.y = 0f;
                if (!wasDodging && facing.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(facing), 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime));
                if (wasDodging)
                    displacement = dodgeDirection * (dodgeDistance / Mathf.Max(0.05f, dodgeDuration)) * dodgeSeconds;
                else if (wasAttacking)
                    displacement = transform.forward * (movementAttackProfile.RootMotionDistance * rootMotionDelta) + move * (moveSpeed * 0.12f * Time.deltaTime);
                else
                {
                    float speed = Input.GetKey(KeyCode.LeftShift) && !Guarding ? sprintSpeed : moveSpeed;
                    if (Guarding) speed *= 0.52f;
                    displacement = move * (speed * Time.deltaTime);
                }
            }
            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            verticalVelocity += gravity * Time.deltaTime;
            displacement.y = verticalVelocity * Time.deltaTime;
            controller.Move(displacement);
        }

        private void ApplyGravityOnly()
        {
            if (controller == null || !controller.enabled) return;
            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            verticalVelocity += gravity * Time.deltaTime;
            controller.Move(Vector3.up * (verticalVelocity * Time.deltaTime));
        }

        private void ToggleLockOn()
        {
            if (lockTarget != null) { lockTarget = null; return; }
            if (arenaDirector == null) return;
            Vector3 cameraForward = gameplayCamera != null ? gameplayCamera.transform.forward : transform.forward;
            cameraForward.y = 0f;
            cameraForward.Normalize();
            float bestScore = float.PositiveInfinity;
            foreach (EnemyGladiator enemy in arenaDirector.AliveEnemies)
            {
                if (enemy == null || enemy.IsDead) continue;
                Vector3 delta = enemy.transform.position - transform.position;
                float distance = delta.magnitude;
                if (distance <= 0.01f || distance > lockOnRange) continue;
                if (CombatQueries.WorldBlocks(transform.position, enemy.transform.position)) continue;
                float score = distance + (1f - Vector3.Dot(cameraForward, delta / distance)) * 3f;
                if (score < bestScore) { bestScore = score; lockTarget = enemy; }
            }
        }

        private void ValidateLockTarget()
        {
            if (lockTarget == null) return;
            Vector3 delta = lockTarget.transform.position - transform.position;
            if (lockTarget.IsDead || delta.sqrMagnitude > lockOnRange * lockOnRange * 1.8f)
                lockTarget = null;
        }

        private void TryPickupWeapon()
        {
            Vector3 drop = transform.position - transform.forward * 1.15f;
            drop.y = 0.35f;
            weaponController.TryPickupNearest(transform.position, pickupRadius, drop);
        }

        private void FaceLockTargetImmediately()
        {
            if (lockTarget == null) return;
            Vector3 delta = lockTarget.transform.position - transform.position;
            delta.y = 0f;
            if (delta.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.LookRotation(delta.normalized);
        }

        protected override void OnDamaged(Vector3 knockback)
        {
            guard?.End();
            attack.Cancel();
            dodge.CancelActive();
            if (execution.IsActive) AbortExecution();
            rig?.ClearAttack();
            rig?.TriggerHit(LastPoiseBroken);
            if (controller != null && controller.enabled && knockback.sqrMagnitude > 0f)
                controller.Move(knockback * 0.08f);
        }

        protected override void OnDeath()
        {
            guard?.End();
            lockTarget = null;
            attack.Cancel();
            dodge.CancelActive();
            if (execution.IsActive) AbortExecution();
            rig?.TriggerHit(true);
        }
    }
}
