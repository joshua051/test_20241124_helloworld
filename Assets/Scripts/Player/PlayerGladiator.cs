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
        [SerializeField, Min(0.1f)] private float attackRadius = 1.4f;
        [SerializeField, Min(0.1f)] private float attackReach = 1.5f;
        [SerializeField, Min(0f)] private float lightDamage = 24f;
        [SerializeField, Min(0f)] private float heavyDamage = 42f;
        [SerializeField, Min(0f)] private float attackCooldown = 0.45f;
        [SerializeField, Range(0f, 1f)] private float guardDamageMultiplier = 0.3f;
        [SerializeField, Min(0f)] private float dodgeDistance = 3.2f;
        [SerializeField, Min(0.05f)] private float dodgeDuration = 0.3f;
        [SerializeField, Min(0.1f)] private float dodgeCooldown = 0.8f;
        [SerializeField, Min(0f)] private float dodgeInvulnerability = 0.22f;
        [SerializeField, Min(1f)] private float lockOnRange = 14f;
        [SerializeField, Min(0.5f)] private float pickupRadius = 2.1f;

        private CharacterController controller;
        private PlayerWeaponController weaponController;
        private Camera gameplayCamera;
        private ArenaDirector arenaDirector;
        private ArenaSession session;
        private readonly DodgeState dodge = new();
        private Vector3 dodgeDirection;
        private float verticalVelocity;
        private float attackCooldownRemaining;
        private bool guarding;
        private EnemyGladiator lockTarget;
        public bool Guarding => guarding;
        public bool IsDodging => dodge.IsActive;
        public float DodgeCooldownRemaining => dodge.CooldownRemaining;
        public EnemyGladiator LockTarget => lockTarget;
        public PlayerWeaponController WeaponController => weaponController;

        protected override void Awake()
        {
            base.Awake();
            controller = GetComponent<CharacterController>();
            weaponController = GetComponent<PlayerWeaponController>();
        }

        private void Start()
        {
            gameplayCamera = Camera.main;
            arenaDirector = FindFirstObjectByType<ArenaDirector>();
            session = FindFirstObjectByType<ArenaSession>();
        }

        protected override void Update()
        {
            base.Update();
            if (IsDead || Time.deltaTime <= 0f || (session != null && !session.GameplayActive))
            {
                guarding = false;
                return;
            }
            if (transform.position.y < -5f)
            {
                ApplyDamage(MaxHealth + 1f, Vector3.zero);
                return;
            }
#if ENABLE_LEGACY_INPUT_MANAGER
            attackCooldownRemaining = Mathf.Max(0f, attackCooldownRemaining - Time.deltaTime);
            ValidateLockTarget();
            bool wasDodging = dodge.IsActive;
            bool free = !IsStunned && !wasDodging && attackCooldownRemaining <= 0f;
            if (Input.GetKeyDown(KeyCode.Tab)) ToggleLockOn();
            Vector3 move = ReadMoveDirection();
            guarding = free && Input.GetKey(KeyCode.Q);
            if (free && !guarding && controller.isGrounded && Input.GetKeyDown(KeyCode.Space))
            {
                float duration = Mathf.Max(0.05f, dodgeDuration);
                if (dodge.TryStart(duration, Mathf.Max(duration + 0.1f, dodgeCooldown)))
                {
                    dodgeDirection = move.sqrMagnitude > 0.01f ? move.normalized : -transform.forward;
                    GrantInvulnerability(Mathf.Min(dodgeInvulnerability, duration * 0.8f));
                    wasDodging = true;
                }
            }
            float activeSeconds = dodge.Tick(Time.deltaTime);
            if (free && !guarding && !wasDodging)
            {
                if (Input.GetKeyDown(KeyCode.E)) TryPickupWeapon();
                else if (Input.GetMouseButtonDown(0)) PerformAttack(AttackKind.Light);
                else if (Input.GetMouseButtonDown(1)) PerformAttack(AttackKind.Heavy);
            }
            Move(move, activeSeconds, wasDodging);
#endif
        }

        private Vector3 ReadMoveDirection()
        {
            // No implicit Horizontal/Vertical axis asset dependency.
            float x = (Input.GetKey(KeyCode.D) ? 1f : 0f) - (Input.GetKey(KeyCode.A) ? 1f : 0f);
            float y = (Input.GetKey(KeyCode.W) ? 1f : 0f) - (Input.GetKey(KeyCode.S) ? 1f : 0f);
            Vector3 forward = gameplayCamera != null ? gameplayCamera.transform.forward : Vector3.forward;
            forward.y = 0f;
            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward);
            return Vector3.ClampMagnitude(forward * y + right * x, 1f);
        }

        private void Move(Vector3 move, float activeSeconds, bool wasDodging)
        {
            Vector3 displacement = Vector3.zero;
            if (!IsStunned)
            {
                Vector3 facing = lockTarget != null ? lockTarget.transform.position - transform.position : move;
                facing.y = 0f;
                if (!wasDodging && facing.sqrMagnitude > 0.01f)
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(facing),
                        1f - Mathf.Exp(-rotationSharpness * Time.deltaTime));
                if (wasDodging)
                    displacement = dodgeDirection * (dodgeDistance / Mathf.Max(0.05f, dodgeDuration)) * activeSeconds;
                else
                {
                    float speed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : moveSpeed;
                    if (guarding || attackCooldownRemaining > 0f) speed = moveSpeed * 0.45f;
                    displacement = move * (speed * Time.deltaTime);
                }
            }
            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            verticalVelocity += gravity * Time.deltaTime;
            displacement.y = verticalVelocity * Time.deltaTime;
            controller.Move(displacement);
        }

        public override bool ApplyDamage(float amount, Vector3 knockback, float stunMultiplier = 1f)
        {
            return base.ApplyDamage(guarding ? amount * guardDamageMultiplier : amount, knockback,
                guarding ? stunMultiplier * 0.35f : stunMultiplier);
        }

        private void PerformAttack(AttackKind kind)
        {
            WeaponStats stats = weaponController.CurrentStats;
            WeaponArchetype weapon = weaponController.CurrentWeapon;
            float damage = kind == AttackKind.Heavy ? heavyDamage * stats.HeavyDamageMultiplier : lightDamage * stats.LightDamageMultiplier;
            float stun = (kind == AttackKind.Heavy ? 1.5f : 1f) * stats.StunMultiplier;
            attackCooldownRemaining = attackCooldown * stats.CooldownMultiplier * (kind == AttackKind.Heavy ? 1.25f : 1f);
            // Root is the torso center, not the feet. Do not offset this up by another metre.
            Vector3 center = transform.position + transform.forward * (attackReach * stats.ReachMultiplier);
            Collider[] hits = Physics.OverlapSphere(center, attackRadius, ~0, QueryTriggerInteraction.Ignore);
            HashSet<EnemyGladiator> struck = new();
            bool hitAny = false;
            foreach (Collider hit in hits)
            {
                EnemyGladiator enemy = hit.GetComponentInParent<EnemyGladiator>();
                if (enemy == null || enemy.IsDead || !struck.Add(enemy)) continue;
                Vector3 toEnemy = enemy.transform.position - transform.position;
                toEnemy.y = 0f;
                if (toEnemy.sqrMagnitude < 0.001f || Vector3.Dot(transform.forward, toEnemy.normalized) < 0.15f) continue;
                if (CombatQueries.WorldBlocks(transform.position, enemy.transform.position)) continue;
                if (enemy.ApplyDamage(damage, toEnemy.normalized * 2.5f, stun))
                {
                    hitAny = true;
                    arenaDirector?.RegisterPlayerHit(kind, weapon, enemy.IsDead, stats.FavorBonus);
                }
            }
            if (hitAny) weaponController.ConsumeDurability(kind == AttackKind.Heavy ? 2 : 1);
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
            drop.y = 0.35f; // This prototype's arena floor is y=0.
            weaponController.TryPickupNearest(transform.position, pickupRadius, drop);
        }

        protected override void OnDamaged(Vector3 knockback)
        {
            guarding = false;
            dodge.CancelActive();
            if (controller != null && controller.enabled && knockback.sqrMagnitude > 0f)
                controller.Move(knockback * 0.08f);
        }

        protected override void OnDeath()
        {
            guarding = false;
            lockTarget = null;
            dodge.CancelActive();
        }
    }
}
