using System.Collections.Generic;
using IronSand.Combat;
using IronSand.Enemy;
using IronSand.Player;
using IronSand.Scoring;
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
        [SerializeField, Min(2f)] private float spawnRadius = 10f;

        private readonly List<EnemyGladiator> aliveEnemies = new();
        private readonly HashSet<EnemyGladiator> attackTokens = new();
        private int currentWaveIndex = -1;

        public IReadOnlyList<EnemyGladiator> AliveEnemies => aliveEnemies;
        public int WaveNumber => currentWaveIndex + 1;
        public int TotalWaves => enemiesPerWave.Length;
        public bool Victory { get; private set; }

        private void Start()
        {
            player ??= FindFirstObjectByType<PlayerGladiator>();
            crowdFavor ??= FindFirstObjectByType<CrowdFavorSystem>();
            styleSystem ??= FindFirstObjectByType<CombatStyleSystem>();

            if (player != null && crowdFavor != null)
            {
                crowdFavor.RewardEarned += player.Heal;
            }

            BeginNextWave();
        }

        private void OnDestroy()
        {
            if (player != null && crowdFavor != null)
            {
                crowdFavor.RewardEarned -= player.Heal;
            }
        }

        public bool TryAcquireAttackToken(EnemyGladiator enemy)
        {
            if (enemy == null || attackTokens.Contains(enemy) || attackTokens.Count >= maxConcurrentAttackers)
            {
                return false;
            }

            attackTokens.Add(enemy);
            return true;
        }

        public void ReleaseAttackToken(EnemyGladiator enemy)
        {
            if (enemy != null)
            {
                attackTokens.Remove(enemy);
            }
        }

        public void RegisterPlayerHit(AttackKind attack, WeaponArchetype weapon, bool kill, int extraFavor)
        {
            int favor = 1 + Mathf.Max(0, extraFavor);
            if (styleSystem != null)
            {
                StyleAward award = styleSystem.RegisterHit(attack, weapon, kill);
                favor = award.Favor + Mathf.Max(0, extraFavor);
            }

            crowdFavor?.AddFavor(favor);
        }

        private void BeginNextWave()
        {
            currentWaveIndex++;
            if (currentWaveIndex >= enemiesPerWave.Length)
            {
                Victory = true;
                return;
            }

            int count = Mathf.Max(1, enemiesPerWave[currentWaveIndex]);
            for (int i = 0; i < count; i++)
            {
                float angle = (Mathf.PI * 2f * i / count) + currentWaveIndex * 0.37f;
                Vector3 position = new(Mathf.Cos(angle) * spawnRadius, 1f, Mathf.Sin(angle) * spawnRadius);
                SpawnEnemy(position, i);
            }
        }

        private void SpawnEnemy(Vector3 position, int index)
        {
            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            root.name = $"Enemy_W{WaveNumber}_{index + 1}";
            root.transform.position = position;
            root.transform.localScale = new Vector3(0.9f, 1f, 0.9f);

            Collider primitiveCollider = root.GetComponent<Collider>();
            if (primitiveCollider != null)
            {
                Destroy(primitiveCollider);
            }

            CharacterController controller = root.AddComponent<CharacterController>();
            controller.height = 2f;
            controller.radius = 0.45f;
            controller.center = Vector3.zero;

            WeaponArchetype weapon = WeaponCatalog.GetArenaWeapon(currentWaveIndex * 17 + index);
            EnemyGladiator enemy = root.AddComponent<EnemyGladiator>();
            enemy.Initialize(player, this, weapon);
            enemy.Died += OnEnemyDied;
            aliveEnemies.Add(enemy);
        }

        private void OnEnemyDied(Combatant combatant)
        {
            if (combatant is not EnemyGladiator enemy)
            {
                return;
            }

            enemy.Died -= OnEnemyDied;
            ReleaseAttackToken(enemy);
            aliveEnemies.Remove(enemy);

            if (aliveEnemies.Count == 0 && !Victory)
            {
                BeginNextWave();
            }
        }
    }
}
