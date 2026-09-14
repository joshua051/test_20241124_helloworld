using System;

namespace IronSand.Combat
{
    public sealed class AttackTimeline
    {
        private AttackProfile profile;
        private float elapsed;
        private bool hasProfile;
        public bool IsRunning { get; private set; }
        public CombatPhase Phase
        {
            get
            {
                if (!IsRunning || !hasProfile) return CombatPhase.Idle;
                if (elapsed < profile.Startup) return CombatPhase.Startup;
                if (elapsed < profile.Startup + profile.Active) return CombatPhase.Active;
                return CombatPhase.Recovery;
            }
        }
        public AttackProfile Profile => hasProfile ? profile : throw new InvalidOperationException("No attack profile is active.");
        public float Normalized => !hasProfile ? 0f : Clamp01(elapsed / profile.Total);
        public float PhaseProgress => !hasProfile ? 0f : Phase switch { CombatPhase.Startup => Clamp01(elapsed / profile.Startup), CombatPhase.Active => Clamp01((elapsed - profile.Startup) / profile.Active), CombatPhase.Recovery => Clamp01((elapsed - profile.Startup - profile.Active) / profile.Recovery), _ => 0f };
        public bool TryStart(AttackProfile value)
        {
            if (IsRunning) return false;
            profile = value; hasProfile = true; elapsed = 0f; IsRunning = true; return true;
        }
        public float Tick(float deltaTime)
        {
            if (!Finite(deltaTime) || deltaTime < 0f) throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (!IsRunning) return 0f;
            float before = MotionProgress(elapsed);
            float afterElapsed = Math.Min(profile.Total, elapsed + deltaTime);
            float after = MotionProgress(afterElapsed);
            elapsed = afterElapsed;
            if (elapsed >= profile.Total) IsRunning = false;
            return Math.Max(0f, after - before);
        }
        public void Cancel() { IsRunning = false; elapsed = 0f; }
        private float MotionProgress(float value) { float motionEnd = profile.Startup + profile.Active; return motionEnd <= 0f ? 1f : Clamp01(value / motionEnd); }
        private static float Clamp01(float value) => Math.Min(1f, Math.Max(0f, value));
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
