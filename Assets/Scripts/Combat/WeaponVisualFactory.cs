using UnityEngine;

namespace IronSand.Combat
{
    public static class WeaponVisualFactory
    {
        public static GameObject CreatePlaceholder(Transform parent, WeaponArchetype archetype, Vector3 localPosition)
        {
            if (parent == null || WeaponCatalog.Get(archetype).MaxDurability <= 0)
            {
                return null;
            }

            PrimitiveType primitive = archetype == WeaponArchetype.Mace
                ? PrimitiveType.Sphere
                : archetype == WeaponArchetype.Spear
                    ? PrimitiveType.Cylinder
                    : PrimitiveType.Cube;

            GameObject visual = GameObject.CreatePrimitive(primitive);
            visual.name = $"WeaponVisual_{archetype}";
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = localPosition;
            ApplyShape(visual.transform, archetype);

            Collider collider = visual.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
                if (Application.isPlaying) Object.Destroy(collider);
                else Object.DestroyImmediate(collider);
            }

            return visual;
        }

        private static void ApplyShape(Transform visual, WeaponArchetype archetype)
        {
            switch (archetype)
            {
                case WeaponArchetype.Axe:
                    visual.localScale = new Vector3(0.12f, 0.55f, 0.75f);
                    visual.localRotation = Quaternion.Euler(15f, 0f, -25f);
                    break;
                case WeaponArchetype.Spear:
                    visual.localScale = new Vector3(0.07f, 0.9f, 0.07f);
                    visual.localRotation = Quaternion.Euler(0f, 0f, 80f);
                    break;
                case WeaponArchetype.Mace:
                    visual.localScale = new Vector3(0.34f, 0.34f, 0.34f);
                    visual.localRotation = Quaternion.identity;
                    break;
                default:
                    visual.localScale = new Vector3(0.12f, 0.12f, 0.95f);
                    visual.localRotation = Quaternion.Euler(8f, 0f, -18f);
                    break;
            }
        }
    }
}
