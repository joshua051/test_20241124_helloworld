using IronSand.Player;
using UnityEngine;

namespace IronSand.Arena
{
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public sealed class CrowdGift : MonoBehaviour
    {
        private float healAmount;
        private float life = 9f;
        private PlayerGladiator player;
        private bool collected;
        public void Initialize(float heal, PlayerGladiator target) { healAmount = Mathf.Max(0f, heal); player = target; }
        private void Update()
        {
            if (collected) return; life -= Time.deltaTime;
            if (player != null && !player.IsDead && (player.transform.position - transform.position).sqrMagnitude <= 1.5f * 1.5f)
            { collected = true; player.Heal(healAmount); Destroy(gameObject); return; }
            if (life <= 0f || transform.position.y < -4f) Destroy(gameObject);
        }
    }
}
