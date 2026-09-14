using System.Collections;
using IronSand.Arena;
using IronSand.Combat;
using IronSand.Player;
using IronSand.Scoring;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace IronSand.Tests
{
    public sealed class ArenaSmokeTests
    {
        private const string ScenePath = "Assets/Scenes/ArenaPrototype.unity";

        [UnitySetUp]
        public IEnumerator LoadGeneratedScene()
        {
            yield return ArenaTestSupport.Load();
        }

        [UnityTest]
        public IEnumerator SceneLoadsWithCombatRigAndGroundedActors()
        {
            yield return ArenaTestSupport.WaitFor(() =>
            {
                var actor = Object.FindFirstObjectByType<PlayerGladiator>();
                var arena = Object.FindFirstObjectByType<ArenaDirector>();
                if (actor == null || arena == null || !actor.GetComponent<CharacterController>().isGrounded) return false;
                foreach (var enemy in arena.AliveEnemies)
                    if (enemy == null || !enemy.GetComponent<CharacterController>().isGrounded) return false;
                return true;
            }, "Actors did not become grounded.");
            var player = Object.FindFirstObjectByType<PlayerGladiator>();
            var camera = Object.FindFirstObjectByType<ThirdPersonArenaCamera>();
            var director = Object.FindFirstObjectByType<ArenaDirector>();
            Assert.IsNotNull(player);
            Assert.AreSame(player, camera.TrackedPlayer);
            Assert.AreEqual(1, director.WaveNumber);
            Assert.AreEqual(3, director.AliveEnemies.Count);
            Assert.IsTrue(player.GetComponent<CharacterController>().isGrounded);
            Assert.IsNotNull(player.GetComponent<ProceduralCombatRig>());
            foreach (var enemy in director.AliveEnemies)
            {
                Assert.IsTrue(enemy.GetComponent<CharacterController>().isGrounded);
                Assert.IsNotNull(enemy.GetComponent<ProceduralCombatRig>());
            }
            Assert.That(director.ActiveAttackers, Is.LessThanOrEqualTo(2));
            Assert.AreEqual(1, Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length);
            Assert.IsNotNull(Object.FindFirstObjectByType<CombatFeedbackSystem>());
            Assert.IsNotNull(Object.FindFirstObjectByType<CrowdRewardDirector>());
        }

        [UnityTest]
        public IEnumerator EnemyPressureEventuallyCommitsAnAttackWithoutDeadZone()
        {
            var director = Object.FindFirstObjectByType<ArenaDirector>();
            float deadline = Time.realtimeSinceStartup + 8f;
            bool sawCommit = false;
            while (Time.realtimeSinceStartup < deadline)
            {
                foreach (var enemy in director.AliveEnemies)
                    sawCommit |= enemy != null && enemy.IsAttackCommitted;
                if (sawCommit) break;
                yield return null;
            }
            Assert.IsTrue(sawCommit, "No enemy committed an attack within 8 seconds; pressure/approach logic may be stalled.");
            Assert.That(director.ActiveAttackers, Is.InRange(1, 2));
        }

        [UnityTest]
        public IEnumerator CrowdThresholdQueuesRewardAndAppealLaunchesPhysicalGifts()
        {
            CrowdFavorSystem favor = Object.FindFirstObjectByType<CrowdFavorSystem>();
            CrowdRewardDirector rewards = Object.FindFirstObjectByType<CrowdRewardDirector>();
            Assert.AreEqual(0, rewards.PendingRewards);
            Assert.AreEqual(0, Object.FindObjectsByType<CrowdGift>(FindObjectsSortMode.None).Length);

            favor.AddFavor(25);
            yield return null;
            Assert.AreEqual(1, rewards.PendingRewards);
            Assert.AreEqual(0, Object.FindObjectsByType<CrowdGift>(FindObjectsSortMode.None).Length,
                "Threshold should queue a reward; it should not auto-launch before Appeal.");
            Assert.IsTrue(rewards.TryAppeal());
            yield return null;
            Assert.AreEqual(0, rewards.PendingRewards);
            Assert.AreEqual(1, Object.FindObjectsByType<CrowdGift>(FindObjectsSortMode.None).Length);

            favor.AddFavor(25);
            yield return null;
            Assert.AreEqual(1, rewards.PendingRewards);
            Assert.IsTrue(rewards.TryAppeal());
            yield return null;
            Assert.AreEqual(1, Object.FindObjectsByType<CrowdWeaponGift>(FindObjectsSortMode.None).Length);
        }

        [UnityTest]
        public IEnumerator DefeatCanRestartWithoutLeavingPlayMode()
        {
            var oldPlayer = Object.FindFirstObjectByType<PlayerGladiator>();
            oldPlayer.ApplyDamage(oldPlayer.MaxHealth + 1f, Vector3.zero);
            yield return null;
            var oldSession = Object.FindFirstObjectByType<ArenaSession>();
            Assert.IsTrue(oldSession.Ended);
            Assert.IsTrue(oldSession.IsPaused);
            oldSession.Restart();
            yield return ArenaTestSupport.WaitReady(oldSession);
            var player = Object.FindFirstObjectByType<PlayerGladiator>();
            var director = Object.FindFirstObjectByType<ArenaDirector>();
            var session = Object.FindFirstObjectByType<ArenaSession>();
            Assert.IsNotNull(player);
            Assert.AreNotSame(oldPlayer, player);
            Assert.IsFalse(player.IsDead);
            Assert.AreEqual(player.MaxHealth, player.Health);
            Assert.AreEqual(WeaponArchetype.Sword, player.WeaponController.CurrentWeapon);
            Assert.AreEqual(1, director.WaveNumber);
            Assert.AreEqual(3, director.AliveEnemies.Count);
            Assert.AreEqual(0, Object.FindFirstObjectByType<CombatStyleSystem>().Score);
            Assert.AreSame(player, Object.FindFirstObjectByType<ThirdPersonArenaCamera>().TrackedPlayer);
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsTrue(session.GameplayActive);
        }

        [UnityTearDown]
        public IEnumerator CleanupScene()
        {
            Scene loaded = SceneManager.GetSceneByPath(ScenePath);
            Scene empty = SceneManager.CreateScene("ArenaTestCleanup");
            SceneManager.SetActiveScene(empty);
            if (loaded.IsValid() && loaded.isLoaded) yield return SceneManager.UnloadSceneAsync(loaded);
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
