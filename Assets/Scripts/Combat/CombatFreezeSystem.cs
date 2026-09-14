using UnityEngine;

namespace IronSand.Combat
{
    [DefaultExecutionOrder(-900)]
    public sealed class CombatFreezeSystem : MonoBehaviour
    {
        private static CombatFreezeSystem instance;
        private float remaining;
        public static bool IsFrozen => instance != null && instance.remaining > 0f;
        private void Awake() { instance = this; }
        private void Update() { remaining = Mathf.Max(0f, remaining - Time.unscaledDeltaTime); }
        private void OnDestroy() { if (instance == this) instance = null; }
        public static void Request(float seconds)
        {
            if (instance == null || seconds <= 0f || float.IsNaN(seconds) || float.IsInfinity(seconds)) return;
            instance.remaining = Mathf.Max(instance.remaining, Mathf.Min(seconds, 0.14f));
        }
    }
}
