using System;
using IronSand.Telemetry;
using UnityEngine;

namespace IronSand.Combat
{
    public abstract class Combatant : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float maxHealth = 100f;
        [SerializeField, Min(0f)] private float health = 100f;
        [SerializeField, Min(0f)] private float hitStunSeconds = 0.2f;
        [Header("Poise")]
        [SerializeField, Min(1f)] private float maxPoise = 85f;
        [SerializeField, Min(0f)] private float poiseRecoveryDelay = 1.1f;
        [SerializeField, Min(0f)] private float poiseRecoveryPerSecond = 24f;
        [SerializeField, Range(0.05f, 0.9f)] private float executionHealthRatio = 0.24f;
        [SerializeField, Min(0.25f)] private float executionReadySeconds = 3.5f;
        private float hitStunRemaining;
        private float invulnerableRemaining;
        private float executionReadyRemaining;
        private PoiseState poise;
        private bool restorePoiseAfterStun;
        public event Action<Combatant> Died;
        public event Action<float, float> HealthChanged;
        public float MaxHealth => maxHealth;
        public float Health => health;
        public float MaxPoise => poise?.Max ?? maxPoise;
        public float Poise => poise?.Current ?? maxPoise;
        public bool IsDead { get; private set; }
        public bool IsStunned => hitStunRemaining > 0f;
        public bool IsInvulnerable => invulnerableRemaining > 0f;
        public bool ExecutionReady => !IsDead && executionReadyRemaining > 0f;
        public float ExecutionReadyRemaining => executionReadyRemaining;
        public bool LastPoiseBroken { get; private set; }
        protected virtual void Awake()
        {
            health = Mathf.Clamp(health, 0f, maxHealth);
            poise = new PoiseState(Mathf.Max(1f, maxPoise), Mathf.Max(0f, poiseRecoveryDelay), Mathf.Max(0f, poiseRecoveryPerSecond));
        }
        protected virtual void Update()
        {
            if (CombatFreezeSystem.IsFrozen || Time.deltaTime <= 0f) return;
            hitStunRemaining = Mathf.Max(0f, hitStunRemaining - Time.deltaTime);
            invulnerableRemaining = Mathf.Max(0f, invulnerableRemaining - Time.deltaTime);
            executionReadyRemaining = Mathf.Max(0f, executionReadyRemaining - Time.deltaTime);
            poise?.Tick(Time.deltaTime);
            if (restorePoiseAfterStun && hitStunRemaining <= 0f) { poise?.Restore(); restorePoiseAfterStun = false; }
        }
        public virtual ImpactResult ReceiveImpact(CombatImpact impact)
        {
            if (IsDead || (!impact.Execution && IsInvulnerable)) return ImpactResult.Ignored;
            if (!Finite(impact.Damage) || !Finite(impact.PoiseDamage) || !Finite(impact.StunMultiplier)) return ImpactResult.Ignored;
            if (impact.Damage <= 0f && impact.PoiseDamage <= 0f && !impact.Execution) return ImpactResult.Ignored;
            float previousHealth = health;
            float previousPoise = Poise;
            float appliedDamage = impact.Execution ? maxHealth + 1f : Mathf.Max(0f, impact.Damage);
            health = Mathf.Max(0f, health - appliedDamage);
            bool poiseBroken = impact.Execution || (poise != null && poise.Apply(Mathf.Max(0f, impact.PoiseDamage)));
            LastPoiseBroken = poiseBroken;
            float stun = hitStunSeconds * Mathf.Max(0f, impact.StunMultiplier);
            if (poiseBroken) { stun = Mathf.Max(stun, hitStunSeconds * 3.2f + 0.18f); restorePoiseAfterStun = true; }
            hitStunRemaining = Mathf.Max(hitStunRemaining, stun);
            bool killed = health <= 0f;
            if (!killed && poiseBroken && maxHealth > 0f && health / maxHealth <= executionHealthRatio) executionReadyRemaining = Mathf.Max(executionReadyRemaining, executionReadySeconds);
            if (killed) { IsDead = true; executionReadyRemaining = 0f; }
            OnDamaged(impact.Knockback);
            if (killed) OnDeath();
            if (!Mathf.Approximately(previousHealth, health)) HealthChanged?.Invoke(health, maxHealth);
            TelemetryRecorder.RecordEvent(
                impact.Execution ? "execution_impact" : "impact",
                impact.Source != null ? impact.Source.name : null,
                name,
                null,
                impact.Execution ? AttackKind.Execution : null,
                previousHealth - health,
                Mathf.Max(0f, previousPoise - Poise),
                transform.position,
                FormattableString.Invariant($"{{\"hp_before\":{previousHealth:0.###},\"hp_after\":{health:0.###},\"poise_before\":{previousPoise:0.###},\"poise_after\":{Poise:0.###},\"poise_broken\":{poiseBroken.ToString().ToLowerInvariant()},\"killed\":{killed.ToString().ToLowerInvariant()}}}"));
            if (killed) Died?.Invoke(this);
            return new ImpactResult(true, false, false, poiseBroken, killed, previousHealth - health);
        }
        public virtual bool ApplyDamage(float amount, Vector3 knockback, float stunMultiplier = 1f) => ReceiveImpact(new CombatImpact(null, amount, 0f, knockback, stunMultiplier)).Accepted;
        public void Heal(float amount)
        {
            if (IsDead || !Finite(amount) || amount <= 0f) return;
            float previous = health; health = Mathf.Min(maxHealth, health + amount);
            if (!Mathf.Approximately(previous, health))
            {
                HealthChanged?.Invoke(health, maxHealth);
                TelemetryRecorder.RecordEvent("heal", null, name, null, null, health - previous, null, transform.position,
                    FormattableString.Invariant($"{{\"hp_before\":{previous:0.###},\"hp_after\":{health:0.###}}}"));
            }
        }
        public void GrantInvulnerability(float seconds) { if (Finite(seconds) && seconds > 0f) invulnerableRemaining = Mathf.Max(invulnerableRemaining, seconds); }
        protected abstract void OnDamaged(Vector3 knockback);
        protected abstract void OnDeath();
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
