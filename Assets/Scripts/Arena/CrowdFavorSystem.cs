using System;
using UnityEngine;

namespace IronSand.Arena
{
    public sealed class CrowdFavorSystem : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maxFavor = 100;
        [SerializeField, Min(1)] private int rewardStep = 25;
        [SerializeField, Min(0f)] private float healReward = 15f;
        private int nextRewardThreshold;
        private int mutationVersion;
        public event Action<int, int> FavorChanged;
        public event Action<float> RewardEarned;
        public int Favor { get; private set; }
        public int MaxFavor => Mathf.Max(1, maxFavor);
        private void Awake() { EnsureInitialized(); }
        public void AddFavor(int amount)
        {
            EnsureInitialized(); if (amount <= 0 || Favor >= MaxFavor) return;
            int version = mutationVersion; long raw = (long)Favor + amount; int previous = Favor; Favor = raw >= MaxFavor ? MaxFavor : (int)Math.Max(0L, raw);
            int rewards = 0; int step = Mathf.Max(1, rewardStep);
            while (previous < nextRewardThreshold && Favor >= nextRewardThreshold)
            {
                rewards++;
                if (nextRewardThreshold > MaxFavor - step) { nextRewardThreshold = int.MaxValue; break; }
                nextRewardThreshold += step;
            }
            FavorChanged?.Invoke(Favor, MaxFavor);
            for (int i = 0; i < rewards; i++) { if (version != mutationVersion) break; RewardEarned?.Invoke(Mathf.Max(0f, healReward)); }
        }
        public void ResetFavor() { mutationVersion++; Favor = 0; nextRewardThreshold = Mathf.Max(1, rewardStep); FavorChanged?.Invoke(Favor, MaxFavor); }
        private void EnsureInitialized() { if (nextRewardThreshold <= 0) nextRewardThreshold = Mathf.Max(1, rewardStep); }
    }
}
