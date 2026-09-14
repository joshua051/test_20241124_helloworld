using UnityEngine;

namespace IronSand.Combat
{
    public static class WeaponSweep
    {
        public static void GetSegment(Transform actor, WeaponArchetype weapon, AttackKind attack, float activeProgress, out Vector3 grip, out Vector3 tip)
        {
            WeaponStats stats = WeaponCatalog.Get(weapon);
            float progress = Mathf.Clamp01(activeProgress);
            float arc = attack == AttackKind.Heavy ? 112f : 78f;
            float start = attack == AttackKind.Heavy ? -arc : arc;
            float angle = Mathf.Lerp(start, -start, progress);
            Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * actor.forward;
            float reach = Mathf.Lerp(1.15f, 1.85f, Mathf.InverseLerp(0.78f, 1.45f, stats.ReachMultiplier));
            grip = actor.position + Vector3.up * 0.35f + actor.right * 0.28f;
            tip = actor.position + Vector3.up * (attack == AttackKind.Heavy ? 0.28f : 0.42f) + direction.normalized * reach;
        }
    }
}
