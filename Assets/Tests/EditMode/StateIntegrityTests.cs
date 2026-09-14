using IronSand.Arena;
using IronSand.Combat;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace IronSand.Tests
{
    public sealed class StateIntegrityTests
    {
        [Test]
        public void WeaponPickup_CanOnlyBeClaimedOnce()
        {
            WeaponPickup pickup = WeaponPickup.Spawn(Vector3.zero, WeaponArchetype.Axe, 5); Assert.IsNotNull(pickup);
            try { Assert.IsTrue(pickup.TryClaim(out WeaponArchetype firstWeapon, out int firstDurability)); Assert.AreEqual(WeaponArchetype.Axe, firstWeapon); Assert.AreEqual(5, firstDurability); Assert.IsFalse(pickup.TryClaim(out _, out _)); }
            finally { if (pickup != null) Object.DestroyImmediate(pickup.gameObject); }
        }
        [Test] public void BrokenWeaponPickup_IsNeverSpawned() { Assert.IsNull(WeaponPickup.Spawn(Vector3.zero, WeaponArchetype.Sword, 0)); }
        [Test]
        public void CrowdFavor_SaturatesOnHugeInput()
        {
            GameObject root = new("CrowdFavorOverflow"); try { CrowdFavorSystem favor = root.AddComponent<CrowdFavorSystem>(); favor.AddFavor(int.MaxValue); Assert.AreEqual(favor.MaxFavor, favor.Favor); } finally { Object.DestroyImmediate(root); }
        }
        [Test]
        public void CrowdFavor_ResetInsideRewardCallbackStopsStaleRewardLoop()
        {
            GameObject root = new("CrowdFavorReentrantReset");
            try
            {
                CrowdFavorSystem favor = root.AddComponent<CrowdFavorSystem>(); int rewards = 0; favor.RewardEarned += _ => { rewards++; if (rewards == 1) favor.ResetFavor(); }; favor.AddFavor(100); Assert.AreEqual(1, rewards); Assert.AreEqual(0, favor.Favor); favor.AddFavor(25); Assert.AreEqual(2, rewards);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
