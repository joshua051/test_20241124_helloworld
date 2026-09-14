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
        [SerializeField, Min(0f)] private float dodgeDistance = 4.5f;
        [SerializeField, Min(0f)] private float dodgeInvulnerability = 0.3f;
        [SerializeField, Min(1f)] private float lockOnRange = 14f;
        [SerializeField, Min(0.5f)] private float pickupRadius = 2.1f;

        private CharacterController controller;
        private PlayerWeaponController weaponController;
        private Camera gameplayCamera;
        private ArenaDirector arenaDirector;
        private float verticalVelocity;
        private float attackCooldownRemaining;
        private bool guarding;
        private EnemyGladiator lockTarget;

        public bool Guarding => guarding;
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
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        protected override void Update()
        {
            base.Update();
            attackCooldownRemaining = Mathf.Max(0f, attackCooldownRemaining - Time.deltaTime);

            if (IsDead)
            {
                return;
            }

            ValidateLockTarget();

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            if (Input.GetKeyDown(KeyCode.Tab))
            {
                ToggleLockOn();
            }

            if (!IsStunned && Input.GetKeyDown(KeyCode.E))
            {
                TryPickupWeapon();
            }

            guarding = Input.GetKey(KeyCode.Q) && !IsStunned;
            HandleMovement();

            if (!IsStunned && attackCooldownRemaining <= 0f)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    PerformAttack(AttackKind.Light);
                }
                else if (Input.GetMouseButtonDown(1))
                {
                    PerformAttack(AttackKind.Heavy);
                }
            }

            if (!IsStunned && Input.GetKeyDown(KeyCode.Space))
            {
                Dodge();
            }
        }

        public override bool ApplyDamage(float amount, Vector3 knockback, float stunMultiplier = 1f)
        {
            float finalAmount = guarding ? amount * guardDamageMultiplier : amount;
            float finalStun = guarding ? stunMultiplier * 0.35f : stunMultiplier;
            return base.ApplyDamage(finalAmount, knockback, finalStun);
        }

        private void HandleMovement()
        {
            if (controller == null || IsStunned)
            {
                ApplyGravityOnly();
                return;
            }

            Vector2 input = new(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            input = Vector2.ClampMagnitude(input, 1f);

            Vector3 forward = gameplayCamera != null ? gameplayCamera.transform.forward : Vector3.forward;
            Vector3 right = gameplayCamera != null ? gameplayCamera.transform.right : Vector3.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 move = forward * input.y + right * input.x;
            float speed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : moveSpeed;

            Vector3 facing = move;
            if (lockTarget != null)
            {
                facing = lockTarget.transform.position - transform.position;
                facing.y = 0f;
            }

            if (facing.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(facing.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 1f - Mathf.Exp(-rotationSharpness * Time.deltaTime));
            }

            if (controller.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }
            verticalVelocity += gravity * Time.deltaTime;

            Vector3 velocity = move * speed;
            velocity.y = verticalVelocity;
            controller.Move(velocity * Time.deltaTime);
        }

        private void ApplyGravityOnly()
        {
            if (controller == null)
            {
                return;
            }

            if (controller.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }
            verticalVelocity += gravity * Time.deltaTime;
            controller.Move(Vector3.up * (verticalVelocity * Time.deltaTime));
        }

        private void PerformAttack(AttackKind kind)
        {
            WeaponStats stats = weaponController != null
                ? weaponController.CurrentStats
                : WeaponCatalog.Get(WeaponArchetype.Unarmed);
            WeaponArchetype weapon = weaponController != null ? weaponController.CurrentWeapon : WeaponArchetype.Unarmed;

            float damage = kind == AttackKind.Heavy
                ? heavyDamage * stats.HeavyDamageMultiplier
                : lightDamage * stats.LightDamageMultiplier;
            float reach = attackReach * stats.ReachMultiplier;
            float stunMultiplier = (kind == AttackKind.Heavy ? 1.5f : 1f) * stats.StunMultiplier;
            float cooldownMultiplier = stats.CooldownMultiplier * (kind == AttackKind.Heavy ? 1.25f : 1f);
            attackCooldownRemaining = attackCooldown * cooldownMultiplier;

            Vector3 center = transform.position + Vector3.up + transform.forward * reach;
            Collider[] hits = Physics.OverlapSphere(center, attackRadius, ~0, QueryTriggerInteraction.Ignore);
            HashSet<EnemyGladiator> struckEnemies = new();
            bool hitAny = false;

            foreach (Collider hit in hits)
            {
                EnemyGladiator enemy = hit.GetComponentInParent<EnemyGladiator>();
                if (enemy == null || enemy.IsDead || !struckEnemies.Add(enemy))
                {
                    continue;
                }

                Vector3 toEnemy = enemy.transform.position - transform.position;
                toEnemy.y = 0f;
                if (toEnemy.sqrMagnitude < 0.001f || Vector3.Dot(transform.forward, toEnemy.normalized) < 0.15f)
                {
                    continue;
                }

                if (enemy.ApplyDamage(damage, toEnemy.normalized * 2.5f, stunMultiplier))
                {
                    hitAny = true;
                    arenaDirector?.RegisterPlayerHit(kind, weapon, enemy.IsDead, stats.FavorBonus);
                }
            }

            if (hitAny)
            {
                weaponController?.ConsumeDurability(kind == AttackKind.Heavy ? 2 : 1);
            }
        }

        private void Dodge()
        {
            Vector3 direction = transform.forward;
            Vector2 input = new(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (input.sqrMagnitude > 0.05f && gameplayCamera != null)
            {
                Vector3 forward = gameplayCamera.transform.forward;
                Vector3 right = gameplayCamera.transform.right;
                forward.y = 0f;
                right.y = 0f;
                direction = (forward.normalized * input.y + right.normalized * input.x).normalized;
            }

            GrantInvulnerability(dodgeInvulnerability);
            controller.Move(direction * dodgeDistance);
        }

        private void ToggleLockOn()
        {
            if (lockTarget != null)
            {
                lockTarget = null;
                return;
            }

            if (arenaDirector == null)
            {
                arenaDirector = FindFirstObjectByType<ArenaDirector>();
            }

            if (arenaDirector == null)
            {
                return;
            }

            Vector3 cameraForward = gameplayCamera != null ? gameplayCamera.transform.forward : transform.forward;
            cameraForward.y = 0f;
            cameraForward.Normalize();

            EnemyGladiator best = null;
            float bestScore = float.PositiveInfinity;
            foreach (EnemyGladiator enemy in arenaDirector.AliveEnemies)
            {
                if (enemy == null || enemy.IsDead)
                {
                    continue;
                }

                Vector3 delta = enemy.transform.position - transform.position;
                delta.y = 0f;
                float distance = delta.magnitude;
                if (distance <= 0.01f || distance > lockOnRange)
                {
                    continue;
                }

                float alignment = Vector3.Dot(cameraForward, delta / distance);
                float score = distance + (1f - alignment) * 3f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = enemy;
                }
            }

            lockTarget = best;
        }

        private void ValidateLockTarget()
        {
            if (lockTarget == null)
            {
                return;
            }

            Vector3 delta = lockTarget.transform.position - transform.position;
            delta.y = 0f;
            if (lockTarget.IsDead || delta.sqrMagnitude > lockOnRange * lockOnRange * 1.8f)
            {
                lockTarget = null;
            }
        }

        private void TryPickupWeapon()
        {
            if (weaponController == null)
            {
                return;
            }

            Vector3 dropPosition = transform.position - transform.forward * 1.15f + Vector3.up * 0.35f;
            weaponController.TryPickupNearest(transform.position, pickupRadius, dropPosition);
        }

        protected override void OnDamaged(Vector3 knockback)
        {
            if (controller != null && knockback.sqrMagnitude > 0f)
            {
                controller.Move(knockback * 0.08f);
            }
        }

        protected override void OnDeath()
        {
            guarding = false;
            lockTarget = null;
        }
    }
}
