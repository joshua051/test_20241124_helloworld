using System;
using IronSand.Combat;
using UnityEngine;

namespace IronSand.Scoring
{
    public sealed class CombatStyleSystem : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float comboWindowSeconds = 2.5f;

        private StyleScoreModel model;

        public event Action StyleChanged;

        public int Score => model?.Score ?? 0;
        public int ComboCount => model?.ComboCount ?? 0;
        public string Rank => model?.Rank ?? "E";

        private void Awake()
        {
            model = new StyleScoreModel(comboWindowSeconds);
        }

        private void Update()
        {
            if (model != null && model.Tick(Time.time))
            {
                StyleChanged?.Invoke();
            }
        }

        public StyleAward RegisterHit(AttackKind attack, WeaponArchetype weapon, bool kill)
        {
            if (model == null)
            {
                model = new StyleScoreModel(comboWindowSeconds);
            }

            StyleAward award = model.RegisterHit(attack, weapon, kill, Time.time);
            StyleChanged?.Invoke();
            return award;
        }

        public void ResetStyle()
        {
            model?.Reset();
            StyleChanged?.Invoke();
        }
    }
}
