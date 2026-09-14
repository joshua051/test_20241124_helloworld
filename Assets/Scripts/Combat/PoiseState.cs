using System;

namespace IronSand.Combat
{
    public sealed class PoiseState
    {
        private readonly float max;
        private readonly float recoveryDelay;
        private readonly float recoveryPerSecond;
        private float recoveryClock;
        public PoiseState(float max, float recoveryDelay, float recoveryPerSecond)
        {
            if (!Finite(max) || max <= 0f) throw new ArgumentOutOfRangeException(nameof(max));
            if (!Finite(recoveryDelay) || recoveryDelay < 0f) throw new ArgumentOutOfRangeException(nameof(recoveryDelay));
            if (!Finite(recoveryPerSecond) || recoveryPerSecond < 0f) throw new ArgumentOutOfRangeException(nameof(recoveryPerSecond));
            this.max = max; this.recoveryDelay = recoveryDelay; this.recoveryPerSecond = recoveryPerSecond; Current = max;
        }
        public float Max => max;
        public float Current { get; private set; }
        public bool Apply(float amount)
        {
            if (!Finite(amount) || amount < 0f) throw new ArgumentOutOfRangeException(nameof(amount));
            if (amount <= 0f) return false;
            recoveryClock = 0f; Current = Math.Max(0f, Current - amount); return Current <= 0f;
        }
        public void Tick(float deltaTime)
        {
            if (!Finite(deltaTime) || deltaTime < 0f) throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (Current >= max || recoveryPerSecond <= 0f) return;
            recoveryClock += deltaTime;
            if (recoveryClock <= recoveryDelay) return;
            Current = Math.Min(max, Current + recoveryPerSecond * deltaTime);
        }
        public void Restore() { Current = max; recoveryClock = 0f; }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
