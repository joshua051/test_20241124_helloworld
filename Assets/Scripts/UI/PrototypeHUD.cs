using IronSand.Arena;
using IronSand.Player;
using UnityEngine;

namespace IronSand.UI
{
    public sealed class PrototypeHUD : MonoBehaviour
    {
        private PlayerGladiator player;
        private CrowdFavorSystem favor;
        private ArenaDirector director;

        private void Start()
        {
            player = FindFirstObjectByType<PlayerGladiator>();
            favor = FindFirstObjectByType<CrowdFavorSystem>();
            director = FindFirstObjectByType<ArenaDirector>();
        }

        private void OnGUI()
        {
            GUI.Box(new Rect(16f, 16f, 360f, 142f), "IRON SAND ARENA — PROTOTYPE");

            string hp = player == null ? "--" : $"{Mathf.CeilToInt(player.Health)} / {Mathf.CeilToInt(player.MaxHealth)}";
            string crowd = favor == null ? "--" : $"{favor.Favor} / {favor.MaxFavor}";
            string wave = director == null ? "--" : $"{director.WaveNumber} / {director.TotalWaves}";
            string enemies = director == null ? "--" : director.AliveEnemies.Count.ToString();

            GUI.Label(new Rect(32f, 46f, 330f, 20f), $"Health: {hp}");
            GUI.Label(new Rect(32f, 68f, 330f, 20f), $"Crowd Favor: {crowd}");
            GUI.Label(new Rect(32f, 90f, 330f, 20f), $"Wave: {wave}   Enemies: {enemies}");
            GUI.Label(new Rect(32f, 112f, 330f, 40f), "LMB Light | RMB Heavy | Q Guard | Space Dodge");

            if (director != null && director.Victory)
            {
                GUI.Box(new Rect(Screen.width * 0.5f - 150f, 50f, 300f, 56f), "VICTORY\nPrototype loop complete");
            }
            else if (player != null && player.IsDead)
            {
                GUI.Box(new Rect(Screen.width * 0.5f - 150f, 50f, 300f, 56f), "DEFEAT\nExit Play Mode to restart");
            }
        }
    }
}
