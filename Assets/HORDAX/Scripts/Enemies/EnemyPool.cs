using System.Collections.Generic;
using UnityEngine;
using HORDAX.Prototype;

namespace HORDAX.Enemies
{
    /// <summary>
    /// Pool keyed by prefab source so future enemy archetypes do not get mixed together.
    /// Null prefab means the built-in prototype cube.
    /// </summary>
    public sealed class EnemyPool : MonoBehaviour
    {
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private int prewarmCount = 96;

        private readonly Queue<EnemyAgent> prototypeInactive = new Queue<EnemyAgent>();
        private readonly Dictionary<GameObject, Queue<EnemyAgent>> prefabInactive = new Dictionary<GameObject, Queue<EnemyAgent>>();
        private readonly Dictionary<EnemyAgent, GameObject> sourcePrefabByAgent = new Dictionary<EnemyAgent, GameObject>();
        private int createdCount;

        public int CreatedCount => createdCount;
        public int AvailableCount
        {
            get
            {
                int total = prototypeInactive.Count;
                foreach (KeyValuePair<GameObject, Queue<EnemyAgent>> pair in prefabInactive)
                    total += pair.Value.Count;
                return total;
            }
        }

        private void Awake()
        {
            Prewarm(prewarmCount, enemyPrefab);
        }

        public void Configure(GameObject prefab, int initialCapacity)
        {
            enemyPrefab = prefab;
            prewarmCount = Mathf.Max(0, initialCapacity);
            Prewarm(prewarmCount, enemyPrefab);
        }

        public void Prewarm(int count, GameObject prefab = null)
        {
            Queue<EnemyAgent> queue = GetQueue(prefab);
            while (queue.Count < count)
            {
                EnemyAgent agent = CreateAgent(prefab);
                queue.Enqueue(agent);
            }
        }

        public EnemyAgent Acquire(GameObject overridePrefab = null)
        {
            GameObject requestedPrefab = overridePrefab != null ? overridePrefab : enemyPrefab;
            Queue<EnemyAgent> queue = GetQueue(requestedPrefab);
            if (queue.Count > 0)
                return queue.Dequeue();

            return CreateAgent(requestedPrefab);
        }

        public void Release(EnemyAgent agent)
        {
            if (agent == null) return;

            agent.gameObject.SetActive(false);
            agent.transform.SetParent(transform, false);

            GameObject sourcePrefab;
            sourcePrefabByAgent.TryGetValue(agent, out sourcePrefab);
            GetQueue(sourcePrefab).Enqueue(agent);
        }

        private Queue<EnemyAgent> GetQueue(GameObject prefab)
        {
            if (prefab == null) return prototypeInactive;

            Queue<EnemyAgent> queue;
            if (!prefabInactive.TryGetValue(prefab, out queue))
            {
                queue = new Queue<EnemyAgent>();
                prefabInactive.Add(prefab, queue);
            }
            return queue;
        }

        private static void CreateVisualPart(
            Transform parent,
            PrimitiveType type,
            string label,
            Vector3 localPosition,
            Vector3 localScale)
        {
            GameObject part = GameObject.CreatePrimitive(type);
            part.name = label;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = localPosition;
            part.transform.localScale = localScale;

            Renderer renderer = part.GetComponent<Renderer>();
            if (renderer != null)
                renderer.sharedMaterial = PrototypeMaterials.Enemy;

            Collider collider = part.GetComponent<Collider>();
            if (collider != null)
                Destroy(collider);
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
                enemy = new GameObject("Prototype Enemy");
                enemy.transform.SetParent(transform, false);

                BoxCollider collider = enemy.AddComponent<BoxCollider>();
                collider.center = Vector3.zero;
                collider.size = new Vector3(0.82f, 1.75f, 0.72f);

                CreateVisualPart(
                    enemy.transform,
                    PrimitiveType.Cube,
                    "Body",
                    new Vector3(0f, 0.10f, 0f),
                    new Vector3(0.72f, 0.88f, 0.46f));

                CreateVisualPart(
                    enemy.transform,
                    PrimitiveType.Sphere,
                    "Head",
                    new Vector3(0f, 0.83f, 0f),
                    Vector3.one * 0.46f);

                CreateVisualPart(
                    enemy.transform,
                    PrimitiveType.Cube,
                    "Left Arm",
                    new Vector3(-0.49f, 0.12f, 0f),
                    new Vector3(0.18f, 0.72f, 0.18f));

                CreateVisualPart(
                    enemy.transform,
                    PrimitiveType.Cube,
                    "Right Arm",
                    new Vector3(0.49f, 0.12f, 0f),
                    new Vector3(0.18f, 0.72f, 0.18f));

                CreateVisualPart(
                    enemy.transform,
                    PrimitiveType.Cube,
                    "Left Leg",
                    new Vector3(-0.20f, -0.62f, 0f),
                    new Vector3(0.22f, 0.72f, 0.24f));

                CreateVisualPart(
                    enemy.transform,
                    PrimitiveType.Cube,
                    "Right Leg",
                    new Vector3(0.20f, -0.62f, 0f),
                    new Vector3(0.22f, 0.72f, 0.24f));
            }

            enemy.name = $"Enemy_Pooled_{createdCount:000}";
            EnemyAgent agent = enemy.GetComponent<EnemyAgent>();
            if (agent == null) agent = enemy.AddComponent<EnemyAgent>();

            sourcePrefabByAgent[agent] = prefab;
            createdCount++;
            enemy.SetActive(false);
            return agent;
        }
    }
}
