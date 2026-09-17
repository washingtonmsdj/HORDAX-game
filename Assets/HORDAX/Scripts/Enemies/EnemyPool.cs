using System.Collections.Generic;
using UnityEngine;
using HORDAX.Prototype;

namespace HORDAX.Enemies
{
    /// <summary>
    /// Lightweight pool used by the blockout horde. Keeping enemy lifetime out of
    /// Instantiate/Destroy makes it possible to push crowd counts much higher on mobile.
    /// For final art, replace the prototype cube through the prefab field without changing gameplay code.
    /// </summary>
    public sealed class EnemyPool : MonoBehaviour
    {
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private int prewarmCount = 96;

        private readonly Queue<EnemyAgent> inactive = new Queue<EnemyAgent>();
        private int createdCount;

        public int CreatedCount => createdCount;
        public int AvailableCount => inactive.Count;

        private void Awake()
        {
            Prewarm(prewarmCount);
        }

        public void Configure(GameObject prefab, int initialCapacity)
        {
            enemyPrefab = prefab;
            prewarmCount = Mathf.Max(0, initialCapacity);
        }

        public void Prewarm(int count)
        {
            while (createdCount < count)
            {
                EnemyAgent agent = CreateAgent(enemyPrefab);
                inactive.Enqueue(agent);
            }
        }

        public EnemyAgent Acquire(GameObject overridePrefab = null)
        {
            if (inactive.Count > 0)
                return inactive.Dequeue();

            return CreateAgent(overridePrefab != null ? overridePrefab : enemyPrefab);
        }

        public void Release(EnemyAgent agent)
        {
            if (agent == null) return;

            agent.gameObject.SetActive(false);
            agent.transform.SetParent(transform, false);
            inactive.Enqueue(agent);
        }

        private EnemyAgent CreateAgent(GameObject prefab)
        {
            GameObject enemy;
            if (prefab != null)
            {
                enemy = Instantiate(prefab, transform);
            }
            else
            {
                enemy = GameObject.CreatePrimitive(PrimitiveType.Cube);
                enemy.transform.SetParent(transform, false);
                enemy.transform.localScale = new Vector3(0.9f, 1.2f, 0.9f);
                Renderer renderer = enemy.GetComponentInChildren<Renderer>();
                if (renderer != null) renderer.sharedMaterial = PrototypeMaterials.Enemy;
            }

            enemy.name = $"Enemy_Pooled_{createdCount:000}";
            EnemyAgent agent = enemy.GetComponent<EnemyAgent>();
            if (agent == null) agent = enemy.AddComponent<EnemyAgent>();

            createdCount++;
            enemy.SetActive(false);
            return agent;
        }
    }
}
