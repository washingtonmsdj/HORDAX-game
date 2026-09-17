using UnityEngine;
using HORDAX.Player;
using HORDAX.Prototype;

namespace HORDAX.Enemies
{
    public sealed class HordeSpawner : MonoBehaviour
    {
        [SerializeField] private int count = 18;
        [SerializeField] private int columns = 6;
        [SerializeField] private float activationDistance = 32f;
        [SerializeField] private float enemyHealth = 5f;
        [SerializeField] private float enemySpeed = 3.2f;
        [SerializeField] private float enemyDamage = 8f;
        [SerializeField] private float spawnJitter = 0.24f;
        [SerializeField] private GameObject enemyPrefab;

        private RunnerController player;
        private EnemyPool pool;
        private bool spawned;

        public void Configure(RunnerController runner, int enemyCount, int columnCount, float health, float speed, float damage)
        {
            player = runner;
            count = Mathf.Max(1, enemyCount);
            columns = Mathf.Max(1, columnCount);
            enemyHealth = health;
            enemySpeed = speed;
            enemyDamage = damage;
        }

        private void Start()
        {
            if (player == null) player = FindObjectOfType<RunnerController>();
            pool = FindObjectOfType<EnemyPool>();

            if (pool == null)
            {
                GameObject poolObject = new GameObject("Enemy Pool");
                pool = poolObject.AddComponent<EnemyPool>();
            }
        }

        private void Update()
        {
            if (spawned || player == null) return;
            if (player.transform.position.z < transform.position.z - activationDistance) return;

            spawned = true;
            Spawn();
        }

        private void Spawn()
        {
            const float spacingX = 1.24f;
            const float spacingZ = 1.10f;

            for (int i = 0; i < count; i++)
            {
                int col = i % columns;
                int row = i / columns;
                float widthOffset = (columns - 1) * spacingX * 0.5f;
                float x = col * spacingX - widthOffset + Random.Range(-spawnJitter, spawnJitter);
                float z = row * spacingZ + Random.Range(-spawnJitter, spawnJitter);

                EnemyAgent agent = pool.Acquire(enemyPrefab);
                GameObject enemy = agent.gameObject;
                enemy.name = $"{name}_Enemy_{i:000}";
                enemy.transform.SetParent(null, true);
                enemy.transform.position = transform.position + new Vector3(x, 0.6f, z);
                enemy.transform.rotation = Quaternion.Euler(0f, 180f + Random.Range(-6f, 6f), 0f);
                enemy.transform.localScale = new Vector3(0.86f, Random.Range(1.05f, 1.28f), 0.86f);

                Renderer renderer = enemy.GetComponentInChildren<Renderer>();
                if (renderer != null && enemyPrefab == null) renderer.sharedMaterial = PrototypeMaterials.Enemy;

                agent.Initialize(player, enemyHealth, enemySpeed, enemyDamage, pool);
                enemy.SetActive(true);
            }
        }
    }
}
