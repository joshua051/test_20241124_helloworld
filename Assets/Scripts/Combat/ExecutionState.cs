using System;

namespace IronSand.Combat
{
    public readonly struct ExecutionTick
    {
        public ExecutionTick(float progress, bool strikeNow, bool completed) { Progress = progress; StrikeNow = strikeNow; Completed = completed; }
        public float Progress { get; }
        public bool StrikeNow { get; }
        public bool Completed { get; }
    }
    public sealed class ExecutionState
    {
        private float duration;
        private float strikeAt;
        private float elapsed;
        private bool strikeFired;
        public bool IsActive { get; private set; }
        public bool TryStart(float durationSeconds, float strikeAtSeconds)
        {
            if (!Finite(durationSeconds) || durationSeconds <= 0f) throw new ArgumentOutOfRangeException(nameof(durationSeconds));
            if (!Finite(strikeAtSeconds) || strikeAtSeconds <= 0f || strikeAtSeconds >= durationSeconds) throw new ArgumentOutOfRangeException(nameof(strikeAtSeconds));
            if (IsActive) return false;
            duration = durationSeconds; strikeAt = strikeAtSeconds; elapsed = 0f; strikeFired = false; IsActive = true; return true;
        }
        public ExecutionTick Tick(float deltaTime)
        {
            if (!Finite(deltaTime) || deltaTime < 0f) throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (!IsActive) return new ExecutionTick(0f, false, false);
            float previous = elapsed; elapsed = Math.Min(duration, elapsed + deltaTime);
            bool strike = !strikeFired && previous < strikeAt && elapsed >= strikeAt;
            if (strike) strikeFired = true;
            bool completed = elapsed >= duration;
            if (completed) IsActive = false;
            return new ExecutionTick(Clamp01(elapsed / duration), strike, completed);
        }
        public void Cancel() { IsActive = false; elapsed = 0f; strikeFired = false; }
        private static float Clamp01(float value) => Math.Min(1f, Math.Max(0f, value));
        private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
