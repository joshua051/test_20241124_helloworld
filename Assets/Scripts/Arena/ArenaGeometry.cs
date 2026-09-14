using UnityEngine;

namespace IronSand.Arena
{
    public static class ArenaGeometry
    {
        public const float FloorRadius = 15f;
        public const float WallRadius = 14.5f;
        public const float SpawnRadius = 10f;
        public const int WallCount = 24;
        public static GameObject Create()
        {
            GameObject root = new("ArenaGeometry"); GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cylinder); floor.name = "ArenaFloor"; floor.transform.SetParent(root.transform, false); floor.transform.localPosition = new Vector3(0f, -0.5f, 0f); floor.transform.localScale = new Vector3(FloorRadius * 2f, 0.5f, FloorRadius * 2f);
            Collider old = floor.GetComponent<Collider>(); old.enabled = false; if (Application.isPlaying) Object.Destroy(old); else Object.DestroyImmediate(old); MeshCollider surface = floor.AddComponent<MeshCollider>(); surface.sharedMesh = floor.GetComponent<MeshFilter>().sharedMesh; surface.convex = false;
            float width = 2f * WallRadius * Mathf.Tan(Mathf.PI / WallCount) + 0.15f;
            for (int i = 0; i < WallCount; i++) { float angle = Mathf.PI * 2f * i / WallCount; GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube); wall.name = $"ArenaWall_{i:00}"; wall.transform.SetParent(root.transform, false); wall.transform.localPosition = new Vector3(Mathf.Cos(angle) * WallRadius, 1.5f, Mathf.Sin(angle) * WallRadius); wall.transform.localRotation = Quaternion.Euler(0f, 90f - angle * Mathf.Rad2Deg, 0f); wall.transform.localScale = new Vector3(width, 3f, 0.8f); }
            for (int i = 0; i < 8; i++) { float angle = Mathf.PI * 2f * i / 8f; GameObject pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder); pillar.name = $"Pillar_{i:00}"; pillar.transform.SetParent(root.transform, false); pillar.transform.localPosition = new Vector3(Mathf.Cos(angle) * 12.2f, 1.8f, Mathf.Sin(angle) * 12.2f); pillar.transform.localScale = new Vector3(0.55f, 1.8f, 0.55f); }
            CreateCrowdRing(root.transform); return root;
        }
        private static void CreateCrowdRing(Transform parent)
        {
            GameObject crowdRoot = new("CrowdRing"); crowdRoot.transform.SetParent(parent, false); const int count = 48; const float radius = 17.2f;
            for (int i = 0; i < count; i++)
            {
                float angle = Mathf.PI * 2f * i / count; GameObject spectator = GameObject.CreatePrimitive(PrimitiveType.Capsule); spectator.name = $"Spectator_{i:00}"; spectator.transform.SetParent(crowdRoot.transform, false); spectator.transform.localPosition = new Vector3(Mathf.Cos(angle) * radius, 2.1f + (i % 3) * 0.22f, Mathf.Sin(angle) * radius); spectator.transform.localScale = new Vector3(0.32f, 0.55f, 0.32f); spectator.transform.LookAt(new Vector3(0f, spectator.transform.position.y, 0f)); Collider collider = spectator.GetComponent<Collider>(); if (collider != null) { collider.enabled = false; if (Application.isPlaying) Object.Destroy(collider); else Object.DestroyImmediate(collider); }
            }
        }
    }
}
