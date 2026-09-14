using System;

namespace IronSand.Combat
{
    public sealed class AttackInputBuffer
    {
        private AttackKind queuedKind;
        private float remaining;
        public bool HasQueued => remaining > 0f;
        public void Queue(AttackKind kind, float lifetimeSeconds)
        {
            if (!Finite(lifetimeSeconds) || lifetimeSeconds <= 0f)
                throw new ArgumentOutOfRangeException(nameof(lifetimeSeconds));
            queuedKind = kind;
            remaining = lifetimeSeconds;
        }
        public void Tick(float deltaTime)
        {
            if (!Finite(deltaTime) || deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            remaining = Math.Max(0f, remaining - deltaTime);
        }
        public bool TryConsume(out AttackKind kind)
        {
            if (!HasQueued)
            {
                kind = default;
                return false;
            }
            kind = queuedKind;
            remaining = 0f;
            return true;
        }
        public void Clear() { remaining = 0f; }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
