using UnityEngine;

namespace IronSand.Arena
{
    // One source of truth for the graybox footprint. Cylinder scale is DIAMETER.
    public static class ArenaGeometry
    {
        public const float FloorRadius = 15f;
        public const float WallRadius = 14.5f;
        public const float SpawnRadius = 10f;
        public const int WallCount = 24;

        public static GameObject Create()
        {
            GameObject root = new("ArenaGeometry");
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            floor.name = "ArenaFloor";
            floor.transform.SetParent(root.transform, false);
            floor.transform.localPosition = new Vector3(0f, -0.5f, 0f);
            floor.transform.localScale = new Vector3(FloorRadius * 2f, 0.5f, FloorRadius * 2f);
            // Unity's cylinder primitive has a CapsuleCollider, NOT a flat floor.
            Collider old = floor.GetComponent<Collider>();
            old.enabled = false;
            if (Application.isPlaying) Object.Destroy(old);
            else Object.DestroyImmediate(old);
            MeshCollider surface = floor.AddComponent<MeshCollider>();
            surface.sharedMesh = floor.GetComponent<MeshFilter>().sharedMesh;
            surface.convex = false; // Static scenery, no Rigidbody.

            float width = 2f * WallRadius * Mathf.Tan(Mathf.PI / WallCount) + 0.15f;
            for (int i = 0; i < WallCount; i++)
            {
                float angle = Mathf.PI * 2f * i / WallCount;
                GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wall.name = $"ArenaWall_{i:00}";
                wall.transform.SetParent(root.transform, false);
                wall.transform.localPosition = new Vector3(Mathf.Cos(angle) * WallRadius, 1.5f, Mathf.Sin(angle) * WallRadius);
                // Local X follows the tangent; local Z points radially outward.
                wall.transform.localRotation = Quaternion.Euler(0f, 90f - angle * Mathf.Rad2Deg, 0f);
                wall.transform.localScale = new Vector3(width, 3f, 0.8f);
            }

            for (int i = 0; i < 8; i++)
            {
                float angle = Mathf.PI * 2f * i / 8f;
                GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pillar.name = $"Pillar_{i:00}";
                pillar.transform.SetParent(root.transform, false);
                pillar.transform.localPosition = new Vector3(Mathf.Cos(angle) * 12.2f, 1.8f, Mathf.Sin(angle) * 12.2f);
                pillar.transform.localScale = new Vector3(0.55f, 1.8f, 0.55f);
            }
            return root;
        }
    }
}
