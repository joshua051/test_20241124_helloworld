using System;
using IronSand.Combat;

namespace IronSand.Scoring
{
    public readonly struct StyleAward
    {
        public StyleAward(int points, int favor, int combo, string rank, bool varied) { Points = points; Favor = favor; Combo = combo; Rank = rank; Varied = varied; }
        public int Points { get; } public int Favor { get; } public int Combo { get; } public string Rank { get; } public bool Varied { get; }
    }
    public sealed class StyleScoreModel
    {
        private readonly float comboWindowSeconds; private bool hasPreviousAttack; private AttackKind lastAttack; private WeaponArchetype lastWeapon; private float lastHitTime = float.NegativeInfinity; private int varietyStreak;
        public StyleScoreModel(float comboWindowSeconds) { this.comboWindowSeconds = Math.Max(0.1f, comboWindowSeconds); }
        public int Score { get; private set; } public int ComboCount { get; private set; } public string Rank => CalculateRank(ComboCount, varietyStreak);
        public StyleAward RegisterHit(AttackKind attack, WeaponArchetype weapon, bool kill, float timeSeconds)
        {
            if (!Finite(timeSeconds)) throw new ArgumentOutOfRangeException(nameof(timeSeconds));
            if (timeSeconds - lastHitTime > comboWindowSeconds) { ComboCount = 0; varietyStreak = 0; hasPreviousAttack = false; }
            ComboCount++; bool varied = !hasPreviousAttack || attack != lastAttack || weapon != lastWeapon; varietyStreak = varied ? Math.Min(99, varietyStreak + 1) : 0;
            int basePoints = attack switch { AttackKind.Heavy => 130, AttackKind.Throw => 170, AttackKind.Execution => 320, AttackKind.PerfectGuard => 180, _ => 80 };
            int comboBonus = Math.Min(10, ComboCount - 1) * 12; int varietyBonus = varied ? 30 + Math.Min(4, varietyStreak) * 10 : -25; int killBonus = kill ? 140 : 0; int points = Math.Max(25, basePoints + comboBonus + varietyBonus + killBonus); int favor = Math.Max(1, (points + 49) / 50);
            Score = SaturatingAdd(Score, points); lastAttack = attack; lastWeapon = weapon; lastHitTime = timeSeconds; hasPreviousAttack = true; return new StyleAward(points, favor, ComboCount, Rank, varied);
        }
        public bool Tick(float timeSeconds)
        {
            if (!Finite(timeSeconds)) throw new ArgumentOutOfRangeException(nameof(timeSeconds)); if (ComboCount == 0 || timeSeconds - lastHitTime <= comboWindowSeconds) return false; ComboCount = 0; varietyStreak = 0; hasPreviousAttack = false; return true;
        }
        public void Reset() { Score = 0; ComboCount = 0; varietyStreak = 0; hasPreviousAttack = false; lastHitTime = float.NegativeInfinity; }
        private static int SaturatingAdd(int left, int right) { long total = (long)left + right; return total > int.MaxValue ? int.MaxValue : total < int.MinValue ? int.MinValue : (int)total; }
        private static string CalculateRank(int combo, int variety) { int momentum = combo + variety; if (momentum >= 16) return "S"; if (momentum >= 12) return "A"; if (momentum >= 8) return "B"; if (momentum >= 5) return "C"; if (momentum >= 2) return "D"; return "E"; }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
