using System.Collections.Generic;
using IronSand.Arena;
using IronSand.Combat;
using IronSand.Player;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace IronSand.Tests
{
    // These are Unity PlayMode tests. The .NET runner does NOT execute them.
    public sealed class ArenaStateIntegrityTests
    {
        private readonly List<GameObject> owned = new();
        private static readonly Vector3 Origin = new(1000f, 10f, 1000f);

        private GameObject Make(string name)
        {
            var root = new GameObject(name);
            root.transform.position = Origin;
            owned.Add(root);
            return root;
        }

        private WeaponPickup Pickup(Vector3 offset, WeaponArchetype kind, int durability = -1)
        {
            var pickup = WeaponPickup.Spawn(Origin + offset, kind, durability);
            Assert.IsNotNull(pickup);
            owned.Add(pickup.gameObject);
            return pickup;
        }

        [TearDown]
        public void Cleanup()
        {
            foreach (var root in owned) if (root != null) Object.DestroyImmediate(root);
            owned.Clear();
        }

        [Test]
        public void FatalObserverCannotResurrectOrApplyDamageAgain()
        {
            var actor = Make("HealthProbe").AddComponent<ArenaHealthProbe>();
            int events = 0;
            actor.Died += _ => events++;
            actor.HealthChanged += (hp, _) =>
            {
                if (hp > 0f) return;
                Assert.IsTrue(actor.IsDead);
                actor.Heal(100f);
                Assert.IsFalse(actor.ApplyDamage(1f, Vector3.zero));
            };
            Assert.IsTrue(actor.ApplyDamage(101f, Vector3.zero));
            Assert.AreEqual(0f, actor.Health);
            Assert.AreEqual(1, events);
            Assert.AreEqual(1, actor.DeathCallbacks);
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void InvalidNumbersDoNotPoisonHealthOrInvulnerability(float invalid)
        {
            var actor = Make("InvalidHealthProbe").AddComponent<ArenaHealthProbe>();
            Assert.IsFalse(actor.ApplyDamage(invalid, Vector3.zero));
            actor.Heal(invalid);
            actor.GrantInvulnerability(invalid);
            Assert.AreEqual(100f, actor.Health);
            Assert.IsFalse(actor.IsInvulnerable);
        }

        [Test]
        public void PickupIsClaimedOnlyOnceWithinTheSameFrame()
        {
            var pickup = Pickup(Vector3.forward, WeaponArchetype.Axe, 5);
            Assert.IsTrue(pickup.TryClaim(out var kind, out var durability));
            Assert.AreEqual(WeaponArchetype.Axe, kind);
            Assert.AreEqual(5, durability);
            Assert.IsFalse(pickup.IsAvailable);
            Assert.IsFalse(pickup.gameObject.activeSelf);
            Assert.IsFalse(pickup.TryClaim(out _, out _));
        }

        [Test]
        public void CannotPickUpThroughSolidScenery()
        {
            var controller = Make("PickupActor").AddComponent<PlayerWeaponController>();
            var pickup = Pickup(Vector3.forward * 1.4f, WeaponArchetype.Axe);
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            owned.Add(wall);
            wall.transform.position = Origin + Vector3.forward * 0.7f;
            wall.transform.localScale = new Vector3(4f, 4f, 0.2f);
            Physics.SyncTransforms();
            Assert.IsFalse(controller.TryPickupNearest(Origin, 2f, Origin + Vector3.back));
            Assert.IsTrue(pickup.IsAvailable);
            Assert.AreEqual(WeaponArchetype.Sword, controller.CurrentWeapon);
        }

        [Test]
        public void LongPickupColliderDoesNotExtendInteractionRadius()
        {
            var controller = Make("RangeActor").AddComponent<PlayerWeaponController>();
            var pickup = Pickup(Vector3.forward * 2.5f, WeaponArchetype.Spear);
            Physics.SyncTransforms();
            Assert.IsFalse(controller.TryPickupNearest(Origin, 2f, Origin + Vector3.back));
            Assert.IsTrue(pickup.IsAvailable);
        }

        [Test]
        public void SuccessfulSwapPreservesBothWeaponDurabilities()
        {
            var controller = Make("SwapActor").AddComponent<PlayerWeaponController>();
            controller.ConsumeDurability(2);
            int oldDurability = controller.Durability;
            Pickup(Vector3.forward, WeaponArchetype.Axe, 5);
            Physics.SyncTransforms();
            Assert.IsTrue(controller.TryPickupNearest(Origin, 2f, Origin + Vector3.back));
            var nearby = Object.FindObjectsByType<WeaponPickup>(FindObjectsSortMode.None);
            WeaponPickup dropped = null;
            foreach (var item in nearby)
            {
                if ((item.transform.position - Origin).sqrMagnitude > 10f) continue;
                if (!owned.Contains(item.gameObject)) owned.Add(item.gameObject);
                if (item.Archetype == WeaponArchetype.Sword) dropped = item;
            }
            Assert.IsNotNull(dropped);
            Assert.AreEqual(oldDurability, dropped.RemainingDurability);
            Assert.AreEqual(WeaponArchetype.Axe, controller.CurrentWeapon);
            Assert.AreEqual(5, controller.Durability);
        }

        [Test]
        public void DurabilityChangesReuseVisualUntilWeaponBreaks()
        {
            var actor = Make("VisualActor");
            var controller = actor.AddComponent<PlayerWeaponController>();
            var original = actor.transform.Find("WeaponVisual_Sword");
            Assert.IsNotNull(original);
            controller.ConsumeDurability(1);
            Assert.AreSame(original, actor.transform.Find("WeaponVisual_Sword"));
            Assert.IsTrue(original.gameObject.activeSelf);
            controller.Equip(WeaponArchetype.Sword, 0);
            Assert.AreEqual(WeaponArchetype.Unarmed, controller.CurrentWeapon);
            Assert.AreEqual(0, controller.Durability);
            Assert.IsFalse(original.gameObject.activeSelf);
        }

        [Test]
        public void ZeroDurabilityCannotSpawnAFreshWeapon()
        {
            Assert.IsNull(WeaponPickup.Spawn(Origin, WeaponArchetype.Sword, 0));
        }

        [Test]
        public void CrowdRulesWorkBeforeAwakeWithoutResettingOnActivation()
        {
            var root = Make("InactiveCrowd");
            root.SetActive(false);
            var crowd = root.AddComponent<CrowdFavorSystem>();
            int rewards = 0;
            crowd.RewardEarned += _ => rewards++;
            crowd.AddFavor(25);
            Assert.AreEqual(1, rewards);
            root.SetActive(true);
            crowd.AddFavor(25);
            Assert.AreEqual(2, rewards);
            Assert.AreEqual(50, crowd.Favor);
        }

        [Test]
        public void ResetInsideRewardDoesNotCorruptNextRunThreshold()
        {
            var crowd = Make("ReentrantCrowd").AddComponent<CrowdFavorSystem>();
            int rewards = 0;
            crowd.RewardEarned += _ => { if (++rewards == 1) crowd.ResetFavor(); };
            crowd.AddFavor(50);
            Assert.AreEqual(1, rewards);
            Assert.AreEqual(0, crowd.Favor);
            crowd.AddFavor(25);
            Assert.AreEqual(2, rewards);
            Assert.AreEqual(25, crowd.Favor);
        }

        [Test]
        public void BlockedDropPreservesOldWeaponAtAccessiblePickupPosition()
        {
            var controller = Make("BlockedDropActor").AddComponent<PlayerWeaponController>();
            controller.ConsumeDurability(2);
            int oldDurability = controller.Durability;
            var pickup = Pickup(Vector3.forward, WeaponArchetype.Axe, 5);
            Vector3 availablePosition = pickup.transform.position;
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            owned.Add(wall);
            wall.transform.position = Origin + Vector3.back * 0.6f;
            wall.transform.localScale = new Vector3(4f, 4f, 0.2f);
            Physics.SyncTransforms();
            Assert.IsTrue(CombatQueries.WorldBlocks(Origin, Origin + Vector3.back));
            Assert.IsTrue(controller.TryPickupNearest(Origin, 2f, Origin + Vector3.back));
            WeaponPickup dropped = null;
            foreach (var item in Object.FindObjectsByType<WeaponPickup>(FindObjectsSortMode.None))
            {
                if ((item.transform.position - Origin).sqrMagnitude > 10f) continue;
                if (!owned.Contains(item.gameObject)) owned.Add(item.gameObject);
                if (item.Archetype == WeaponArchetype.Sword) dropped = item;
            }
            Assert.IsNotNull(dropped, "A blocked drop must not silently delete the old weapon.");
            Assert.AreEqual(oldDurability, dropped.RemainingDurability);
            Assert.That(Vector3.Distance(availablePosition, dropped.transform.position), Is.LessThan(0.001f));
        }

        [Test]
        public void InvalidWeaponAndNegativeSentinelDoNotCreateUsableItems()
        {
            var controller = Make("InvalidWeaponActor").AddComponent<PlayerWeaponController>();
            controller.Equip((WeaponArchetype)999, 5);
            Assert.AreEqual(WeaponArchetype.Unarmed, controller.CurrentWeapon);
            Assert.AreEqual(0, controller.Durability);
            Assert.IsNull(WeaponPickup.Spawn(Origin, (WeaponArchetype)999, 5));
            Assert.IsNull(WeaponPickup.Spawn(Origin, WeaponArchetype.Axe, -2));
        }

        [Test]
        public void InactivePickupCannotBeClaimed()
        {
            var pickup = Pickup(Vector3.forward, WeaponArchetype.Axe, 5);
            pickup.gameObject.SetActive(false);
            Assert.IsFalse(pickup.TryClaim(out _, out _));
            Assert.IsFalse(pickup.IsConsumed);
            pickup.gameObject.SetActive(true);
            Assert.IsTrue(pickup.TryClaim(out _, out _));
            Assert.IsFalse(pickup.Configure(WeaponArchetype.Sword, 5));
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void InvalidKnockbackCannotReachDamageCallback(float invalid)
        {
            var actor = Make("InvalidKnockbackProbe").AddComponent<ArenaHealthProbe>();
            Assert.IsFalse(actor.ApplyDamage(1f, new Vector3(invalid, 0f, 0f)));
            Assert.AreEqual(0, actor.DamageCallbacks);
            Assert.AreEqual(100f, actor.Health);
        }
    }
}
