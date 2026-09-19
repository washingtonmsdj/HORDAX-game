using UnityEngine;
using HORDAX.Data;
using HORDAX.Player;
using HORDAX.Prototype;

namespace HORDAX.Enemies
{
    public sealed class HordeSpawner : MonoBehaviour
    {
        [SerializeField] private int count = 18;
        [SerializeField] private int columns = 6;
        [SerializeField] private float activationDistance = 34f;
        [SerializeField] private float enemyHealth = 5f;
        [SerializeField] private float enemySpeed = 3.2f;
        [SerializeField] private float enemyDamage = 8f;
        [SerializeField] private EnemyRank enemyRank = EnemyRank.Grunt;
        [SerializeField] private int coinReward = 1;
        [SerializeField] private int scoreReward = 10;
        [SerializeField] private float scaleMultiplier = 1f;
        [SerializeField] private float spawnJitter = 0.18f;
        [SerializeField] private float laneCenterX;
        [SerializeField] private float laneHalfWidth = 2.75f;
        [SerializeField] private bool constrainToLane;
        [SerializeField, Min(1)] private int spawnPerBatch = 6;
        [SerializeField, Min(0.01f)] private float spawnInterval = 0.045f;
        [SerializeField] private EnemyData enemyData;
        [SerializeField] private GameObject enemyPrefab;

        private RunnerController player;
        private EnemyPool pool;
        private bool activated;
        private int spawnedCount;
        private float spawnTimer;

        public void Configure(
            RunnerController runner,
            int enemyCount,
            int columnCount,
            float health,
            float speed,
            float damage,
            EnemyData definition = null,
            EnemyRank fallbackRank = EnemyRank.Grunt,
            int fallbackCoinReward = 1,
            int fallbackScoreReward = 10,
            float fallbackScale = 1f)
        {
            player = runner;
            count = Mathf.Max(1, enemyCount);
            columns = Mathf.Max(1, columnCount);
            enemyHealth = health;
            enemySpeed = speed;
            enemyDamage = damage;
            enemyData = definition;
            enemyRank = fallbackRank;
            coinReward = Mathf.Max(0, fallbackCoinReward);
            scoreReward = Mathf.Max(0, fallbackScoreReward);
            scaleMultiplier = Mathf.Max(0.1f, fallbackScale);
        }

        public void ConfigureLane(float centerX, float halfWidth)
        {
            laneCenterX = centerX;
            laneHalfWidth = Mathf.Max(0.5f, halfWidth);
            constrainToLane = true;
        }

        private void Start()
        {
            if (player == null) player = FindAnyObjectByType<RunnerController>();
            pool = FindAnyObjectByType<EnemyPool>();

            if (pool == null)
            {
                GameObject poolObject = new GameObject("Enemy Pool");
                pool = poolObject.AddComponent<EnemyPool>();
            }
        }

        private void Update()
        {
            if (player == null || spawnedCount >= count) return;

            if (!activated)
            {
                if (player.transform.position.z < transform.position.z - activationDistance) return;
                activated = true;
                spawnTimer = 0f;
            }

            spawnTimer -= Time.deltaTime;
            if (spawnTimer > 0f) return;

            SpawnBatch();
            spawnTimer = spawnInterval;
        }

        private void SpawnBatch()
        {
            int batchEnd = Mathf.Min(count, spawnedCount + Mathf.Max(1, spawnPerBatch));
            for (; spawnedCount < batchEnd; spawnedCount++)
                SpawnOne(spawnedCount);
        }

        private void SpawnOne(int index)
        {
            const float maxSpacingX = 1.08f;
            const float spacingZ = 1.10f;

            float health = enemyData != null ? enemyData.Health : enemyHealth;
            float speed = enemyData != null ? enemyData.MoveSpeed : enemySpeed;
            float damage = enemyData != null ? enemyData.ContactDamage : enemyDamage;
            EnemyRank rank = enemyData != null ? enemyData.Rank : enemyRank;
            int coins = enemyData != null ? enemyData.CoinReward : coinReward;
            int score = enemyData != null ? enemyData.ScoreReward : scoreReward;
            float size = enemyData != null ? enemyData.ScaleMultiplier : scaleMultiplier;
            GameObject requestedPrefab = enemyData != null && enemyData.VisualPrefab != null ? enemyData.VisualPrefab : enemyPrefab;

            int effectiveColumns = constrainToLane ? Mathf.Clamp(columns, 1, 6) : columns;
            int col = index % effectiveColumns;
            int row = index / effectiveColumns;
            float availableWidth = constrainToLane
                ? laneHalfWidth * 2f
                : Mathf.Max(0f, (effectiveColumns - 1) * maxSpacingX);
            float spacingX = effectiveColumns > 1
                ? Mathf.Min(maxSpacingX, availableWidth / Mathf.Max(1, effectiveColumns - 1))
                : 0f;
            float widthOffset = (effectiveColumns - 1) * spacingX * 0.5f;
            float localX = col * spacingX - widthOffset + Random.Range(-spawnJitter, spawnJitter);
            float x = constrainToLane
                ? Mathf.Clamp(laneCenterX + localX, laneCenterX - laneHalfWidth, laneCenterX + laneHalfWidth)
                : localX;
            float z = row * spacingZ + Random.Range(-spawnJitter, spawnJitter);

            EnemyAgent agent = pool.Acquire(requestedPrefab);
            GameObject enemy = agent.gameObject;
            enemy.name = $"{name}_Enemy_{index:000}";
            enemy.transform.SetParent(null, true);
            float spawnHeight = requestedPrefab == null ? 0.95f : 0.6f;
            enemy.transform.position = new Vector3(x, transform.position.y + spawnHeight * size, transform.position.z + z);
            enemy.transform.rotation = Quaternion.Euler(0f, 180f + Random.Range(-6f, 6f), 0f);
            enemy.transform.localScale = new Vector3(0.86f, Random.Range(1.05f, 1.28f), 0.86f) * size;

            if (requestedPrefab == null)
            {
                Material material = rank == EnemyRank.Boss
                    ? PrototypeMaterials.Boss
                    : rank == EnemyRank.Elite ? PrototypeMaterials.Elite : PrototypeMaterials.Enemy;

                Renderer[] renderers = enemy.GetComponentsInChildren<Renderer>();
                for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
                    renderers[rendererIndex].sharedMaterial = material;
            }

            agent.Initialize(
                player,
                health,
                speed,
                damage,
                pool,
                rank,
                coins,
                score,
                constrainToLane,
                x);
            enemy.SetActive(true);
        }
    }
}
