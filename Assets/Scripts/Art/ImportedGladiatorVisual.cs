using System;
using IronSand.Combat;
using UnityEngine;

namespace IronSand.Art
{
    /// <summary>Actual CC0 mesh/UV data with shared Blender/Unity procedural skinning.</summary>
    public sealed class ImportedGladiatorVisual : MonoBehaviour
    {
        [Serializable] public sealed class BoneData { public string name; public int parent; public float[] position; }
        [Serializable] public sealed class SurfaceData { public int material; public int[] triangles; }
        [Serializable] public sealed class MeshData { public float[] vertices, normals, uv, weights; public int[] boneIndices; public SurfaceData[] submeshes; }
        [Serializable] public sealed class MaterialData { public string name, texture; public float[] color; public float metallic, roughness; public bool teamTint; }
        [Serializable] public sealed class PoseData { public string name; public Vector3[] euler; }
        [Serializable] public sealed class SocketData { public float[] position, euler; }
        [Serializable] public sealed class ModelData
        {
            public int schemaVersion;
            public string name;
            public BoneData[] bones;
            public MaterialData[] materials;
            public MeshData body, sword, shield;
            public PoseData[] poses;
            public SocketData swordSocket, shieldSocket;
        }
        private static ModelData source;
        private Transform[] bones;
        private Material[] materials;
        private Mesh bodyMesh, shieldMesh;
        private SkinnedMeshRenderer body;
        private GameObject shield;
        public Transform WeaponSocket { get; private set; }
        public SkinnedMeshRenderer BodyRenderer => body;
        public int BoneCount => bones != null ? bones.Length : 0;

