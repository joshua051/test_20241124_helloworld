using IronSand.Arena;
using IronSand.Combat;
using IronSand.Player;
using IronSand.Scoring;
using UnityEngine;

namespace IronSand.UI
{
    public sealed class PrototypeHUD : MonoBehaviour
    {
        private PlayerGladiator player;
        private PlayerWeaponController weapon;
        private CrowdFavorSystem favor;
        private ArenaDirector director;
        private CombatStyleSystem style;
        private ArenaSession session;
        private Camera gameplayCamera;

        private void Start()
        {
            player = FindFirstObjectByType<PlayerGladiator>();
            weapon = player != null ? player.WeaponController : null;
            favor = FindFirstObjectByType<CrowdFavorSystem>();
            director = FindFirstObjectByType<ArenaDirector>();
            style = FindFirstObjectByType<CombatStyleSystem>();
            session = FindFirstObjectByType<ArenaSession>();
            gameplayCamera = Camera.main;
        }

        private void OnGUI()
        {
            GUI.Box(new Rect(16f, 16f, 440f, 228f), "IRON SAND ARENA - GRAYBOX / UNVALIDATED");
            string hp = player == null ? "--" : $"{Mathf.CeilToInt(player.Health)} / {Mathf.CeilToInt(player.MaxHealth)}";
            string crowd = favor == null ? "--" : $"{favor.Favor} / {favor.MaxFavor}";
            string wave = director == null ? "--" : $"{director.WaveNumber} / {director.TotalWaves}";
            string enemies = director == null ? "--" : director.AliveEnemies.Count.ToString();
            string weaponName = weapon == null ? "--" : weapon.CurrentStats.DisplayName;
            string durability = weapon == null || weapon.CurrentWeapon == WeaponArchetype.Unarmed ? "--" : $"{weapon.Durability}/{weapon.MaxDurability}";
            string score = style == null ? "--" : $"{style.Score}  x{style.ComboCount}  Rank {style.Rank}";
            string dodge = player == null ? "--" : player.IsDodging ? "DODGING" : player.DodgeCooldownRemaining > 0f ? $"{player.DodgeCooldownRemaining:0.0}s" : "Ready";
            GUI.Label(new Rect(32f, 46f, 410f, 20f), $"Health: {hp}   Dodge: {dodge}");
            GUI.Label(new Rect(32f, 68f, 410f, 20f), $"Weapon: {weaponName}   Durability: {durability}");
            GUI.Label(new Rect(32f, 90f, 410f, 20f), $"Style: {score}");
            GUI.Label(new Rect(32f, 112f, 410f, 20f), $"Crowd Favor: {crowd} (threshold heal, no crowd art)");
            GUI.Label(new Rect(32f, 134f, 410f, 20f), $"Wave: {wave}   Enemies: {enemies}");
            GUI.Label(new Rect(32f, 158f, 410f, 64f), "LMB Light | RMB Heavy | Q Guard | Space Dodge\nTab Lock | E Swap | WASD Move | Shift Sprint\nEsc Pause / Resume | R Restart when paused / ended");
            DrawEnemyMarkers();
            if (director != null && director.IntermissionRemaining > 0f)
                GUI.Box(new Rect(Screen.width * 0.5f - 110f, 20f, 220f, 28f), $"Round transition: {director.IntermissionRemaining:0.0}s");
            if (session != null && (session.IsPaused || session.Ended)) DrawSessionPanel();
        }

        private void DrawSessionPanel()
        {
            string title = !string.IsNullOrEmpty(session.SetupError) ? "SETUP REQUIRED" :
                director != null && director.Victory ? "VICTORY" : player != null && player.IsDead ? "DEFEAT" : "PAUSED";
            Rect area = new(Screen.width * 0.5f - 200f, Screen.height * 0.5f - 110f, 400f, 220f);
            GUI.Box(area, title);
            if (!string.IsNullOrEmpty(session.SetupError))
            {
                GUI.Label(new Rect(area.x + 18f, area.y + 36f, 364f, 160f), session.SetupError,
                    new GUIStyle(GUI.skin.label) { wordWrap = true });
                return;
            }
            if (!session.Ended && GUI.Button(new Rect(area.x + 80f, area.y + 50f, 240f, 36f), "Resume (Esc)"))
                session.Resume();
            if (GUI.Button(new Rect(area.x + 80f, area.y + 100f, 240f, 36f), "Restart arena (R)"))
                session.Restart();
            GUI.Label(new Rect(area.x + 20f, area.y + 162f, 360f, 40f), "Prototype: no production character animation yet.");
        }

        private void DrawEnemyMarkers()
        {
            if (director == null || gameplayCamera == null) return;
            foreach (var enemy in director.AliveEnemies)
            {
                if (enemy == null || enemy.IsDead) continue;
                bool locked = player != null && player.LockTarget == enemy;
                if (!locked && !enemy.IsAttackCommitted) continue;
                Vector3 screen = gameplayCamera.WorldToScreenPoint(enemy.transform.position + Vector3.up * 1.4f);
                if (screen.z <= 0f) continue;
                float y = Screen.height - screen.y;
                string label = enemy.IsAttackCommitted ? "ATTACK INCOMING" : $"LOCK  HP {enemy.Health:0}";
                GUI.Box(new Rect(screen.x - 70f, y - 14f, 140f, 28f), label);
                if (enemy.IsAttackCommitted)
                    GUI.Box(new Rect(screen.x - 70f, y + 14f, Mathf.Max(1f, 140f * enemy.WindupProgress), 8f), "");
            }
        }
    }
}
