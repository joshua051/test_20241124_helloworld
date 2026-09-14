using IronSand.Combat;
using IronSand.Scoring;
using NUnit.Framework;

namespace IronSand.Tests
{
    public sealed class StyleScoreModelTests
    {
        [Test]
        public void RegisterHit_VaryingWeaponRewardsMoreThanImmediateRepetition()
        {
            StyleScoreModel repeated = new(2.5f);
            repeated.RegisterHit(AttackKind.Light, WeaponArchetype.Sword, false, 0f);
            StyleAward repeatedAward = repeated.RegisterHit(AttackKind.Light, WeaponArchetype.Sword, false, 1f);

            StyleScoreModel varied = new(2.5f);
            varied.RegisterHit(AttackKind.Light, WeaponArchetype.Sword, false, 0f);
            StyleAward variedAward = varied.RegisterHit(AttackKind.Light, WeaponArchetype.Axe, false, 1f);

            Assert.That(repeatedAward.Varied, Is.False);
            Assert.That(variedAward.Varied, Is.True);
            Assert.That(variedAward.Points, Is.GreaterThan(repeatedAward.Points));
        }

        [Test]
        public void Tick_ExpiresComboAfterWindow()
        {
            StyleScoreModel model = new(2.5f);
            model.RegisterHit(AttackKind.Light, WeaponArchetype.Sword, false, 0f);
            model.RegisterHit(AttackKind.Heavy, WeaponArchetype.Sword, false, 1f);

            bool changed = model.Tick(4f);

            Assert.That(changed, Is.True);
            Assert.That(model.ComboCount, Is.Zero);
            Assert.That(model.Rank, Is.EqualTo("E"));
        }

        [Test]
        public void RegisterHit_KillAddsMeaningfulScoreBonus()
        {
            StyleScoreModel normal = new(2.5f);
            StyleAward normalAward = normal.RegisterHit(AttackKind.Heavy, WeaponArchetype.Mace, false, 0f);

            StyleScoreModel kill = new(2.5f);
            StyleAward killAward = kill.RegisterHit(AttackKind.Heavy, WeaponArchetype.Mace, true, 0f);

            Assert.That(killAward.Points, Is.GreaterThan(normalAward.Points));
            Assert.That(killAward.Favor, Is.GreaterThan(normalAward.Favor));
        }
    }
}
