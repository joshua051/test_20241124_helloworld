using IronSand.Arena;
using IronSand.Combat;
using IronSand.Enemy;
using UnityEngine;

namespace IronSand.Player
{
    [RequireComponent(typeof(CharacterController))]
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

        private CharacterController controller;
        private Camera gameplayCamera;
        private ArenaDirector arenaDirector;
        private float verticalVelocity;
        private float attackCooldownRemaining;
        private bool guarding;

        public bool Guarding => guarding;

        protected override void Awake()
        {
            base.Awake();
            controller = GetComponent<CharacterController>();
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

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }

            guarding = Input.GetKey(KeyCode.Q) && !IsStunned;
            HandleMovement();

            if (!IsStunned && attackCooldownRemaining <= 0f)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    PerformAttack(lightDamage, 1, 1f);
                }
                else if (Input.GetMouseButtonDown(1))
                {
                    PerformAttack(heavyDamage, 3, 1.5f);
                }
            }

            if (!IsStunned && Input.GetKeyDown(KeyCode.Space))
            {
                Dodge();
            }
        }

        public new bool ApplyDamage(float amount, Vector3 knockback, float stunMultiplier = 1f)
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

            if (move.sqrMagnitude > 0.01f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(move, Vector3.up);
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

        private void PerformAttack(float damage, int favor, float stunMultiplier)
        {
            attackCooldownRemaining = attackCooldown;
            Vector3 center = transform.position + Vector3.up + transform.forward * attackReach;
            Collider[] hits = Physics.OverlapSphere(center, attackRadius, ~0, QueryTriggerInteraction.Ignore);

            foreach (Collider hit in hits)
            {
                EnemyGladiator enemy = hit.GetComponentInParent<EnemyGladiator>();
                if (enemy == null || enemy.IsDead)
                {
                    continue;
                }

                Vector3 toEnemy = (enemy.transform.position - transform.position);
                toEnemy.y = 0f;
                if (toEnemy.sqrMagnitude < 0.001f || Vector3.Dot(transform.forward, toEnemy.normalized) < 0.15f)
                {
                    continue;
                }

                if (enemy.ApplyDamage(damage, toEnemy.normalized * 2.5f, stunMultiplier))
                {
                    arenaDirector?.RegisterStylishHit(favor);
                }
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
        }
    }
}
