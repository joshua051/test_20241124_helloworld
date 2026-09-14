using System;

namespace IronSand.Combat
{
    public readonly struct WeaponStats
    {
        public WeaponStats(
            string displayName,
            float lightDamageMultiplier,
            float heavyDamageMultiplier,
            float reachMultiplier,
            float cooldownMultiplier,
            float stunMultiplier,
            int maxDurability,
            int favorBonus)
        {
            DisplayName = displayName;
            LightDamageMultiplier = lightDamageMultiplier;
            HeavyDamageMultiplier = heavyDamageMultiplier;
            ReachMultiplier = reachMultiplier;
            CooldownMultiplier = cooldownMultiplier;
            StunMultiplier = stunMultiplier;
            MaxDurability = maxDurability;
            FavorBonus = favorBonus;
        }

        public string DisplayName { get; }
        public float LightDamageMultiplier { get; }
        public float HeavyDamageMultiplier { get; }
        public float ReachMultiplier { get; }
        public float CooldownMultiplier { get; }
        public float StunMultiplier { get; }
        public int MaxDurability { get; }
        public int FavorBonus { get; }
    }

    public static class WeaponCatalog
    {
        public static WeaponStats Get(WeaponArchetype archetype)
        {
            return archetype switch
            {
                WeaponArchetype.Sword => new WeaponStats("Gladius", 1.00f, 1.00f, 1.00f, 1.00f, 1.00f, 14, 0),
                WeaponArchetype.Axe => new WeaponStats("Arena Axe", 1.10f, 1.30f, 0.95f, 1.15f, 1.15f, 10, 1),
                WeaponArchetype.Spear => new WeaponStats("Arena Spear", 0.90f, 1.05f, 1.45f, 1.08f, 0.90f, 12, 1),
                WeaponArchetype.Mace => new WeaponStats("Iron Mace", 1.05f, 1.20f, 0.90f, 1.20f, 1.35f, 11, 1),
                _ => new WeaponStats("Unarmed", 0.65f, 0.70f, 0.78f, 0.88f, 0.75f, 0, 0)
            };
        }

        public static WeaponArchetype GetArenaWeapon(int seed)
        {
            int index = (int)(Math.Abs((long)seed) % 4L);
            return (WeaponArchetype)(index + 1);
        }
    }
}
