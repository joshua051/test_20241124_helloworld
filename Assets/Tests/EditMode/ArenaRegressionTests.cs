using System;
using IronSand.Arena;
using IronSand.Combat;
using IronSand.Player;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace IronSand.Tests
{
    public sealed class ArenaRegressionTests
    {
        [TestCase(30)]
        [TestCase(60)]
        [TestCase(144)]
        public void DodgeTravelTimeDoesNotDependOnFrameRate(int fps)
        {
            var state = new DodgeState();
            Assert.IsTrue(state.TryStart(0.3f, 0.8f));
            float travelTime = 0f;
            for (int i = 0; i < fps; i++) travelTime += state.Tick(1f / fps);
            Assert.That(travelTime, Is.EqualTo(0.3f).Within(0.00001f));
            Assert.IsFalse(state.IsActive);
            Assert.IsTrue(state.CanStart);
        }

        [Test]
        public void DodgeCannotBeRestartedDuringActiveOrRecovery()
        {
            var state = new DodgeState();
            Assert.IsTrue(state.TryStart(0.3f, 0.8f));
            Assert.IsFalse(state.TryStart(0.3f, 0.8f));
            state.Tick(0.31f);
            Assert.IsFalse(state.IsActive);
            Assert.IsFalse(state.TryStart(0.3f, 0.8f));
            state.Tick(0.5f);
            Assert.IsTrue(state.TryStart(0.3f, 0.8f));
        }

        [Test]
        public void DodgeCancelDoesNotEraseCooldown()
        {
            var state = new DodgeState();
            state.TryStart(0.3f, 0.8f);
            state.CancelActive();
            Assert.IsFalse(state.IsActive);
            Assert.IsFalse(state.CanStart);
            Assert.AreEqual(0f, state.Tick(0f));
            Assert.That(state.CooldownRemaining, Is.EqualTo(0.8f));
        }

        [Test]
        public void DodgeRejectsInvalidDurationsAndClockValues()
        {
            var state = new DodgeState();
            Assert.Throws<ArgumentOutOfRangeException>(() => state.TryStart(0f, 1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => state.TryStart(0.3f, 0.3f));
            Assert.Throws<ArgumentOutOfRangeException>(() => state.Tick(float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => state.Tick(-1f));
        }

        [Test]
        public void FloorSupportsSpawnRingAndMovementAreaAtHeightZero()
        {
            GameObject geometry = ArenaGeometry.Create();
            try
            {
                Physics.SyncTransforms();
                var floor = geometry.transform.Find("ArenaFloor").GetComponent<MeshCollider>();
                Assert.IsNotNull(floor);
                Assert.IsNull(floor.GetComponent<CapsuleCollider>());
                foreach (float radius in new[] { 0f, 3f, ArenaGeometry.SpawnRadius, 13f })
                {
                    for (int i = 0; i < 72; i++)
                    {
                        float angle = i * Mathf.PI * 2f / 72f;
                        Vector3 origin = new(Mathf.Cos(angle) * radius, 5f, Mathf.Sin(angle) * radius);
                        Assert.IsTrue(floor.Raycast(new Ray(origin, Vector3.down), out RaycastHit hit, 8f), $"No floor at {origin}");
                        Assert.That(hit.point.y, Is.EqualTo(0f).Within(0.01f));
                    }
                }
            }
            finally { Object.DestroyImmediate(geometry); }
        }

        [Test]
        public void WallsAreTangentialAndCoverEveryExitDirection()
        {
            GameObject geometry = ArenaGeometry.Create();
            try
            {
                Physics.SyncTransforms();
                var walls = new Collider[ArenaGeometry.WallCount];
                for (int i = 0; i < walls.Length; i++)
                {
                    Transform wall = geometry.transform.Find($"ArenaWall_{i:00}");
                    Vector3 radial = wall.position; radial.y = 0f;
                    Assert.That(Mathf.Abs(Vector3.Dot(wall.right, radial.normalized)), Is.LessThan(0.001f));
                    walls[i] = wall.GetComponent<Collider>();
                }
                for (int i = 0; i < 360; i++)
                {
                    float angle = i * Mathf.Deg2Rad;
                    Ray ray = new(new Vector3(0f, 1.5f, 0f), new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)));
                    bool blocked = false;
                    foreach (Collider wall in walls) blocked |= wall.Raycast(ray, out _, 16f);
                    Assert.IsTrue(blocked, $"Wall gap at {i} degrees");
                }
            }
            finally { Object.DestroyImmediate(geometry); }
        }

        [Test]
        public void CameraRebindsSerializedTargetOnAwake()
        {
            GameObject actor = new("TestInactiveActor");
            actor.SetActive(false); // Do not run player/weapon gameplay lifecycle in EditMode.
            GameObject sourceObject = new("SourceCamera");
            GameObject copyObject = new("ReloadedCamera");
            try
            {
                var player = actor.AddComponent<PlayerGladiator>();
                var source = sourceObject.AddComponent<ThirdPersonArenaCamera>();
                source.SetTarget(actor.transform);
                string serialized = EditorJsonUtility.ToJson(source);
                var copy = copyObject.AddComponent<ThirdPersonArenaCamera>();
                EditorJsonUtility.FromJsonOverwrite(serialized, copy);
                copyObject.SendMessage("Awake", SendMessageOptions.RequireReceiver);
                Assert.AreSame(player, copy.TrackedPlayer);
            }
            finally
            {
                Object.DestroyImmediate(copyObject);
                Object.DestroyImmediate(sourceObject);
                Object.DestroyImmediate(actor);
            }
        }

        [Test]
        public void CameraAndCombatQueriesRespectSolidWorldBlockers()
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject cameraObject = new("TestCamera");
            try
            {
                // Away from an open editor scene's origin; no assets are modified.
                Vector3 origin = new(1000f, 1f, 0f);
                wall.transform.position = origin + Vector3.forward * 3f;
                wall.transform.localScale = new Vector3(4f, 4f, 1f);
                Physics.SyncTransforms();
                var camera = cameraObject.AddComponent<ThirdPersonArenaCamera>();
                Vector3 constrained = camera.ConstrainToWorld(origin, origin + Vector3.forward * 6f);
                Assert.That(constrained.z, Is.InRange(2f, 2.5f));
                Assert.IsTrue(CombatQueries.WorldBlocks(origin, origin + Vector3.forward * 6f));
                Assert.IsFalse(CombatQueries.WorldBlocks(origin, origin + Vector3.back * 2f));
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(wall);
            }
        }
    }
}
