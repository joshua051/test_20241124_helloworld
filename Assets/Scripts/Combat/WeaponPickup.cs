using UnityEngine;

namespace IronSand.Combat
{
    public sealed class WeaponPickup : MonoBehaviour
    {
        [SerializeField] private WeaponArchetype archetype = WeaponArchetype.Sword;
        [SerializeField, Min(1)] private int remainingDurability = 1;
        [SerializeField, Min(0f)] private float spinDegreesPerSecond = 55f;
        [SerializeField, Min(0f)] private float bobAmplitude = 0.08f;
        private float baseY, phase;
        private bool consumed;
        public WeaponArchetype Archetype => archetype;
        public int RemainingDurability => remainingDurability;
        public bool IsConsumed => consumed;
        private void Awake() { baseY = transform.position.y; phase = Random.value * Mathf.PI * 2f; }
        private void Update()
        {
            if (consumed) return; transform.Rotate(Vector3.up, spinDegreesPerSecond * Time.deltaTime, Space.World);
            Vector3 position = transform.position; position.y = baseY + Mathf.Sin(Time.time * 2.2f + phase) * bobAmplitude; transform.position = position;
        }
        public bool Configure(WeaponArchetype value, int durability = -1)
        {
            archetype = value == WeaponArchetype.Unarmed ? WeaponArchetype.Sword : value; WeaponStats stats = WeaponCatalog.Get(archetype);
            if (durability == 0) { remainingDurability = 0; return false; }
            remainingDurability = durability < 0 ? stats.MaxDurability : Mathf.Clamp(durability, 1, stats.MaxDurability); gameObject.name = $"Pickup_{archetype}"; return true;
        }
        public bool TryClaim(out WeaponArchetype claimedArchetype, out int claimedDurability)
        {
            claimedArchetype = WeaponArchetype.Unarmed; claimedDurability = 0; if (consumed || remainingDurability <= 0) return false;
            consumed = true; claimedArchetype = archetype; claimedDurability = remainingDurability;
            Collider collider = GetComponent<Collider>(); if (collider != null) collider.enabled = false; gameObject.SetActive(false); Destroy(gameObject); return true;
        }
        public static WeaponPickup Spawn(Vector3 position, WeaponArchetype archetype, int durability = -1)
        {
            if (archetype == WeaponArchetype.Unarmed || durability == 0) return null;
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cube); root.transform.position = position; root.transform.localScale = GetPickupScale(archetype);
            Collider collider = root.GetComponent<Collider>(); if (collider != null) collider.isTrigger = true;
            WeaponPickup pickup = root.AddComponent<WeaponPickup>(); pickup.baseY = position.y;
            if (!pickup.Configure(archetype, durability)) { Destroy(root); return null; }
            return pickup;
        }
        public static Vector3 GetPickupScale(WeaponArchetype archetype) => archetype switch { WeaponArchetype.Spear => new Vector3(0.18f, 0.18f, 1.8f), WeaponArchetype.Axe => new Vector3(0.55f, 0.15f, 0.85f), WeaponArchetype.Mace => new Vector3(0.28f, 0.28f, 0.9f), _ => new Vector3(0.18f, 0.12f, 1.05f) };
    }
}
