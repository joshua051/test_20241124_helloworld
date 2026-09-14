using UnityEngine;

namespace IronSand.Combat
{
    public static class CombatQueries
    {
        // Graybox world occlusion, not an animation-driven weapon sweep.
        public static bool WorldBlocks(Vector3 from, Vector3 to)
        {
            Vector3 delta = to - from;
            if (delta.sqrMagnitude < 0.0001f) return false;
            foreach (RaycastHit hit in Physics.RaycastAll(from, delta.normalized, delta.magnitude, ~0, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.GetComponentInParent<Combatant>() == null) return true;
            }
            return false;
        }
    }
}
