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

        private void Start()
        {
            player = FindFirstObjectByType<PlayerGladiator>();
            weapon = player != null ? player.WeaponController : null;
            favor = FindFirstObjectByType<CrowdFavorSystem>();
            director = FindFirstObjectByType<ArenaDirector>();
            style = FindFirstObjectByType<CombatStyleSystem>();
        }

        private void OnGUI()
        {
            GUI.Box(new Rect(16f, 16f, 430f, 202f), "IRON SAND ARENA — PROTOTYPE");

            string hp = player == null ? "--" : $"{Mathf.CeilToInt(player.Health)} / {Mathf.CeilToInt(player.MaxHealth)}";
            string crowd = favor == null ? "--" : $"{favor.Favor} / {favor.MaxFavor}";
            string wave = director == null ? "--" : $"{director.WaveNumber} / {director.TotalWaves}";
            string enemies = director == null ? "--" : director.AliveEnemies.Count.ToString();
            string weaponName = weapon == null ? "--" : weapon.CurrentStats.DisplayName;
            string durability = weapon == null ? "--" : weapon.CurrentWeapon == WeaponArchetype.Unarmed ? "--" : $"{weapon.Durability}/{weapon.MaxDurability}";
            string score = style == null ? "--" : style.Score.ToString();
            string combo = style == null ? "--" : $"x{style.ComboCount}  Rank {style.Rank}";
            string target = player == null || player.LockTarget == null ? "None" : player.LockTarget.name;

            GUI.Label(new Rect(32f, 46f, 390f, 20f), $"Health: {hp}");
            GUI.Label(new Rect(32f, 68f, 390f, 20f), $"Weapon: {weaponName}   Durability: {durability}");
            GUI.Label(new Rect(32f, 90f, 390f, 20f), $"Style: {score}   Combo: {combo}");
            GUI.Label(new Rect(32f, 112f, 390f, 20f), $"Crowd Favor: {crowd}");
            GUI.Label(new Rect(32f, 134f, 390f, 20f), $"Wave: {wave}   Enemies: {enemies}   Lock: {target}");
            GUI.Label(new Rect(32f, 158f, 390f, 44f), "LMB Light | RMB Heavy | Q Guard | Space Dodge\nTab Lock | E Swap weapon | Shift Sprint");

            DrawLockMarker();

            if (director != null && director.Victory)
            {
                GUI.Box(new Rect(Screen.width * 0.5f - 150f, 50f, 300f, 56f), "VICTORY\nPrototype loop complete");
            }
            else if (player != null && player.IsDead)
            {
                GUI.Box(new Rect(Screen.width * 0.5f - 150f, 50f, 300f, 56f), "DEFEAT\nExit Play Mode to restart");
            }
        }

        private void DrawLockMarker()
        {
            if (player == null || player.LockTarget == null || Camera.main == null)
            {
                return;
            }

            Vector3 screen = Camera.main.WorldToScreenPoint(player.LockTarget.transform.position + Vector3.up * 1.55f);
            if (screen.z <= 0f)
            {
                return;
            }

            float guiY = Screen.height - screen.y;
            GUI.Box(new Rect(screen.x - 28f, guiY - 12f, 56f, 24f), "LOCK");
        }
    }
}
