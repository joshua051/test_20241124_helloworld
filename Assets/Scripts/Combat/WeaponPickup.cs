using UnityEngine;

namespace IronSand.Combat
{
    public sealed class WeaponPickup : MonoBehaviour
    {
        [SerializeField] private WeaponArchetype archetype = WeaponArchetype.Sword;
        [SerializeField, Min(1)] private int remainingDurability = 1;
        [SerializeField, Min(0f)] private float spinDegreesPerSecond = 55f;
        [SerializeField, Min(0f)] private float bobAmplitude = 0.08f;

        private float baseY;
        private float phase;
        private bool consumed;

        public WeaponArchetype Archetype => archetype;
        public int RemainingDurability => remainingDurability;

        private void Awake()
        {
            baseY = transform.position.y;
            phase = Random.value * Mathf.PI * 2f;
        }

        private void Update()
        {
            if (consumed)
            {
                return;
            }

            transform.Rotate(Vector3.up, spinDegreesPerSecond * Time.deltaTime, Space.World);
            Vector3 position = transform.position;
            position.y = baseY + Mathf.Sin(Time.time * 2.2f + phase) * bobAmplitude;
            transform.position = position;
        }

        public void Configure(WeaponArchetype value, int durability = -1)
        {
            archetype = value == WeaponArchetype.Unarmed ? WeaponArchetype.Sword : value;
            WeaponStats stats = WeaponCatalog.Get(archetype);
            remainingDurability = durability > 0
                ? Mathf.Clamp(durability, 1, stats.MaxDurability)
                : stats.MaxDurability;
            gameObject.name = $"Pickup_{archetype}";
        }

        public void Consume()
        {
            if (consumed)
            {
                return;
            }

            consumed = true;
            Destroy(gameObject);
        }

        public static WeaponPickup Spawn(Vector3 position, WeaponArchetype archetype, int durability = -1)
        {
            if (archetype == WeaponArchetype.Unarmed)
            {
                return null;
            }

            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            root.transform.position = position;
            root.transform.localScale = GetPickupScale(archetype);

            Collider collider = root.GetComponent<Collider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }

            WeaponPickup pickup = root.AddComponent<WeaponPickup>();
            pickup.baseY = position.y;
            pickup.Configure(archetype, durability);
            return pickup;
        }

        private static Vector3 GetPickupScale(WeaponArchetype archetype)
        {
            return archetype switch
            {
                WeaponArchetype.Spear => new Vector3(0.18f, 0.18f, 1.8f),
                WeaponArchetype.Axe => new Vector3(0.55f, 0.15f, 0.85f),
                WeaponArchetype.Mace => new Vector3(0.28f, 0.28f, 0.9f),
                _ => new Vector3(0.18f, 0.12f, 1.05f)
            };
        }
    }
}
