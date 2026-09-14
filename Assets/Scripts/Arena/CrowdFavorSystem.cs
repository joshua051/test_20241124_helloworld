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

        public event Action<int, int> FavorChanged;
        public event Action<float> RewardEarned;

        public int Favor { get; private set; }
        public int MaxFavor => maxFavor;

        private void Awake()
        {
            nextRewardThreshold = rewardStep;
        }

        public void AddFavor(int amount)
        {
            if (amount <= 0 || Favor >= maxFavor)
            {
                return;
            }

            int previous = Favor;
            Favor = Mathf.Clamp(Favor + amount, 0, maxFavor);
            FavorChanged?.Invoke(Favor, maxFavor);

            while (previous < nextRewardThreshold && Favor >= nextRewardThreshold)
            {
                RewardEarned?.Invoke(healReward);
                nextRewardThreshold += rewardStep;
            }
        }

        public void ResetFavor()
        {
            Favor = 0;
            nextRewardThreshold = rewardStep;
            FavorChanged?.Invoke(Favor, maxFavor);
        }
    }
}
