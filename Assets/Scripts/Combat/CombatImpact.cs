using UnityEngine;

namespace IronSand.Combat
{
    public readonly struct CombatImpact
    {
        public CombatImpact(Combatant source, float damage, float poiseDamage, Vector3 knockback, float stunMultiplier = 1f, bool unblockable = false, bool execution = false)
        {
            Source = source;
            Damage = Mathf.Max(0f, damage);
            PoiseDamage = Mathf.Max(0f, poiseDamage);
            Knockback = knockback;
            StunMultiplier = Mathf.Max(0f, stunMultiplier);
            Unblockable = unblockable;
            Execution = execution;
        }
        public Combatant Source { get; }
        public float Damage { get; }
        public float PoiseDamage { get; }
        public Vector3 Knockback { get; }
        public float StunMultiplier { get; }
        public bool Unblockable { get; }
        public bool Execution { get; }
        public CombatImpact Scale(float damageMultiplier, float poiseMultiplier, float knockbackMultiplier, float stunMultiplier)
        {
            return new CombatImpact(Source, Damage * Mathf.Max(0f, damageMultiplier), PoiseDamage * Mathf.Max(0f, poiseMultiplier), Knockback * Mathf.Max(0f, knockbackMultiplier), StunMultiplier * Mathf.Max(0f, stunMultiplier), Unblockable, Execution);
        }
    }

    public readonly struct ImpactResult
    {
        public ImpactResult(bool accepted, bool guarded, bool perfectGuard, bool poiseBroken, bool killed, float damageApplied)
        {
            Accepted = accepted; Guarded = guarded; PerfectGuard = perfectGuard; PoiseBroken = poiseBroken; Killed = killed; DamageApplied = damageApplied;
        }
        public bool Accepted { get; }
        public bool Guarded { get; }
        public bool PerfectGuard { get; }
        public bool PoiseBroken { get; }
        public bool Killed { get; }
        public float DamageApplied { get; }
        public static ImpactResult Ignored => new(false, false, false, false, false, 0f);
        public static ImpactResult PerfectBlocked => new(false, true, true, false, false, 0f);
        public ImpactResult AsGuarded() => new(Accepted, true, false, PoiseBroken, Killed, DamageApplied);
    }
}
