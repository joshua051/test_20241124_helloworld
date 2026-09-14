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
        [SerializeField, Min(0.1f)] private float attackCooldown = 1.1f;
        private CharacterController controller;
        private PlayerGladiator player;
        private ArenaDirector director;
        private ProceduralCombatRig rig;
        private GameObject weaponVisual;
        private readonly AttackTimeline attack = new();
        private float cooldownRemaining;
        private float verticalVelocity;
        private float orbitDirection = 1f;
        private bool ownsAttackToken;
        private bool attackResolved;
        private Vector3 committedForward;
        private AttackKind committedKind;
        private AttackProfile committedProfile;
        public WeaponArchetype HeldWeapon { get; private set; } = WeaponArchetype.Sword;
        public EnemyRole Role { get; private set; }
        public bool IsAttackCommitted => attack.IsRunning;
        public float WindupProgress => attack.IsRunning && attack.Phase == CombatPhase.Startup ? attack.PhaseProgress : attack.IsRunning ? 1f : 0f;
        protected override void Awake()
        {
            base.Awake();
            controller = GetComponent<CharacterController>();
            orbitDirection = Random.value > 0.5f ? 1f : -1f;
        }
        public void Initialize(PlayerGladiator target, ArenaDirector arenaDirector, WeaponArchetype weapon, EnemyRole role)
        {
            player = target;
            director = arenaDirector;
            Role = role;
            HeldWeapon = weapon == WeaponArchetype.Unarmed ? WeaponArchetype.Sword : weapon;
            ApplyRoleTuning(role);
            WeaponStats stats = WeaponCatalog.Get(HeldWeapon);
            preferredRange *= Mathf.Clamp(stats.ReachMultiplier, 0.85f, 1.35f);
            cooldownRemaining = 0.45f;
            rig = gameObject.AddComponent<ProceduralCombatRig>();
            rig.ConfigureTeam(false);
            RefreshWeaponVisual();
        }
        protected override void Update()
        {
            base.Update();
            if (IsDead || Time.timeScale <= 0f || Time.deltaTime <= 0f || CombatFreezeSystem.IsFrozen) return;
            if (transform.position.y < -5f)
            {
                Debug.LogError("Enemy fell outside the arena; verify the rebuilt floor collider.", this);
                ApplyDamage(MaxHealth + 1f, Vector3.zero);
                return;
            }
            ApplyGravity();
            cooldownRemaining = Mathf.Max(0f, cooldownRemaining - Time.deltaTime);
            if (player == null || player.IsDead) { CancelAttackAndRelease(); return; }
            if (ExecutionReady) { CancelAttackAndRelease(); rig?.SetVulnerable(true); rig?.SetMotion(0f, false); return; }
            rig?.SetVulnerable(false);
            if (IsStunned) { CancelAttackAndRelease(); rig?.SetMotion(0f, false); return; }
            Vector3 toPlayer = player.transform.position - transform.position;
            toPlayer.y = 0f;
            float distance = toPlayer.magnitude;
            if (attack.IsRunning) { TickAttack(toPlayer, distance); return; }
            if (distance > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(toPlayer), 1f - Mathf.Exp(-10f * Time.deltaTime));
            float attackRange = preferredRange + 0.34f;
            if (distance <= attackRange && cooldownRemaining <= 0f && !CombatQueries.WorldBlocks(transform.position, player.transform.position))
            {
                if (!ownsAttackToken) ownsAttackToken = director != null && director.TryAcquireAttackToken(this);
                if (ownsAttackToken) { StartAttack(toPlayer); return; }
            }
            MoveAroundTarget(toPlayer, distance, attackRange);
        }
        private void StartAttack(Vector3 toPlayer)
        {
            committedKind = Role == EnemyRole.Brute ? AttackKind.Heavy : Random.value < 0.28f ? AttackKind.Heavy : AttackKind.Light;
            committedProfile = AttackLibrary.Get(HeldWeapon, committedKind);
            attack.TryStart(committedProfile);
            attackResolved = false;
            committedForward = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : transform.forward;
            rig?.SetAttack(attack.Phase, committedKind, attack.Normalized);
        }
        private void TickAttack(Vector3 toPlayer, float distance)
        {
            float rootDelta = attack.Tick(Time.deltaTime);
            if (rootDelta > 0f && controller.enabled)
                controller.Move(committedForward * (committedProfile.RootMotionDistance * 0.72f * rootDelta));
            rig?.SetAttack(attack.Phase, committedKind, attack.Normalized);
            if (!attackResolved && attack.Phase == CombatPhase.Active)
            {
                attackResolved = true;
                float range = preferredRange + (HeldWeapon == WeaponArchetype.Spear ? 0.95f : 0.62f);
                if (distance <= range && toPlayer.sqrMagnitude > 0.001f && Vector3.Dot(committedForward, toPlayer.normalized) >= 0.25f &&
                    !CombatQueries.WorldBlocks(transform.position, player.transform.position))
                {
                    WeaponStats stats = WeaponCatalog.Get(HeldWeapon);
                    float weaponDamage = committedKind == AttackKind.Heavy ? stats.HeavyDamageMultiplier : stats.LightDamageMultiplier;
                    CombatImpact impact = new(this,
                        attackDamage * weaponDamage * committedProfile.DamageMultiplier,
                        committedProfile.PoiseDamage,
                        toPlayer.normalized * (committedKind == AttackKind.Heavy ? 3.3f : 1.9f),
                        stats.StunMultiplier * (committedKind == AttackKind.Heavy ? 1.45f : 1f));
                    ImpactResult result = player.ReceiveImpact(impact);
                    if (!result.PerfectGuard && (result.Accepted || result.Guarded))
                        CombatFeedbackSystem.EmitImpact(player.transform.position + Vector3.up * 0.35f,
                            committedProfile.HitStopSeconds, committedProfile.CameraShake, committedKind == AttackKind.Heavy);
                }
            }
            if (!attack.IsRunning)
            {
                rig?.ClearAttack();
                cooldownRemaining = attackCooldown * (Role == EnemyRole.Aggressor ? 0.82f : Role == EnemyRole.Brute ? 1.24f : 1f);
                ReleaseToken();
            }
        }
        private void MoveAroundTarget(Vector3 toPlayer, float distance, float attackRange)
        {
            if (toPlayer.sqrMagnitude < 0.001f || !controller.enabled) return;
            Vector3 radial = toPlayer.normalized;
            Vector3 tangent = Vector3.Cross(Vector3.up, radial) * orbitDirection;
            Vector3 velocity;
            bool worldBlocked = CombatQueries.WorldBlocks(transform.position, player.transform.position);
            if (worldBlocked)
                velocity = tangent * orbitSpeed * 1.6f + radial * moveSpeed * 0.18f;
            else if (distance > attackRange - 0.03f)
                velocity = radial * moveSpeed;
            else if (distance < preferredRange - 0.30f)
                velocity = (-radial * 0.72f + tangent * 0.28f) * moveSpeed;
            else
            {
                float roleOrbit = Role == EnemyRole.Flanker ? 1.55f : Role == EnemyRole.Skirmisher ? 1.30f : 1f;
                velocity = tangent * orbitSpeed * roleOrbit;
            }
            controller.Move(velocity * Time.deltaTime);
            rig?.SetMotion(Mathf.Clamp01(velocity.magnitude / Mathf.Max(0.1f, moveSpeed)), false);
        }
        private void ApplyGravity()
        {
            if (!controller.enabled) return;
            if (controller.isGrounded && verticalVelocity < 0f) verticalVelocity = -2f;
            verticalVelocity -= 25f * Time.deltaTime;
            controller.Move(Vector3.up * (verticalVelocity * Time.deltaTime));
        }
        public void SetExecutionPose(float progress) { rig?.SetExecution(false, progress); }
        public void ClearExecutionPose() { rig?.ClearExecution(); }
        public void ApplyPerfectGuardCounter(PlayerGladiator source)
        {
            CancelAttackAndRelease();
            Vector3 away = transform.position - source.transform.position;
            away.y = 0f;
            ReceiveImpact(new CombatImpact(source, 0f, 72f,
                away.sqrMagnitude > 0.01f ? away.normalized * 2.8f : Vector3.zero, 2.2f));
            cooldownRemaining = Mathf.Max(cooldownRemaining, 1.15f);
            if (HeldWeapon != WeaponArchetype.Unarmed) Disarm();
        }
        private void Disarm()
        {
            WeaponArchetype dropped = HeldWeapon;
            WeaponStats stats = WeaponCatalog.Get(dropped);
            Vector3 drop = transform.position + transform.right * 0.7f;
            drop.y = 0.35f;
            WeaponPickup.Spawn(drop, dropped, Mathf.Max(1, stats.MaxDurability / 2));
            HeldWeapon = WeaponArchetype.Unarmed;
            RefreshWeaponVisual();
        }
        private void RefreshWeaponVisual()
        {
            if (weaponVisual != null) Destroy(weaponVisual);
            if (rig == null || HeldWeapon == WeaponArchetype.Unarmed) return;
            weaponVisual = WeaponVisualFactory.CreatePlaceholder(rig.WeaponSocket, HeldWeapon, new Vector3(0f, 0f, 0.46f));
        }
        private void ApplyRoleTuning(EnemyRole role)
        {
            switch (role)
            {
                case EnemyRole.Aggressor: moveSpeed *= 1.14f; orbitSpeed *= 0.85f; attackCooldown *= 0.84f; break;
                case EnemyRole.Flanker: moveSpeed *= 1.05f; orbitSpeed *= 1.45f; break;
                case EnemyRole.Brute: moveSpeed *= 0.82f; attackDamage *= 1.32f; attackCooldown *= 1.18f; break;
                case EnemyRole.Skirmisher: preferredRange *= 1.15f; moveSpeed *= 1.08f; break;
            }
        }
        private void CancelAttackAndRelease() { attack.Cancel(); rig?.ClearAttack(); ReleaseToken(); }
        private void ReleaseToken() { if (ownsAttackToken) director?.ReleaseAttackToken(this); ownsAttackToken = false; }
        protected override void OnDamaged(Vector3 knockback)
        {
            CancelAttackAndRelease();
            cooldownRemaining = Mathf.Max(cooldownRemaining, 0.45f);
            rig?.TriggerHit(LastPoiseBroken);
            if (controller != null && controller.enabled && knockback.sqrMagnitude > 0f)
                controller.Move(knockback * 0.12f);
        }
        protected override void OnDeath()
        {
            CancelAttackAndRelease();
            rig?.TriggerHit(true);
            if (Application.isPlaying && HeldWeapon != WeaponArchetype.Unarmed)
            {
                WeaponStats stats = WeaponCatalog.Get(HeldWeapon);
                Vector3 drop = transform.position;
                drop.y = 0.35f;
                WeaponPickup.Spawn(drop, HeldWeapon, Mathf.Max(1, stats.MaxDurability / 2));
                HeldWeapon = WeaponArchetype.Unarmed;
            }
            if (controller != null) controller.enabled = false;
            if (Application.isPlaying) Destroy(gameObject, 1.15f);
        }
        private void OnDisable() { ReleaseToken(); }
    }
}
