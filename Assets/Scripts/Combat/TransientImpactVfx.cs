using UnityEngine;

namespace IronSand.Combat
{
    public sealed class TransientImpactVfx : MonoBehaviour
    {
        private float duration = 0.12f;
        private float elapsed;
        private Vector3 startScale;
        public void Configure(float seconds, float size)
        {
            duration = Mathf.Max(0.04f, seconds); transform.localScale = Vector3.one * Mathf.Max(0.05f, size); startScale = transform.localScale;
        }
        private void Update()
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            transform.localScale = Vector3.Lerp(startScale, startScale * 2.8f, t);
            if (elapsed >= duration) Destroy(gameObject);
        }
    }
}
