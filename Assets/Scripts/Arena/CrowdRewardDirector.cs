using IronSand.Player;
using UnityEngine;

namespace IronSand.Arena
{
    public sealed class CrowdRewardDirector : MonoBehaviour
    {
        private CrowdFavorSystem favor;
        private PlayerGladiator player;
        private int rewardIndex;
        private void Start() { favor = FindFirstObjectByType<CrowdFavorSystem>(); player = FindFirstObjectByType<PlayerGladiator>(); if (favor != null) favor.RewardEarned += OnReward; }
        private void OnDestroy() { if (favor != null) favor.RewardEarned -= OnReward; }
        private void OnReward(float healAmount)
        {
            if (player == null || player.IsDead) return;
            rewardIndex++; float angle = (rewardIndex * 137.5f) * Mathf.Deg2Rad; float radius = ArenaGeometry.WallRadius - 1.2f;
            Vector3 origin = new(Mathf.Cos(angle) * radius, 6.5f, Mathf.Sin(angle) * radius); Vector3 target = player.transform.position + Vector3.up * 0.55f;
            SpawnGift(origin, target, healAmount);
        }
        private void SpawnGift(Vector3 origin, Vector3 target, float healAmount)
        {
            GameObject gift = GameObject.CreatePrimitive(PrimitiveType.Sphere); gift.name = "CrowdGift_Food"; gift.transform.position = origin; gift.transform.localScale = Vector3.one * 0.42f;
            Rigidbody body = gift.AddComponent<Rigidbody>(); body.mass = 0.45f; body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            const float flightTime = 1.05f; Vector3 gravity = Physics.gravity; body.linearVelocity = (target - origin - 0.5f * gravity * flightTime * flightTime) / flightTime;
            gift.AddComponent<CrowdGift>().Initialize(healAmount, player);
        }
    }
}
