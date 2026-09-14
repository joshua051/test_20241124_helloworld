using System.Collections.Generic;
using IronSand.Combat;
using IronSand.Enemy;
using IronSand.Player;
using IronSand.Scoring;
using IronSand.Telemetry;
using UnityEngine;

namespace IronSand.Arena
{
    public sealed class ArenaDirector : MonoBehaviour
    {
        [SerializeField] private PlayerGladiator player;
        [SerializeField] private CrowdFavorSystem crowdFavor;
        [SerializeField] private CombatStyleSystem styleSystem;
        [SerializeField, Min(1)] private int maxConcurrentAttackers = 2;
        [SerializeField] private int[] enemiesPerWave = { 3, 4, 5 };
        [SerializeField, Min(2f)] private float spawnRadius = ArenaGeometry.SpawnRadius;
        [SerializeField, Min(0f)] private float intermissionSeconds = 1.5f;
        private readonly List<EnemyGladiator> aliveEnemies = new();
        private readonly HashSet<EnemyGladiator> attackTokens = new();
        private int currentWaveIndex = -1;
        private bool nextWavePending;
        public IReadOnlyList<EnemyGladiator> AliveEnemies => aliveEnemies;
        public int WaveNumber => Mathf.Clamp(currentWaveIndex + 1, 0, TotalWaves);
        public int TotalWaves => enemiesPerWave != null ? enemiesPerWave.Length : 0;
        public int ActiveAttackers => attackTokens.Count;
        public float IntermissionRemaining { get; private set; }
        public bool Victory { get; private set; }
        private void Start()
        {
            if (player == null) player = FindFirstObjectByType<PlayerGladiator>(); if (crowdFavor == null) crowdFavor = FindFirstObjectByType<CrowdFavorSystem>(); if (styleSystem == null) styleSystem = FindFirstObjectByType<CombatStyleSystem>();
            if (player == null || crowdFavor == null || styleSystem == null || TotalWaves == 0) { Debug.LogError("Arena setup incomplete. Rebuild the prototype scene.", this); enabled = false; return; }
            BeginNextWave();
        }
        private void Update()
        {
            if (!nextWavePending || Victory || player == null || player.IsDead || Time.timeScale <= 0f || Time.deltaTime <= 0f) return;
            IntermissionRemaining = Mathf.Max(0f, IntermissionRemaining - Time.deltaTime); if (IntermissionRemaining <= 0f) { nextWavePending = false; BeginNextWave(); }
        }
        private void OnDestroy() { foreach (EnemyGladiator enemy in aliveEnemies) if (enemy != null) enemy.Died -= OnEnemyDied; attackTokens.Clear(); }
        public bool TryAcquireAttackToken(EnemyGladiator enemy)
        {
            attackTokens.RemoveWhere(entry => entry == null || entry.IsDead || !entry.isActiveAndEnabled);
            if (Victory || player == null || player.IsDead || enemy == null || enemy.IsDead || !aliveEnemies.Contains(enemy) || attackTokens.Contains(enemy) || attackTokens.Count >= maxConcurrentAttackers) return false;
            attackTokens.Add(enemy);
            TelemetryRecorder.RecordEvent("attack_token_acquired", enemy.name, player != null ? player.name : null, null, null, null, null, enemy.transform.position,
                $"{{\"active_attackers\":{attackTokens.Count}}}");
            return true;
        }
        public void ReleaseAttackToken(EnemyGladiator enemy) { if (enemy != null) attackTokens.Remove(enemy); }
        public void RegisterPlayerHit(AttackKind attack, WeaponArchetype weapon, bool kill, int extraFavor)
        {
            int favor = 1 + Mathf.Max(0, extraFavor);
            int score = 0;
            int combo = 0;
            if (styleSystem != null)
            {
                StyleAward award = styleSystem.RegisterHit(attack, weapon, kill);
                favor = award.Favor + Mathf.Max(0, extraFavor);
                score = styleSystem.Score;
                combo = styleSystem.ComboCount;
            }
            crowdFavor?.AddFavor(favor);
            TelemetryRecorder.RecordEvent("player_scored_hit", player != null ? player.name : "Player", null, weapon, attack, null, null,
                player != null ? player.transform.position : Vector3.zero,
                $"{{\"kill\":{kill.ToString().ToLowerInvariant()},\"favor_award\":{favor},\"combo\":{combo},\"style_score\":{score},\"crowd_favor\":{(crowdFavor != null ? crowdFavor.Favor : 0)}}}");
        }
        private void BeginNextWave()
        {
            currentWaveIndex++;
            if (currentWaveIndex >= TotalWaves)
            {
                Victory = true;
                TelemetryRecorder.RecordEvent("victory", player != null ? player.name : "Player", null, null, null, null, null,
                    player != null ? player.transform.position : Vector3.zero,
                    $"{{\"waves\":{TotalWaves},\"style_score\":{(styleSystem != null ? styleSystem.Score : 0)},\"crowd_favor\":{(crowdFavor != null ? crowdFavor.Favor : 0)}}}");
                return;
            }
            int count = Mathf.Max(1, enemiesPerWave[currentWaveIndex]); float radius = Mathf.Clamp(spawnRadius, 2f, ArenaGeometry.WallRadius - 2f);
            TelemetryRecorder.RecordEvent("wave_start", "ArenaDirector", null, null, null, null, null, Vector3.zero,
                $"{{\"wave\":{WaveNumber},\"enemy_count\":{count}}}");
            for (int i = 0; i < count; i++) { float angle = Mathf.PI * 2f * i / count + currentWaveIndex * 0.37f; SpawnEnemy(new Vector3(Mathf.Cos(angle) * radius, 1.1f, Mathf.Sin(angle) * radius), i); }
        }
        private void SpawnEnemy(Vector3 position, int index)
        {
            GameObject root = new($"Enemy_W{WaveNumber}_{index + 1}"); root.transform.position = position; CharacterController controller = root.AddComponent<CharacterController>(); controller.height = 2f; controller.radius = 0.45f; controller.center = Vector3.zero;
            WeaponArchetype weapon = WeaponCatalog.GetArenaWeapon(currentWaveIndex * 17 + index); EnemyRole role = (EnemyRole)((currentWaveIndex + index) % 4); EnemyGladiator enemy = root.AddComponent<EnemyGladiator>(); enemy.Initialize(player, this, weapon, role); enemy.Died += OnEnemyDied; aliveEnemies.Add(enemy);
            TelemetryRecorder.RecordEvent("enemy_spawn", enemy.name, player != null ? player.name : null, weapon, null, null, null, position,
                $"{{\"wave\":{WaveNumber},\"role\":\"{role}\"}}");
        }
        private void OnEnemyDied(Combatant combatant)
        {
            if (combatant is not EnemyGladiator enemy) return; enemy.Died -= OnEnemyDied; ReleaseAttackToken(enemy); aliveEnemies.Remove(enemy);
            TelemetryRecorder.RecordEvent("enemy_death", enemy.name, player != null ? player.name : null, enemy.HeldWeapon, null, null, null, enemy.transform.position,
                $"{{\"wave\":{WaveNumber},\"remaining\":{aliveEnemies.Count}}}");
            if (aliveEnemies.Count == 0 && !Victory) { nextWavePending = true; IntermissionRemaining = intermissionSeconds; }
        }
    }
}
