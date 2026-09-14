using System;

namespace IronSand.Combat
{
    public readonly struct AttackProfile
    {
        public AttackProfile(float startup, float active, float recovery, float damageMultiplier, float poiseDamage, float sweepRadius, float rootMotionDistance, float hitStopSeconds, float cameraShake)
        {
            Startup = Positive(startup, nameof(startup)); Active = Positive(active, nameof(active)); Recovery = Positive(recovery, nameof(recovery)); DamageMultiplier = Positive(damageMultiplier, nameof(damageMultiplier)); PoiseDamage = NonNegative(poiseDamage, nameof(poiseDamage)); SweepRadius = Positive(sweepRadius, nameof(sweepRadius)); RootMotionDistance = NonNegative(rootMotionDistance, nameof(rootMotionDistance)); HitStopSeconds = NonNegative(hitStopSeconds, nameof(hitStopSeconds)); CameraShake = NonNegative(cameraShake, nameof(cameraShake));
        }
        public float Startup { get; }
        public float Active { get; }
        public float Recovery { get; }
        public float DamageMultiplier { get; }
        public float PoiseDamage { get; }
        public float SweepRadius { get; }
        public float RootMotionDistance { get; }
        public float HitStopSeconds { get; }
        public float CameraShake { get; }
        public float Total => Startup + Active + Recovery;
        private static float Positive(float value, string name) { if (!Finite(value) || value <= 0f) throw new ArgumentOutOfRangeException(name); return value; }
        private static float NonNegative(float value, string name) { if (!Finite(value) || value < 0f) throw new ArgumentOutOfRangeException(name); return value; }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }

    public static class AttackLibrary
    {
        public static AttackProfile Get(WeaponArchetype weapon, AttackKind kind)
        {
            WeaponStats stats = WeaponCatalog.Get(weapon);
            bool heavy = kind == AttackKind.Heavy;
            float cadence = Math.Max(0.72f, stats.CooldownMultiplier);
            float weight = weapon switch { WeaponArchetype.Axe => 1.18f, WeaponArchetype.Mace => 1.25f, WeaponArchetype.Spear => 1.04f, WeaponArchetype.Sword => 1f, _ => 0.86f };
            float startup = (heavy ? 0.32f : 0.14f) * cadence * weight;
            float active = (heavy ? 0.18f : 0.11f) * Math.Max(0.9f, weight);
            float recovery = (heavy ? 0.48f : 0.25f) * cadence * weight;
            float damage = heavy ? 1.15f : 1f;
            float poise = (heavy ? 46f : 24f) * weight;
            float sweep = weapon == WeaponArchetype.Spear ? 0.24f : weapon == WeaponArchetype.Mace ? 0.42f : 0.34f;
            float root = heavy ? 0.92f : 0.52f;
            if (weapon == WeaponArchetype.Spear) root += 0.28f;
            float hitStop = (heavy ? 0.075f : 0.045f) * Math.Min(1.35f, weight);
            float shake = heavy ? 0.22f : 0.12f;
            return new AttackProfile(startup, active, recovery, damage, poise, sweep, root, hitStop, shake);
        }
    }
}
