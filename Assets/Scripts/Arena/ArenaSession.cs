using IronSand.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace IronSand.Arena
{
    // Runs before actor Update so Esc/R cannot also trigger a combat action.
    [DefaultExecutionOrder(-1000)]
    public sealed class ArenaSession : MonoBehaviour
    {
        private PlayerGladiator player;
        private ArenaDirector director;
        private bool restarting;
        private float previousTimeScale;
        public bool IsPaused { get; private set; }
        public string SetupError { get; private set; }
        public bool Ended => (player != null && player.IsDead) || (director != null && director.Victory);
        public bool GameplayActive => !IsPaused && !Ended && !restarting && string.IsNullOrEmpty(SetupError);

        private void Awake()
        {
            previousTimeScale = Time.timeScale;
            player = FindFirstObjectByType<PlayerGladiator>();
            director = GetComponent<ArenaDirector>();
        }

        private void Start()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            Resume();
#else
            SetupError = "Set Active Input Handling to Input Manager (Old) or Both, then restart Unity.";
            Pause();
            Debug.LogError(SetupError);
#endif
        }

        private void Update()
        {
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

        public void Pause()
        {
            IsPaused = true;
            Time.timeScale = 0f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        public void Resume()
        {
            if (Ended || restarting || !string.IsNullOrEmpty(SetupError)) return;
            IsPaused = false;
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void Restart()
        {
            if (restarting || !string.IsNullOrEmpty(SetupError)) return;
            string path = SceneManager.GetActiveScene().path;
            if (string.IsNullOrEmpty(path) || !Application.CanStreamedLevelBeLoaded(path))
            {
                SetupError = "Scene is not in the active build scene list. Exit Play Mode and rebuild the arena.";
                Pause();
                Debug.LogError(SetupError);
                return;
            }
            restarting = true;
            Time.timeScale = 1f;
            SceneManager.LoadScene(path, LoadSceneMode.Single);
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) Pause();
        }

        private void OnDestroy()
        {
            Time.timeScale = previousTimeScale;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}
