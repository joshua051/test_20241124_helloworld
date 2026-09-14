using System;
using UnityEngine;

namespace IronSand.Combat
{
    public enum GuardResolution { None = 0, Block = 1, Perfect = 2 }
    public sealed class GuardState
    {
        private readonly float perfectWindowSeconds;
        private readonly float halfArcCosine;
        private float startedAt = float.NegativeInfinity;
        public GuardState(float perfectWindowSeconds, float totalArcDegrees)
        {
            if (!Finite(perfectWindowSeconds) || perfectWindowSeconds <= 0f) throw new ArgumentOutOfRangeException(nameof(perfectWindowSeconds));
            if (!Finite(totalArcDegrees) || totalArcDegrees <= 0f || totalArcDegrees > 180f) throw new ArgumentOutOfRangeException(nameof(totalArcDegrees));
            this.perfectWindowSeconds = perfectWindowSeconds;
            halfArcCosine = Mathf.Cos(totalArcDegrees * 0.5f * Mathf.Deg2Rad);
        }
        public bool IsGuarding { get; private set; }
        public void Begin(float timeSeconds)
        {
            if (!Finite(timeSeconds)) throw new ArgumentOutOfRangeException(nameof(timeSeconds));
            if (IsGuarding) return;
            IsGuarding = true; startedAt = timeSeconds;
        }
        public void End() { IsGuarding = false; startedAt = float.NegativeInfinity; }
        public GuardResolution Resolve(Vector3 forward, Vector3 toSource, float timeSeconds)
        {
            if (!IsGuarding || !Finite(timeSeconds) || toSource.sqrMagnitude < 0.0001f) return GuardResolution.None;
            forward.y = 0f; toSource.y = 0f;
            if (forward.sqrMagnitude < 0.0001f || toSource.sqrMagnitude < 0.0001f) return GuardResolution.None;
            if (Vector3.Dot(forward.normalized, toSource.normalized) < halfArcCosine) return GuardResolution.None;
            return timeSeconds - startedAt <= perfectWindowSeconds ? GuardResolution.Perfect : GuardResolution.Block;
        }
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
