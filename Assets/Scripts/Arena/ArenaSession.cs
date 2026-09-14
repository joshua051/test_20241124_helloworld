using System;
using System.Collections;
using IronSand.Combat;
using IronSand.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IronSand.Arena
{
    // Owns the global clock only while this is the live single-arena session.
    [DefaultExecutionOrder(-1000)]
    public sealed class ArenaSession : MonoBehaviour
    {
        private static ArenaSession clockOwner;
        private readonly SessionFlow flow = new();
        private PlayerGladiator player;
        private ArenaDirector director;
        private float previousTimeScale;
        private int resumedFrame = -1;
        public bool IsPaused => flow.Paused;
        public bool IsReady => flow.Ready;
        public string SetupError => flow.Error;
        public bool Ended => (player != null && player.IsDead) || (director != null && director.Victory);
        public bool GameplayActive => ReferenceEquals(clockOwner, this) && flow.CanPlay &&
            !Ended && Time.frameCount > resumedFrame;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { clockOwner = null; }

        private void Awake()
        {
            if (clockOwner != null && !clockOwner.flow.Restarting)
            {
                flow.Fail("Multiple live arena sessions are not supported. Unload the other arena first.");
                Debug.LogError(SetupError, this);
                return;
            }
            // During scene replacement, the old OnDestroy must not reset the new clock.
            previousTimeScale = clockOwner != null ? clockOwner.previousTimeScale : Time.timeScale;
            clockOwner = this;
            director = GetComponent<ArenaDirector>();
            Pause();
        }

        private IEnumerator Start()
        {
            // Actor Start and director initialization must finish before input is enabled.
            yield return null;
            if (!ReferenceEquals(clockOwner, this) || SetupError != null) yield break;
#if !ENABLE_LEGACY_INPUT_MANAGER
            FailSetup("Set Active Input Handling to Input Manager (Old) or Both, then restart Unity.");
            yield break;
#else
            foreach (GameObject root in gameObject.scene.GetRootGameObjects())
            {
                foreach (PlayerGladiator candidate in root.GetComponentsInChildren<PlayerGladiator>())
                {
                    if (player != null) { FailSetup("Arena must contain exactly one active player."); yield break; }
                    player = candidate;
                }
            }
            if (player == null || director == null || !director.isActiveAndEnabled || director.WaveNumber < 1)
            { FailSetup("Arena setup incomplete. Rebuild the prototype scene."); yield break; }
            ProceduralCombatRig rig = player.GetComponent<ProceduralCombatRig>();
            if (rig == null || !rig.UsesImportedModel)
            { FailSetup("Player imported gladiator is unavailable. Rebuild the arena and check its Resources assets."); yield break; }
            foreach (var enemy in director.AliveEnemies)
            {
                ProceduralCombatRig enemyRig = enemy != null ? enemy.GetComponent<ProceduralCombatRig>() : null;
                if (enemyRig == null || !enemyRig.UsesImportedModel)
                { FailSetup("Enemy imported gladiator is unavailable; primitive fallback is not accepted."); yield break; }
            }
            Camera camera = Camera.main;
            if (camera == null || camera.gameObject.scene != gameObject.scene ||
                camera.GetComponent<ThirdPersonArenaCamera>() == null)
            { FailSetup("Arena camera is missing or belongs to another scene."); yield break; }
            flow.MarkReady();
            if (Application.isFocused || Application.isBatchMode) Resume();
#endif
        }

        private void Update()
        {
            if (!ReferenceEquals(clockOwner, this)) return;
            if (Ended && !IsPaused) Pause();
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.R) && (Ended || IsPaused)) Restart();
            else if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (IsPaused && !Ended) Resume();
                else Pause();
            }
#endif
        }
        public void FailSetup(string message)
        {
            flow.Fail(message);
            Pause();
            Debug.LogError(SetupError, this);
        }
        public void Pause()
        {
            flow.Pause();
            if (!ReferenceEquals(clockOwner, this)) return;
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        public void Resume()
        {
            if (!ReferenceEquals(clockOwner, this) || !flow.Resume(Ended)) return;
            // A Resume button click must not also become an attack in this frame.
            resumedFrame = Time.frameCount;
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        public void Restart()
        {
            if (!ReferenceEquals(clockOwner, this) || !flow.BeginRestart(Ended)) return;
            string path = gameObject.scene.path; // Never reload an unrelated active additive scene.
            if (string.IsNullOrEmpty(path) || !Application.CanStreamedLevelBeLoaded(path))
            { FailSetup("Arena scene is not in the build scene list. Exit Play Mode and rebuild the arena."); return; }
            try
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(path, LoadSceneMode.Single);
            }
            catch (Exception error) { FailSetup("Arena restart failed: " + error.Message); }
        }
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && !Application.isBatchMode) Pause();
        }
        private void OnDestroy()
        {
            if (!ReferenceEquals(clockOwner, this)) return;
            clockOwner = null;
            Time.timeScale = previousTimeScale;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
