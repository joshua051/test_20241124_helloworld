using IronSand.Arena;
using IronSand.Enemy;
using UnityEngine;

namespace IronSand.Combat
{
    [RequireComponent(typeof(Rigidbody), typeof(Collider))]
    public sealed class ThrownWeapon : MonoBehaviour
    {
        private WeaponArchetype archetype;
        private int durability;
        private Combatant owner;
        private Rigidbody body;
        private bool resolved;
        private float life = 6f;
        public static bool Spawn(Vector3 origin, Vector3 direction, WeaponArchetype weapon, int remainingDurability, Combatant source)
        {
            if (weapon == WeaponArchetype.Unarmed || remainingDurability <= 0 || direction.sqrMagnitude < 0.001f) return false;
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cube); root.name = $"Thrown_{weapon}"; root.transform.position = origin; root.transform.localScale = WeaponPickup.GetPickupScale(weapon) * 0.82f;
            Rigidbody rigidbody = root.AddComponent<Rigidbody>(); rigidbody.mass = weapon == WeaponArchetype.Mace ? 2.2f : weapon == WeaponArchetype.Axe ? 1.7f : 1.1f; rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            ThrownWeapon thrown = root.AddComponent<ThrownWeapon>(); thrown.archetype = weapon; thrown.durability = remainingDurability; thrown.owner = source; thrown.body = rigidbody;
            rigidbody.linearVelocity = direction.normalized * (weapon == WeaponArchetype.Spear ? 17f : 13f) + Vector3.up * 1.8f; rigidbody.angularVelocity = new Vector3(8f, 14f, 11f); return true;
        }
        private void Awake() { body ??= GetComponent<Rigidbody>(); }
        private void Update() { life -= Time.deltaTime; if (!resolved && life <= 0f) ResolveToPickup(transform.position); }
        private void OnCollisionEnter(Collision collision)
        {
            if (resolved) return; EnemyGladiator enemy = collision.collider.GetComponentInParent<EnemyGladiator>();
            if (enemy != null && enemy != owner && !enemy.IsDead)
            {
                WeaponStats stats = WeaponCatalog.Get(archetype); Vector3 direction = body != null && body.linearVelocity.sqrMagnitude > 0.01f ? body.linearVelocity.normalized : transform.forward;
                ImpactResult result = enemy.ReceiveImpact(new CombatImpact(owner, 26f * stats.HeavyDamageMultiplier, 58f * stats.StunMultiplier, direction * 4.2f, 1.6f));
                if (result.Accepted)
                {
                    CombatFeedbackSystem.EmitImpact(collision.contactCount > 0 ? collision.GetContact(0).point : transform.position, 0.075f, 0.25f, true);
                    FindFirstObjectByType<ArenaDirector>()?.RegisterPlayerHit(AttackKind.Throw, archetype, result.Killed, stats.FavorBonus + 1);
                }
            }
            Vector3 point = collision.contactCount > 0 ? collision.GetContact(0).point : transform.position; ResolveToPickup(point + Vector3.up * 0.25f);
        }
        private void ResolveToPickup(Vector3 position)
        {
            if (resolved) return; resolved = true; int remaining = Mathf.Max(0, durability - 2); position.y = 0.35f;
            if (remaining > 0) WeaponPickup.Spawn(position, archetype, remaining); Destroy(gameObject);
        }
    }
}
