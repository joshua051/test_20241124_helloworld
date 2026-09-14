using IronSand.Combat;
using UnityEngine;

namespace IronSand.Tests
{
    public sealed class ArenaHealthProbe : Combatant
    {
        public int DamageCallbacks { get; private set; }
        public int DeathCallbacks { get; private set; }
        protected override void OnDamaged(Vector3 knockback) { DamageCallbacks++; }
        protected override void OnDeath() { DeathCallbacks++; }
    }
}
