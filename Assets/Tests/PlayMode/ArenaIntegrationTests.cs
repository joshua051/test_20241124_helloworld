using System.Collections;
using System.Linq;
using IronSand.Arena;
using IronSand.Art;
using IronSand.Combat;
using IronSand.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace IronSand.Tests
{
    // Authored, NOT_RUN until executed by a real Unity Test Runner.
    public sealed class ArenaIntegrationTests
    {
        [UnitySetUp] public IEnumerator Load() { yield return ArenaTestSupport.Load(); }
        [UnityTest]
        public IEnumerator PauseResumeSuppressesSameFrameInputAndRetainsModel()
        {
            ArenaSession session = Object.FindFirstObjectByType<ArenaSession>();
            PlayerGladiator player = Object.FindFirstObjectByType<PlayerGladiator>();
            ProceduralCombatRig rig = player.GetComponent<ProceduralCombatRig>();
            Assert.IsTrue(rig.UsesImportedModel);
            var visual = player.GetComponentInChildren<ImportedGladiatorVisual>();
            Assert.AreEqual(16, visual.BoneCount);
            foreach (Material mat in visual.BodyRenderer.sharedMaterials)
                Assert.AreEqual(GladiatorMaterialLibrary.RequireTemplate().shader, mat.shader);
            session.Pause();
            float pausedAt = Time.time;
            yield return null; yield return null;
            Assert.AreEqual(pausedAt, Time.time);
            Assert.IsFalse(session.GameplayActive);
            session.Resume();
            Assert.IsFalse(session.GameplayActive, "Resume click must not also attack this frame.");
            yield return null; yield return null;
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsTrue(session.GameplayActive);
            Assert.AreSame(visual, player.GetComponentInChildren<ImportedGladiatorVisual>());
        }
        [UnityTest]
        public IEnumerator RepeatedRestartReloadsOwnedSceneNotUnrelatedActiveScene()
        {
            for (int attempt = 0; attempt < 2; attempt++)
            {
                ArenaSession old = Object.FindFirstObjectByType<ArenaSession>();
                Scene helper = SceneManager.CreateScene("UnrelatedActiveScene");
                SceneManager.SetActiveScene(helper);
                old.Pause(); old.Restart(); old.Restart(); // Repeated request is idempotent.
                yield return ArenaTestSupport.WaitReady(old);
                Assert.AreEqual(ArenaTestSupport.ScenePath, SceneManager.GetActiveScene().path);
                Assert.AreEqual(1, Object.FindObjectsByType<ArenaSession>(FindObjectsSortMode.None).Length);
                PlayerGladiator player = Object.FindFirstObjectByType<PlayerGladiator>();
                Assert.AreEqual(player.MaxHealth, player.Health);
                Assert.IsTrue(player.GetComponent<ProceduralCombatRig>().UsesImportedModel);
                Assert.AreEqual(1, player.GetComponentsInChildren<ImportedGladiatorVisual>().Length);
                Assert.AreSame(player, Object.FindFirstObjectByType<ThirdPersonArenaCamera>().TrackedPlayer);
                yield return ArenaTestSupport.WaitFor(() => player.GetComponent<CharacterController>().isGrounded,
                    "Player did not become grounded after restart.");
            }
        }
        [UnityTest]
        public IEnumerator ScriptedWaveRemovalReachesVictoryWithoutIndexOverflow()
        {
            // This validates encounter lifecycle, NOT player combat mechanics or feel.
            var director = Object.FindFirstObjectByType<ArenaDirector>();
            var player = Object.FindFirstObjectByType<PlayerGladiator>();
            player.GrantInvulnerability(60f);
            for (int wave = 1; wave <= director.TotalWaves; wave++)
            {
                int expected = wave;
                yield return ArenaTestSupport.WaitFor(() => director.WaveNumber == expected && director.AliveEnemies.Count > 0,
                    "Next wave failed to spawn.");
                foreach (var enemy in director.AliveEnemies.ToArray())
                    Assert.IsTrue(enemy.ApplyDamage(enemy.MaxHealth + 1f, Vector3.zero));
                Assert.That(director.ActiveAttackers, Is.LessThanOrEqualTo(2));
            }
            yield return ArenaTestSupport.WaitFor(() => director.Victory, "Final wave did not complete.");
            yield return null;
            Assert.AreEqual(director.TotalWaves, director.WaveNumber);
            Assert.AreEqual(0, director.AliveEnemies.Count);
            Assert.IsTrue(Object.FindFirstObjectByType<ArenaSession>().IsPaused);
        }
        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            Scene arena = SceneManager.GetSceneByPath(ArenaTestSupport.ScenePath);
            Scene empty = SceneManager.CreateScene("IntegrationCleanup");
            SceneManager.SetActiveScene(empty);
            if (arena.IsValid() && arena.isLoaded) yield return SceneManager.UnloadSceneAsync(arena);
            Time.timeScale = 1f;
        }
    }
}
