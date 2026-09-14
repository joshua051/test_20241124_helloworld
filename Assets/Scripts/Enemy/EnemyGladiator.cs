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
        [SerializeField, Min(0.1f)] private float attackWindup = 0.35f;

        private CharacterController controller;
        private PlayerGladiator player;
        private ArenaDirector director;
        private float cooldownRemaining;
        private float windupRemaining;
        private bool ownsAttackToken;
        private bool attackCommitted;
        private float orbitDirection = 1f;

        protected override void Awake()
        {
            base.Awake();
            controller = GetComponent<CharacterController>();
            orbitDirection = Random.value > 0.5f ? 1f : -1f;
        }

        public void Initialize(PlayerGladiator target, ArenaDirector arenaDirector)
        {
            player = target;
            director = arenaDirector;
        }

        protected override void Update()
        {
            base.Update();
            cooldownRemaining = Mathf.Max(0f, cooldownRemaining - Time.deltaTime);

            if (IsDead || player == null || player.IsDead)
            {
                ReleaseToken();
                return;
            }

            if (IsStunned)
            {
                return;
            }

            Vector3 toPlayer = player.transform.position - transform.position;
            toPlayer.y = 0f;
            float distance = toPlayer.magnitude;
            if (distance > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toPlayer), 10f * Time.deltaTime);
            }

            if (attackCommitted)
            {
                windupRemaining -= Time.deltaTime;
                if (windupRemaining <= 0f)
                {
                    ResolveAttack(distance, toPlayer);
                }
                return;
            }

            if (distance <= preferredRange + 0.35f && cooldownRemaining <= 0f)
            {
                if (!ownsAttackToken)
                {
                    ownsAttackToken = director != null && director.TryAcquireAttackToken(this);
                }

                if (ownsAttackToken)
                {
                    attackCommitted = true;
                    windupRemaining = attackWindup;
                    return;
                }
            }

            MoveAroundTarget(toPlayer, distance);
        }

        private void MoveAroundTarget(Vector3 toPlayer, float distance)
        {
            if (controller == null || toPlayer.sqrMagnitude < 0.001f)
            {
                return;
            }

            Vector3 radial = toPlayer.normalized;
            Vector3 tangent = Vector3.Cross(Vector3.up, radial) * orbitDirection;
            Vector3 movement;

            if (distance > preferredRange + 0.75f)
            {
                movement = radial;
            }
            else if (distance < preferredRange - 0.35f)
            {
                movement = -radial * 0.75f + tangent * 0.25f;
            }
            else
            {
                movement = tangent * orbitSpeed;
            }

            controller.Move(movement.normalized * (moveSpeed * Time.deltaTime));
        }

        private void ResolveAttack(float distance, Vector3 toPlayer)
        {
            attackCommitted = false;
            cooldownRemaining = attackCooldown;

            if (distance <= preferredRange + 0.65f && toPlayer.sqrMagnitude > 0.001f)
            {
                player.ApplyDamage(attackDamage, toPlayer.normalized * 1.8f, 1f);
            }

            ReleaseToken();
        }

        private void ReleaseToken()
        {
            if (!ownsAttackToken)
            {
                return;
            }

            director?.ReleaseAttackToken(this);
            ownsAttackToken = false;
            attackCommitted = false;
        }

        protected override void OnDamaged(Vector3 knockback)
        {
            ReleaseToken();
            if (controller != null && knockback.sqrMagnitude > 0f)
            {
                controller.Move(knockback * 0.12f);
            }
        }

        protected override void OnDeath()
        {
            ReleaseToken();
            if (controller != null)
            {
                controller.enabled = false;
            }
            Destroy(gameObject, 0.8f);
        }

        private void OnDisable()
        {
            ReleaseToken();
        }
    }
}
