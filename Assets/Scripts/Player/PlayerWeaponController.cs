using System;
using IronSand.Combat;
using UnityEngine;

namespace IronSand.Player
{
    public sealed class PlayerWeaponController : MonoBehaviour
    {
        [SerializeField] private WeaponArchetype startingWeapon = WeaponArchetype.Sword;
        [SerializeField] private Vector3 visualLocalPosition = new(0f, 0f, 0.48f);
        private GameObject weaponVisual;
        private Transform visualParent;
        public event Action<WeaponArchetype, int, int> LoadoutChanged;
        public WeaponArchetype CurrentWeapon { get; private set; }
        public int Durability { get; private set; }
        public WeaponStats CurrentStats => WeaponCatalog.Get(CurrentWeapon);
        public int MaxDurability => CurrentStats.MaxDurability;
        private void Awake() { visualParent = transform; EquipFresh(startingWeapon); }
        public void SetVisualParent(Transform parent) { visualParent = parent != null ? parent : transform; RefreshVisual(); }
        public bool TryPickupNearest(Vector3 origin, float radius, Vector3 dropPosition)
        {
            if (radius <= 0f) return false;
            Collider[] hits = Physics.OverlapSphere(origin, radius, ~0, QueryTriggerInteraction.Collide); WeaponPickup nearest = null; float bestSqrDistance = radius * radius;
            foreach (Collider hit in hits)
            {
                WeaponPickup pickup = hit.GetComponentInParent<WeaponPickup>(); if (pickup == null || pickup.IsConsumed) continue;
                float sqrDistance = (pickup.transform.position - origin).sqrMagnitude; if (sqrDistance > bestSqrDistance) continue;
                if (CombatQueries.WorldBlocks(origin, pickup.transform.position)) continue; bestSqrDistance = sqrDistance; nearest = pickup;
            }
            if (nearest == null || !nearest.TryClaim(out WeaponArchetype newWeapon, out int newDurability)) return false;
            WeaponArchetype oldWeapon = CurrentWeapon; int oldDurability = Durability; Equip(newWeapon, newDurability);
            if (oldWeapon != WeaponArchetype.Unarmed && oldDurability > 0 && !CombatQueries.WorldBlocks(origin, dropPosition)) WeaponPickup.Spawn(dropPosition, oldWeapon, oldDurability);
            return true;
        }
        public bool TryThrow(Vector3 origin, Vector3 direction, Combatant owner)
        {
            if (CurrentWeapon == WeaponArchetype.Unarmed || Durability <= 0) return false;
            WeaponArchetype weapon = CurrentWeapon; int durability = Durability;
            if (!ThrownWeapon.Spawn(origin, direction, weapon, durability, owner)) return false;
            Equip(WeaponArchetype.Unarmed, 0); return true;
        }
        public void ConsumeDurability(int amount)
        {
            if (amount <= 0 || CurrentWeapon == WeaponArchetype.Unarmed || Durability <= 0) return;
            Durability = Mathf.Max(0, Durability - amount); bool broke = Durability == 0; if (broke) CurrentWeapon = WeaponArchetype.Unarmed;
            if (broke) RefreshVisual(); LoadoutChanged?.Invoke(CurrentWeapon, Durability, MaxDurability);
        }
        public void EquipFresh(WeaponArchetype archetype) { WeaponStats stats = WeaponCatalog.Get(archetype); Equip(archetype, stats.MaxDurability); }
        public void Equip(WeaponArchetype archetype, int durability)
        {
            if (archetype == WeaponArchetype.Unarmed || durability <= 0)
            { CurrentWeapon = WeaponArchetype.Unarmed; Durability = 0; RefreshVisual(); LoadoutChanged?.Invoke(CurrentWeapon, Durability, MaxDurability); return; }
            WeaponStats stats = WeaponCatalog.Get(archetype); CurrentWeapon = archetype; Durability = Mathf.Clamp(durability, 1, stats.MaxDurability); RefreshVisual(); LoadoutChanged?.Invoke(CurrentWeapon, Durability, MaxDurability);
        }
        private void RefreshVisual() { if (weaponVisual != null) Destroy(weaponVisual); weaponVisual = WeaponVisualFactory.CreatePlaceholder(visualParent != null ? visualParent : transform, CurrentWeapon, visualLocalPosition); }
    }
}
