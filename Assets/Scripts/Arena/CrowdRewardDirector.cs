using System.Collections.Generic;
using IronSand.Combat;
using IronSand.Player;
using IronSand.Telemetry;
using UnityEngine;

namespace IronSand.Arena
{
    public sealed class CrowdRewardDirector : MonoBehaviour
    {
        private readonly Queue<float> pendingRewards = new();
        private CrowdFavorSystem favor;
        private PlayerGladiator player;
        private int rewardIndex;
        public int PendingRewards => pendingRewards.Count;

        private void Start()
        {
            favor = FindFirstObjectByType<CrowdFavorSystem>();
            player = FindFirstObjectByType<PlayerGladiator>();
            if (favor != null) favor.RewardEarned += OnRewardEarned;
        }

        private void OnDestroy()
        {
            if (favor != null) favor.RewardEarned -= OnRewardEarned;
        }

        private void OnRewardEarned(float healAmount)
        {
            pendingRewards.Enqueue(Mathf.Max(0f, healAmount));
            TelemetryRecorder.RecordEvent("crowd_reward_queued", "Crowd", player != null ? player.name : null, null, null, null, null,
                player != null ? player.transform.position : Vector3.zero,
                $"{{\"pending\":{pendingRewards.Count},\"heal_value\":{Mathf.Max(0f, healAmount):0.###}}}");
        }

        public bool TryAppeal()
        {
            if (pendingRewards.Count == 0 || player == null || player.IsDead) return false;
            float healAmount = pendingRewards.Dequeue();
            rewardIndex++;
            float angle = rewardIndex * 137.5f * Mathf.Deg2Rad;
            float radius = ArenaGeometry.WallRadius - 1.2f;
            Vector3 origin = new(Mathf.Cos(angle) * radius, 6.5f, Mathf.Sin(angle) * radius);
            Vector3 target = player.transform.position + Vector3.up * 0.55f;
            bool weaponGift = rewardIndex % 2 == 0;
            if (weaponGift)
                SpawnWeaponGift(origin, target, WeaponCatalog.GetArenaWeapon(rewardIndex * 31));
            else
                SpawnFoodGift(origin, target, healAmount);
            TelemetryRecorder.RecordEvent("crowd_appeal", player.name, "Crowd", null, null, null, null, player.transform.position,
                $"{{\"gift_type\":\"{(weaponGift ? "weapon" : "food")}\",\"pending_after\":{pendingRewards.Count}}}");
            return true;
        }

        private void SpawnFoodGift(Vector3 origin, Vector3 target, float healAmount)
        {
            GameObject gift = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            gift.name = "CrowdGift_Food";
            gift.transform.position = origin;
            gift.transform.localScale = Vector3.one * 0.42f;
            Rigidbody body = gift.AddComponent<Rigidbody>();
            body.mass = 0.45f;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.linearVelocity = BallisticVelocity(origin, target, 1.05f);
            gift.AddComponent<CrowdGift>().Initialize(healAmount, player);
            TelemetryRecorder.RecordEvent("crowd_gift_launched", "Crowd", player != null ? player.name : null, null, null, healAmount, null, origin,
                "{\"gift_type\":\"food\"}");
        }

        private void SpawnWeaponGift(Vector3 origin, Vector3 target, WeaponArchetype weapon)
        {
            GameObject gift = GameObject.CreatePrimitive(PrimitiveType.Cube);
            gift.name = $"CrowdGift_Weapon_{weapon}";
            gift.transform.position = origin;
            gift.transform.localScale = WeaponPickup.GetPickupScale(weapon) * 0.72f;
            Rigidbody body = gift.AddComponent<Rigidbody>();
            body.mass = 1.1f;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.angularVelocity = new Vector3(4f, 8f, 6f);
            body.linearVelocity = BallisticVelocity(origin, target, 1.10f);
            gift.AddComponent<CrowdWeaponGift>().Initialize(weapon);
            TelemetryRecorder.RecordEvent("crowd_gift_launched", "Crowd", player != null ? player.name : null, weapon, null, null, null, origin,
                "{\"gift_type\":\"weapon\"}");
        }

        private static Vector3 BallisticVelocity(Vector3 origin, Vector3 target, float flightTime)
        {
            Vector3 gravity = Physics.gravity;
            return (target - origin - 0.5f * gravity * flightTime * flightTime) / flightTime;
        }
    }
}
