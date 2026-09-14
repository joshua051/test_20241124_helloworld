using System;
using IronSand.Combat;
using UnityEngine;

namespace IronSand.Player
{
    public sealed class PlayerWeaponController : MonoBehaviour
    {
        [SerializeField] private WeaponArchetype startingWeapon = WeaponArchetype.Sword;

        public event Action<WeaponArchetype, int, int> LoadoutChanged;

        public WeaponArchetype CurrentWeapon { get; private set; }
        public int Durability { get; private set; }
        public WeaponStats CurrentStats => WeaponCatalog.Get(CurrentWeapon);
        public int MaxDurability => CurrentStats.MaxDurability;

        private void Awake()
        {
            EquipFresh(startingWeapon);
        }

        public bool TryPickupNearest(Vector3 origin, float radius, Vector3 dropPosition)
        {
            Collider[] hits = Physics.OverlapSphere(origin, radius, ~0, QueryTriggerInteraction.Collide);
            WeaponPickup nearest = null;
            float bestSqrDistance = float.PositiveInfinity;

            foreach (Collider hit in hits)
            {
                WeaponPickup pickup = hit.GetComponentInParent<WeaponPickup>();
                if (pickup == null)
                {
                    continue;
                }

                float sqrDistance = (pickup.transform.position - origin).sqrMagnitude;
                if (sqrDistance < bestSqrDistance)
                {
                    bestSqrDistance = sqrDistance;
                    nearest = pickup;
                }
            }

            if (nearest == null)
            {
                return false;
            }

            WeaponArchetype oldWeapon = CurrentWeapon;
            int oldDurability = Durability;
            WeaponArchetype newWeapon = nearest.Archetype;
            int newDurability = nearest.RemainingDurability;

            nearest.Consume();
            Equip(newWeapon, newDurability);

            if (oldWeapon != WeaponArchetype.Unarmed && oldDurability > 0)
            {
                WeaponPickup.Spawn(dropPosition, oldWeapon, oldDurability);
            }

            return true;
        }

        public void ConsumeDurability(int amount)
        {
            if (amount <= 0 || CurrentWeapon == WeaponArchetype.Unarmed || Durability <= 0)
            {
                return;
            }

            Durability = Mathf.Max(0, Durability - amount);
            if (Durability == 0)
            {
                CurrentWeapon = WeaponArchetype.Unarmed;
            }

            NotifyChanged();
        }

        public void EquipFresh(WeaponArchetype archetype)
        {
            WeaponStats stats = WeaponCatalog.Get(archetype);
            Equip(archetype, stats.MaxDurability);
        }

        public void Equip(WeaponArchetype archetype, int durability)
        {
            if (archetype == WeaponArchetype.Unarmed)
            {
                CurrentWeapon = WeaponArchetype.Unarmed;
                Durability = 0;
                NotifyChanged();
                return;
            }

            WeaponStats stats = WeaponCatalog.Get(archetype);
            CurrentWeapon = archetype;
            Durability = Mathf.Clamp(durability, 1, stats.MaxDurability);
            NotifyChanged();
        }

        private void NotifyChanged()
        {
            LoadoutChanged?.Invoke(CurrentWeapon, Durability, MaxDurability);
        }
    }
}
