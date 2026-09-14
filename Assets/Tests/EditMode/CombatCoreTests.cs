using IronSand.Combat;
using IronSand.Enemy;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace IronSand.Tests
{
    public sealed class CombatCoreTests
    {
        [Test]
        public void AttackTimeline_TraversesStartupActiveRecoveryAndCompletes()
        {
            var profile = new AttackProfile(0.2f, 0.1f, 0.3f, 1f, 20f, 0.3f, 0.6f, 0.05f, 0.1f); var timeline = new AttackTimeline(); Assert.IsTrue(timeline.TryStart(profile)); Assert.AreEqual(CombatPhase.Startup, timeline.Phase); timeline.Tick(0.21f); Assert.AreEqual(CombatPhase.Active, timeline.Phase); timeline.Tick(0.11f); Assert.AreEqual(CombatPhase.Recovery, timeline.Phase); timeline.Tick(0.30f); Assert.IsFalse(timeline.IsRunning); Assert.AreEqual(CombatPhase.Idle, timeline.Phase);
        }
        [Test]
        public void AttackTimeline_RootMotionFractionSumsToOne()
        {
            var profile = new AttackProfile(0.2f, 0.1f, 0.3f, 1f, 20f, 0.3f, 0.6f, 0.05f, 0.1f); var timeline = new AttackTimeline(); timeline.TryStart(profile); float sum = 0f; for (int i = 0; i < 120; i++) sum += timeline.Tick(1f / 120f); Assert.That(sum, Is.EqualTo(1f).Within(0.0001f));
        }
        [Test]
        public void AttackTimeline_CannotRestartWhileRunning()
        {
            var profile = AttackLibrary.Get(WeaponArchetype.Sword, AttackKind.Light); var timeline = new AttackTimeline(); Assert.IsTrue(timeline.TryStart(profile)); Assert.IsFalse(timeline.TryStart(profile)); timeline.Cancel(); Assert.IsTrue(timeline.TryStart(profile));
        }
        [Test]
        public void GuardState_RequiresFrontArcAndHasShortPerfectWindow()
        {
            var guard = new GuardState(0.16f, 120f); guard.Begin(10f); Assert.AreEqual(GuardResolution.Perfect, guard.Resolve(Vector3.forward, Vector3.forward, 10.10f)); Assert.AreEqual(GuardResolution.Block, guard.Resolve(Vector3.forward, Vector3.forward, 10.30f)); Assert.AreEqual(GuardResolution.None, guard.Resolve(Vector3.forward, Vector3.back, 10.10f)); guard.End(); Assert.AreEqual(GuardResolution.None, guard.Resolve(Vector3.forward, Vector3.forward, 10.11f));
        }
        [Test]
        public void PoiseState_BreaksThenRecoversAfterDelay()
        {
            var poise = new PoiseState(100f, 0.5f, 40f); Assert.IsFalse(poise.Apply(60f)); Assert.IsTrue(poise.Apply(40f)); Assert.AreEqual(0f, poise.Current); poise.Tick(0.4f); Assert.AreEqual(0f, poise.Current); poise.Tick(0.2f); Assert.Greater(poise.Current, 0f);
        }
        [Test]
        public void ExecutionState_FiresStrikeExactlyOnce()
        {
            var state = new ExecutionState(); Assert.IsTrue(state.TryStart(1f, 0.5f)); int strikes = 0; for (int i = 0; i < 10; i++) { ExecutionTick tick = state.Tick(0.1f); if (tick.StrikeNow) strikes++; } Assert.AreEqual(1, strikes); Assert.IsFalse(state.IsActive);
        }
        [Test]
        public void Combatant_LowHealthPoiseBreakCreatesExecutionWindow()
        {
            GameObject root = new("ExecutionTarget");
            try
            {
                root.AddComponent<CharacterController>(); EnemyGladiator enemy = root.AddComponent<EnemyGladiator>(); ImpactResult result = enemy.ReceiveImpact(new CombatImpact(null, 80f, 200f, Vector3.zero, 1f)); Assert.IsTrue(result.Accepted); Assert.IsTrue(result.PoiseBroken); Assert.IsFalse(enemy.IsDead); Assert.IsTrue(enemy.ExecutionReady); ImpactResult execution = enemy.ReceiveImpact(new CombatImpact(null, 999f, 999f, Vector3.zero, 3f, true, true)); Assert.IsTrue(execution.Killed); Assert.IsTrue(enemy.IsDead);
            }
            finally { Object.DestroyImmediate(root); }
        }
        [Test]
        public void AttackLibrary_GivesDistinctWeaponTimingsAndReachBehavior()
        {
            AttackProfile sword = AttackLibrary.Get(WeaponArchetype.Sword, AttackKind.Heavy); AttackProfile mace = AttackLibrary.Get(WeaponArchetype.Mace, AttackKind.Heavy); AttackProfile spear = AttackLibrary.Get(WeaponArchetype.Spear, AttackKind.Light); Assert.Greater(mace.Total, sword.Total); Assert.Greater(spear.RootMotionDistance, AttackLibrary.Get(WeaponArchetype.Sword, AttackKind.Light).RootMotionDistance); Assert.Greater(mace.PoiseDamage, sword.PoiseDamage);
        }
    }
}
