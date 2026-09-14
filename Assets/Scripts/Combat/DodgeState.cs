using System;

namespace IronSand.Combat
{
    // Pure timing rules. The returned active seconds make travel independent of frame rate.
    public sealed class DodgeState
    {
        public float ActiveRemaining { get; private set; }
        public float CooldownRemaining { get; private set; }
        public bool IsActive => ActiveRemaining > 0f;
        public bool CanStart => CooldownRemaining <= 0f;

        public bool TryStart(float duration, float cooldown)
        {
            if (!Finite(duration) || !Finite(cooldown) || duration <= 0f || cooldown <= duration)
                throw new ArgumentOutOfRangeException(nameof(duration), "Cooldown must exceed a positive duration.");
            if (!CanStart) return false;
            ActiveRemaining = duration;
            CooldownRemaining = cooldown;
            return true;
        }

        public float Tick(float deltaTime)
        {
            if (!Finite(deltaTime) || deltaTime < 0f) throw new ArgumentOutOfRangeException(nameof(deltaTime));
            float activeSeconds = Math.Min(ActiveRemaining, deltaTime);
            ActiveRemaining = Math.Max(0f, ActiveRemaining - deltaTime);
            CooldownRemaining = Math.Max(0f, CooldownRemaining - deltaTime);
            return activeSeconds;
        }

        public void CancelActive() { ActiveRemaining = 0f; }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
