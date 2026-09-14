using System;
using System.Collections;
using IronSand.Arena;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace IronSand.Tests
{
    internal static class ArenaTestSupport
    {
        public const string ScenePath = "Assets/Scenes/ArenaPrototype.unity";
        public static IEnumerator WaitFor(Func<bool> condition, string failure, float seconds = 12f)
        {
            float deadline = Time.realtimeSinceStartup + seconds;
            while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsTrue(condition(), failure); // Wall-clock timeout also works while paused.
        }
        public static IEnumerator WaitReady(ArenaSession previous = null)
        {
            yield return WaitFor(() =>
            {
                ArenaSession value = Object.FindFirstObjectByType<ArenaSession>();
                return value != null && !ReferenceEquals(value, previous) &&
                    (value.IsReady || !string.IsNullOrEmpty(value.SetupError));
            }, "Arena never reached readiness or reported a setup error.");
            ArenaSession session = Object.FindFirstObjectByType<ArenaSession>();
            Assert.IsTrue(string.IsNullOrEmpty(session.SetupError), session.SetupError);
            Assert.IsTrue(session.IsReady);
            session.Resume();
            yield return null;
            yield return null;
            Assert.IsTrue(session.GameplayActive, "Ready arena did not restore gameplay.");
        }
        public static IEnumerator Load()
        {
            Assert.IsTrue(Application.CanStreamedLevelBeLoaded(ScenePath), "Rebuild the prototype before tests.");
            yield return SceneManager.LoadSceneAsync(ScenePath, LoadSceneMode.Single);
            yield return WaitReady();
        }
    }
}
