using IronSand.Combat;
using UnityEngine;

namespace IronSand.Arena
{
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public sealed class CrowdWeaponGift : MonoBehaviour
    {
        private WeaponArchetype weapon = WeaponArchetype.Sword;
        private bool resolved;
        private float life = 8f;

        public void Initialize(WeaponArchetype value)
        {
            weapon = value == WeaponArchetype.Unarmed ? WeaponArchetype.Sword : value;
        }

        private void Update()
        {
            if (resolved) return;
            life -= Time.deltaTime;
            if (life <= 0f || transform.position.y < -4f)
                Resolve(transform.position);
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (resolved) return;
            Vector3 point = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position;
            Resolve(point + Vector3.up * 0.25f);
        }

        private void Resolve(Vector3 position)
        {
            if (resolved) return;
            resolved = true;
            position.y = 0.35f;
            WeaponPickup.Spawn(position, weapon);
            Destroy(gameObject);
        }
    }
}
