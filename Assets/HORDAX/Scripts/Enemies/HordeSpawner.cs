using UnityEngine;
using HORDAX.Player;
using HORDAX.Prototype;

namespace HORDAX.Enemies
{
    public sealed class HordeSpawner : MonoBehaviour
    {
        [SerializeField] private int count = 18;
        [SerializeField] private int columns = 6;
        [SerializeField] private float activationDistance = 28f;
        [SerializeField] private float enemyHealth = 5f;
        [SerializeField] private float enemySpeed = 3.2f;
        [SerializeField] private float enemyDamage = 8f;
        [SerializeField] private GameObject enemyPrefab;

        private RunnerController player;
        private bool spawned;

        public void Configure(RunnerController runner, int enemyCount, int columnCount, float health, float speed, float damage)
        {
            player = runner;
            count = enemyCount;
            columns = Mathf.Max(1, columnCount);
            enemyHealth = health;
            enemySpeed = speed;
            enemyDamage = damage;
        }

        private void Start()
        {
            if (player == null) player = FindObjectOfType<RunnerController>();
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
            const float spacingX = 1.35f;
            const float spacingZ = 1.25f;

            for (int i = 0; i < count; i++)
            {
                int col = i % columns;
                int row = i / columns;
                float widthOffset = (columns - 1) * spacingX * 0.5f;
                float x = col * spacingX - widthOffset;
                float z = row * spacingZ;

                GameObject enemy = enemyPrefab != null
                    ? Instantiate(enemyPrefab)
                    : GameObject.CreatePrimitive(PrimitiveType.Cube);

                enemy.name = $"Enemy_{i:00}";
                enemy.transform.position = transform.position + new Vector3(x, 0.6f, z);
                enemy.transform.localScale = new Vector3(0.9f, 1.2f, 0.9f);

                Renderer renderer = enemy.GetComponentInChildren<Renderer>();
                if (renderer != null) renderer.sharedMaterial = PrototypeMaterials.Enemy;

                EnemyAgent agent = enemy.GetComponent<EnemyAgent>();
                if (agent == null) agent = enemy.AddComponent<EnemyAgent>();
                agent.Initialize(player, enemyHealth, enemySpeed, enemyDamage);
            }
        }
    }
}
