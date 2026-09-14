using IronSand.Arena;
using IronSand.Combat;
using IronSand.Player;
using UnityEngine;

namespace IronSand.Enemy
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class EnemyGladiator : Combatant
    {
        [SerializeField, Min(0f)] private float moveSpeed = 3.2f;
        [SerializeField, Min(0f)] private float orbitSpeed = 1.2f;
        [SerializeField, Min(0.1f)] private float preferredRange = 2.1f;
        [SerializeField, Min(0f)] private float attackDamage = 14f;
        [SerializeField, Min(0.1f)] private float attackCooldown = 1.35f;
        [SerializeField, Min(0.1f)] private float attackWindup = 0.5f;
        private CharacterController controller;
        private PlayerGladiator player;
        private ArenaDirector director;
        private float cooldownRemaining;
        private float windupRemaining;
        private float verticalVelocity;
        private bool ownsAttackToken;
        private bool attackCommitted;
        private float orbitDirection = 1f;
        private Vector3 committedForward;
        public WeaponArchetype HeldWeapon { get; private set; } = WeaponArchetype.Sword;
        public bool IsAttackCommitted => attackCommitted;
        public float WindupProgress => attackCommitted ? 1f - Mathf.Clamp01(windupRemaining / attackWindup) : 0f;

        protected override void Awake()
        {
            base.Awake();
            controller = GetComponent<CharacterController>();
            orbitDirection = Random.value > 0.5f ? 1f : -1f;
        }

        public void Initialize(PlayerGladiator target, ArenaDirector arenaDirector, WeaponArchetype weapon)
        {
            player = target;
            director = arenaDirector;
            HeldWeapon = weapon == WeaponArchetype.Unarmed ? WeaponArchetype.Sword : weapon;
            WeaponStats stats = WeaponCatalog.Get(HeldWeapon);
            attackDamage *= stats.LightDamageMultiplier;
            preferredRange *= Mathf.Clamp(stats.ReachMultiplier, 0.85f, 1.35f);
            attackCooldown *= stats.CooldownMultiplier;
            cooldownRemaining = 0.5f;
            WeaponVisualFactory.CreatePlaceholder(transform, HeldWeapon, new Vector3(0.6f, 0.25f, 0.35f));
        }

        protected override void Update()
        {
            base.Update();
            if (IsDead || Time.timeScale <= 0f || Time.deltaTime <= 0f) return;
            if (transform.position.y < -5f)
            {
                Debug.LogError("Enemy fell outside the arena; verify the rebuilt floor collider.", this);
                ApplyDamage(MaxHealth + 1f, Vector3.zero);
                return;
            }
            // CharacterController.Move never supplies gravity automatically.
            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            verticalVelocity -= 25f * Time.deltaTime;
            controller.Move(Vector3.up * (verticalVelocity * Time.deltaTime));
            cooldownRemaining = Mathf.Max(0f, cooldownRemaining - Time.deltaTime);
            if (player == null || player.IsDead) { ReleaseToken(); return; }
            if (IsStunned) return;

            Vector3 toPlayer = player.transform.position - transform.position;
            toPlayer.y = 0f;
            float distance = toPlayer.magnitude;
            if (attackCommitted)
            {
                windupRemaining -= Time.deltaTime;
                if (windupRemaining <= 0f) ResolveAttack(distance, toPlayer);
                return;
            }
            if (distance > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toPlayer),
                    1f - Mathf.Exp(-10f * Time.deltaTime));
            if (distance <= preferredRange + 0.35f && cooldownRemaining <= 0f &&
                !CombatQueries.WorldBlocks(transform.position, player.transform.position))
            {
                if (!ownsAttackToken) ownsAttackToken = director != null && director.TryAcquireAttackToken(this);
                if (ownsAttackToken)
                {
                    attackCommitted = true;
                    windupRemaining = attackWindup;
                    committedForward = transform.forward;
                    return;
                }
            }
            MoveAroundTarget(toPlayer, distance);
        }

        private void MoveAroundTarget(Vector3 toPlayer, float distance)
        {
            if (toPlayer.sqrMagnitude < 0.001f) return;
            Vector3 radial = toPlayer.normalized;
            Vector3 tangent = Vector3.Cross(Vector3.up, radial) * orbitDirection;
            Vector3 velocity;
            if (distance > preferredRange + 0.75f) velocity = radial * moveSpeed;
            else if (distance < preferredRange - 0.35f) velocity = (-radial * 0.75f + tangent * 0.25f) * moveSpeed;
            else velocity = tangent * orbitSpeed;
            controller.Move(velocity * Time.deltaTime);
        }

        private void ResolveAttack(float distance, Vector3 toPlayer)
        {
            attackCommitted = false;
            cooldownRemaining = attackCooldown;
            if (distance <= preferredRange + 0.65f && toPlayer.sqrMagnitude > 0.001f &&
                Vector3.Dot(committedForward, toPlayer.normalized) >= 0.35f &&
                !CombatQueries.WorldBlocks(transform.position, player.transform.position))
            {
                player.ApplyDamage(attackDamage, toPlayer.normalized * 1.8f, WeaponCatalog.Get(HeldWeapon).StunMultiplier);
            }
            ReleaseToken();
        }

        private void ReleaseToken()
        {
            if (ownsAttackToken) director?.ReleaseAttackToken(this);
            ownsAttackToken = false;
            attackCommitted = false;
        }

        protected override void OnDamaged(Vector3 knockback)
        {
            ReleaseToken();
            cooldownRemaining = Mathf.Max(cooldownRemaining, 0.4f);
            if (controller != null && controller.enabled && knockback.sqrMagnitude > 0f)
                controller.Move(knockback * 0.12f);
        }

        protected override void OnDeath()
        {
            ReleaseToken();
            WeaponStats stats = WeaponCatalog.Get(HeldWeapon);
            Vector3 drop = transform.position;
            drop.y = 0.35f;
            WeaponPickup.Spawn(drop, HeldWeapon, Mathf.Max(1, stats.MaxDurability / 2));
            if (controller != null) controller.enabled = false;
            Destroy(gameObject, 0.8f);
        }

        private void OnDisable() { ReleaseToken(); }
    }
}
