using IronSand.Art;
using IronSand.Combat;
using NUnit.Framework;
using UnityEngine;

namespace IronSand.Tests
{
    // Authored cases: Unity execution evidence is still required.
    public sealed class ImportedGladiatorTests
    {
        private GameObject root;
        private ImportedGladiatorVisual visual;
        [SetUp]
        public void Setup()
        {
            Assert.IsNotNull(Resources.Load<TextAsset>("Gladiators/Gladiator"), "Licensed model data must be committed, not only present in a CI artifact.");
            root = new GameObject("ImportedGladiatorTest");
            Assert.IsTrue(ImportedGladiatorVisual.TryAttach(root.transform, true, out visual));
        }
        [TearDown]
        public void Cleanup() { if (root != null) Object.DestroyImmediate(root); }
        [Test]
        public void UsesActualMeshAndCompleteSixteenBoneSkin()
        {
            Assert.AreEqual(16, visual.BoneCount);
            Assert.AreEqual(2270, visual.BodyRenderer.sharedMesh.vertexCount);
            Assert.AreEqual(16, visual.BodyRenderer.sharedMesh.bindposes.Length);
            Assert.AreEqual(2270, visual.BodyRenderer.sharedMesh.boneWeights.Length);
            Assert.IsTrue(visual.WeaponSocket.IsChildOf(root.transform));
            Assert.AreEqual(0, root.GetComponentsInChildren<Collider>(true).Length);
        }
        [Test]
        public void TeamChangePreservesSkinTextureAndTint()
        {
            Material skin = visual.BodyRenderer.sharedMaterials[0];
            Color before = skin.color; Texture texture = skin.mainTexture;
            Assert.IsNotNull(texture);
            visual.ConfigureTeam(false);
            Assert.AreEqual(before, skin.color);
            Assert.AreSame(texture, skin.mainTexture);
        }
        [Test]
        public void GuardChangesSkinWithoutMovingActorRoot()
        {
            Mesh idle = new Mesh(), guard = new Mesh();
            try
            {
                visual.BodyRenderer.BakeMesh(idle);
                Vector3 socketBefore = visual.WeaponSocket.position;
                visual.ApplyPose(0f, 0f, true, CombatPhase.Idle, 0f, 0f, 0f, 0f, false);
                visual.BodyRenderer.BakeMesh(guard);
                Assert.AreEqual(Vector3.zero, root.transform.position);
                Assert.Greater((visual.WeaponSocket.position - socketBefore).sqrMagnitude, .0001f);
                float moved = 0f; Vector3[] a = idle.vertices, b = guard.vertices;
                for (int i = 0; i < a.Length; i++) moved += (a[i] - b[i]).sqrMagnitude;
                Assert.Greater(moved, .01f);
            }
            finally { Object.DestroyImmediate(idle); Object.DestroyImmediate(guard); }
        }
        [Test]
        public void SwordUsesAuthoredGeometryWithoutGameplayColliders()
        {
            GameObject sword = ImportedGladiatorVisual.CreateSword(visual.WeaponSocket, new Vector3(0f, 0f, .48f));
            Assert.IsNotNull(sword);
            Assert.AreEqual(258, sword.GetComponent<MeshFilter>().sharedMesh.vertexCount);
            Assert.AreEqual(0, sword.GetComponentsInChildren<Collider>(true).Length);
        }
    }
}
