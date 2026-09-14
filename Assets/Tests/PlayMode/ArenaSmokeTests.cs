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
            Assert.IsTrue(Application.CanStreamedLevelBeLoaded(ScenePath),
                "Run Tools > Iron Sand Arena > Rebuild Prototype Arena before PlayMode tests.");
            Time.timeScale = 1f;
            yield return SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Single);
            yield return null;
            var session = Object.FindFirstObjectByType<ArenaSession>();
            Assert.IsNotNull(session);
            Assert.IsTrue(string.IsNullOrEmpty(session.SetupError), session.SetupError);
            session.Resume();
        }

        [UnityTest]
        public IEnumerator SceneLoadsWithSupportedActorsAndReboundCamera()
        {
            for (int i = 0; i < 60; i++) yield return null;
            var player = Object.FindFirstObjectByType<PlayerGladiator>();
            var camera = Object.FindFirstObjectByType<ThirdPersonArenaCamera>();
            var director = Object.FindFirstObjectByType<ArenaDirector>();
            Assert.IsNotNull(player);
            Assert.AreSame(player, camera.TrackedPlayer);
            Assert.AreEqual(1, director.WaveNumber);
            Assert.AreEqual(3, director.AliveEnemies.Count);
            Assert.That(player.transform.position.y, Is.InRange(0.8f, 1.2f));
            foreach (var enemy in director.AliveEnemies)
                Assert.That(enemy.transform.position.y, Is.InRange(0.8f, 1.2f));
            Assert.That(director.ActiveAttackers, Is.LessThanOrEqualTo(2));
            Assert.AreEqual(1, Object.FindObjectsByType<AudioListener>(FindObjectsSortMode.None).Length);
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
            yield return null;
            yield return null;
            var player = Object.FindFirstObjectByType<PlayerGladiator>();
            var director = Object.FindFirstObjectByType<ArenaDirector>();
            Assert.IsNotNull(player);
            Assert.AreNotSame(oldPlayer, player);
            Assert.IsFalse(player.IsDead);
            Assert.AreEqual(player.MaxHealth, player.Health);
            Assert.AreEqual(WeaponArchetype.Sword, player.WeaponController.CurrentWeapon);
            Assert.AreEqual(1, director.WaveNumber);
            Assert.AreEqual(3, director.AliveEnemies.Count);
            Assert.AreEqual(0, Object.FindFirstObjectByType<CombatStyleSystem>().Score);
            Assert.AreSame(player, Object.FindFirstObjectByType<ThirdPersonArenaCamera>().TrackedPlayer);
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