        public static bool TryAttach(Transform owner, bool playerTeam, out ImportedGladiatorVisual visual)
        {
            visual = null;
            if (owner == null) return false;
            GameObject root = null;
            try
            {
                if (!LoadSource()) return false;
                root = new GameObject("Imported_CC0_Gladiator"); root.transform.SetParent(owner, false);
                visual = root.AddComponent<ImportedGladiatorVisual>(); visual.Build(playerTeam);
                return true;
            }
            catch (Exception error)
            {
                if (root != null) { root.SetActive(false); Dispose(root); }
                Debug.LogError("Imported gladiator setup failed; retaining graybox fallback: " + error.Message, owner);
                visual = null;
                return false;
            }
        }
        private static bool LoadSource()
        {
            if (source != null) return true;
            TextAsset asset = Resources.Load<TextAsset>("Gladiators/Gladiator");
            if (asset == null) return false;
            ModelData value = JsonUtility.FromJson<ModelData>(asset.text);
            if (value == null || value.schemaVersion != 1 || value.bones == null || value.bones.Length != 16 || value.poses == null || value.poses.Length != 4)
                throw new InvalidOperationException("Invalid gladiator data schema.");
            source = value;
            return true;
        }
        private void Build(bool playerTeam)
        {
            materials = CreateMaterials(playerTeam);
            bones = new Transform[source.bones.Length];
            for (int i = 0; i < bones.Length; i++)
            {
                BoneData entry = source.bones[i];
                if (entry.parent >= i || entry.parent < -1) throw new InvalidOperationException("Invalid bone hierarchy.");
                bones[i] = new GameObject(entry.name).transform;
                bones[i].SetParent(entry.parent < 0 ? transform : bones[entry.parent], false);
                bones[i].localPosition = V(entry.position) - (entry.parent < 0 ? Vector3.zero : V(source.bones[entry.parent].position));
            }
            bodyMesh = MakeMesh(source.body, true);
            Matrix4x4[] bindposes = new Matrix4x4[bones.Length];
            for (int i = 0; i < bones.Length; i++) bindposes[i] = bones[i].worldToLocalMatrix * transform.localToWorldMatrix;
            bodyMesh.bindposes = bindposes;
            body = gameObject.AddComponent<SkinnedMeshRenderer>();
            body.sharedMesh = bodyMesh; body.bones = bones; body.rootBone = bones[0];
            body.sharedMaterials = SelectMaterials(source.body, materials);
            body.localBounds = new Bounds(Vector3.zero, new Vector3(3.5f, 3f, 3.5f));
            body.quality = SkinQuality.Bone4;
            WeaponSocket = new GameObject("WeaponSocket").transform;
            WeaponSocket.SetParent(bones[9], false);
            WeaponSocket.localPosition = V(source.swordSocket.position);
            WeaponSocket.localRotation = Quaternion.Euler(V(source.swordSocket.euler));
            shield = new GameObject("Imported_Shield"); shield.transform.SetParent(bones[6], false);
            shield.transform.localPosition = V(source.shieldSocket.position);
            shield.transform.localRotation = Quaternion.Euler(V(source.shieldSocket.euler));
            shieldMesh = MakeMesh(source.shield, false);
            shield.AddComponent<MeshFilter>().sharedMesh = shieldMesh;
            shield.AddComponent<MeshRenderer>().sharedMaterials = SelectMaterials(source.shield, materials);
            ConfigureTeam(playerTeam);
            ApplyPose(0f, 0f, false, CombatPhase.Idle, 0f, 0f, 0f, 0f, false);
        }
        public void ConfigureTeam(bool playerTeam)
        {
            if (materials == null) return;
            for (int i = 0; i < materials.Length; i++)
                if (source.materials[i].teamTint)
                    SetColor(materials[i], playerTeam ? new Color(.20f, .32f, .48f) : new Color(.46f, .12f, .07f));
            if (shield != null) shield.SetActive(playerTeam);
        }
        public void ApplyPose(float walkPhase, float motion, bool guarding, CombatPhase phase,
            float attackProgress, float hit, float vulnerable, float execution, bool executionActor)
        {
            if (bones == null) return;
            int from = guarding ? 1 : 0, to = from; float blend = 0f;
            if (phase != CombatPhase.Idle || (executionActor && execution > 0f))
            {
                float p = Mathf.Clamp01(executionActor && execution > 0f ? execution : attackProgress);
                if (p < .35f) { from = 0; to = 2; blend = p / .35f; }
                else if (p < .62f) { from = 2; to = 3; blend = (p - .35f) / .27f; }
                else { from = 3; to = 0; blend = (p - .62f) / .38f; }
            }
            for (int i = 0; i < bones.Length; i++)
                bones[i].localRotation = Quaternion.Slerp(Quaternion.Euler(source.poses[from].euler[i]), Quaternion.Euler(source.poses[to].euler[i]), blend);
            float swing = Mathf.Sin(walkPhase) * 23f * Mathf.Clamp01(motion);
            bones[10].localRotation *= Quaternion.Euler(swing, 0f, 0f);
            bones[13].localRotation *= Quaternion.Euler(-swing, 0f, 0f);
            bones[11].localRotation *= Quaternion.Euler(Mathf.Max(0f, -swing) * .65f, 0f, 0f);
            bones[14].localRotation *= Quaternion.Euler(Mathf.Max(0f, swing) * .65f, 0f, 0f);
            bones[2].localRotation *= Quaternion.Euler(vulnerable * 12f, 0f, -hit * 14f - vulnerable * 10f);
            bones[3].localRotation *= Quaternion.Euler(vulnerable * 12f, 0f, hit * 6f);
            if (!executionActor && execution > 0f) bones[2].localRotation *= Quaternion.Euler(execution * 35f, 0f, -execution * 20f);
        }
        public static GameObject CreateSword(Transform parent, Vector3 localPosition)
        {
            if (parent == null || !LoadSource()) return null;
            GameObject root = new GameObject("WeaponVisual_Sword"); root.transform.SetParent(parent, false); root.transform.localPosition = localPosition;
            Mesh mesh = MakeMesh(source.sword, false); Material[] owned = CreateMaterials(true);
            root.AddComponent<MeshFilter>().sharedMesh = mesh;
            root.AddComponent<MeshRenderer>().sharedMaterials = SelectMaterials(source.sword, owned);
            GladiatorMeshOwner lifetime = root.AddComponent<GladiatorMeshOwner>(); lifetime.Mesh = mesh; lifetime.Materials = owned;
            return root;
        }
        private static Mesh MakeMesh(MeshData data, bool skinned)
        {
            int count = data.vertices.Length / 3;
            if (count == 0 || count > 65000 || data.normals.Length != count * 3 || data.uv.Length != count * 2)
                throw new InvalidOperationException("Malformed mesh buffers.");
            Vector3[] vertices = new Vector3[count], normals = new Vector3[count]; Vector2[] uv = new Vector2[count];
            for (int i = 0; i < count; i++) { vertices[i] = V(data.vertices, i * 3); normals[i] = V(data.normals, i * 3); uv[i] = new Vector2(data.uv[i * 2], data.uv[i * 2 + 1]); }
            Mesh mesh = new Mesh { name = "CC0_GladiatorMesh", vertices = vertices, normals = normals, uv = uv, subMeshCount = data.submeshes.Length };
            for (int i = 0; i < data.submeshes.Length; i++) mesh.SetTriangles(data.submeshes[i].triangles, i);
            if (skinned)
            {
                if (data.weights.Length != count * 4 || data.boneIndices.Length != count * 4) throw new InvalidOperationException("Malformed skin buffers.");
                BoneWeight[] weights = new BoneWeight[count];
                for (int i = 0; i < count; i++)
                {
                    int j = i * 4;
                    weights[i] = new BoneWeight { boneIndex0 = data.boneIndices[j], boneIndex1 = data.boneIndices[j + 1], boneIndex2 = data.boneIndices[j + 2], boneIndex3 = data.boneIndices[j + 3], weight0 = data.weights[j], weight1 = data.weights[j + 1], weight2 = data.weights[j + 2], weight3 = data.weights[j + 3] };
                }
                mesh.boneWeights = weights;
            }
            mesh.RecalculateBounds(); return mesh;
        }
        private static Material[] CreateMaterials(bool playerTeam)
        {
            Shader shader = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null
                ? Shader.Find("Universal Render Pipeline/Lit") : Shader.Find("Standard");
            if (shader == null) throw new InvalidOperationException("The active render pipeline requires a supported lit shader.");
            Material[] result = new Material[source.materials.Length];
            for (int i = 0; i < result.Length; i++)
            {
                MaterialData spec = source.materials[i]; Material mat = new Material(shader) { name = "CC0_" + spec.name }; result[i] = mat;
                Color color = new Color(spec.color[0], spec.color[1], spec.color[2], spec.color[3]);
                if (spec.teamTint && !playerTeam) color = new Color(.46f, .12f, .07f);
                SetColor(mat, color);
                if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", spec.metallic);
                if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", 1f - spec.roughness);
                if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 1f - spec.roughness);
                if (!string.IsNullOrEmpty(spec.texture))
                {
                    Texture2D texture = Resources.Load<Texture2D>("Gladiators/" + spec.texture);
                    if (texture == null) throw new InvalidOperationException("Missing gladiator texture: " + spec.texture);
                    mat.mainTexture = texture; if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", texture);
                }
            }
            return result;
        }
        private static Material[] SelectMaterials(MeshData mesh, Material[] all)
        {
            Material[] result = new Material[mesh.submeshes.Length];
            for (int i = 0; i < result.Length; i++) result[i] = all[mesh.submeshes[i].material];
            return result;
        }
        private static void SetColor(Material mat, Color color) { mat.color = color; if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color); }
        private static Vector3 V(float[] data, int at = 0) => new Vector3(data[at], data[at + 1], data[at + 2]);
        internal static void Dispose(UnityEngine.Object item) { if (item == null) return; if (Application.isPlaying) Destroy(item); else DestroyImmediate(item); }
        private void OnDestroy() { Dispose(bodyMesh); Dispose(shieldMesh); if (materials != null) foreach (Material mat in materials) Dispose(mat); }
    }
}
