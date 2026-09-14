using IronSand.Combat;
using NUnit.Framework;

namespace IronSand.Tests
{
    public sealed class WeaponCatalogTests
    {
        [TestCase(WeaponArchetype.Sword)]
        [TestCase(WeaponArchetype.Axe)]
        [TestCase(WeaponArchetype.Spear)]
        [TestCase(WeaponArchetype.Mace)]
        public void ArmedWeapons_HaveValidCombatTuning(WeaponArchetype archetype)
        {
            WeaponStats stats = WeaponCatalog.Get(archetype);

            Assert.That(stats.LightDamageMultiplier, Is.GreaterThan(0f));
            Assert.That(stats.HeavyDamageMultiplier, Is.GreaterThan(0f));
            Assert.That(stats.ReachMultiplier, Is.GreaterThan(0f));
            Assert.That(stats.CooldownMultiplier, Is.GreaterThan(0f));
            Assert.That(stats.StunMultiplier, Is.GreaterThan(0f));
            Assert.That(stats.MaxDurability, Is.GreaterThan(0));
        }

        [Test]
        public void Unarmed_HasNoDurability()
        {
            Assert.That(WeaponCatalog.Get(WeaponArchetype.Unarmed).MaxDurability, Is.Zero);
        }

        [Test]
        public void ArenaWeaponSelection_IsDeterministicAndNeverUnarmed()
        {
            for (int seed = -20; seed <= 20; seed++)
            {
                WeaponArchetype first = WeaponCatalog.GetArenaWeapon(seed);
                WeaponArchetype second = WeaponCatalog.GetArenaWeapon(seed);

                Assert.That(first, Is.EqualTo(second));
                Assert.That(first, Is.Not.EqualTo(WeaponArchetype.Unarmed));
            }
        }
    }
}
