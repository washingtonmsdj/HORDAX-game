using UnityEngine;

namespace HORDAX.Data
{
    [CreateAssetMenu(fileName = "EnemyData", menuName = "HORDAX/Enemy Data")]
    public sealed class EnemyData : ScriptableObject
    {
        [SerializeField] private string enemyId = "grunt";
        [SerializeField] private string displayName = "Grunt";
        [SerializeField] private float health = 5f;
        [SerializeField] private float moveSpeed = 3.2f;
        [SerializeField] private float contactDamage = 8f;
        [SerializeField] private GameObject visualPrefab;

        public string EnemyId => enemyId;
        public string DisplayName => displayName;
        public float Health => health;
        public float MoveSpeed => moveSpeed;
        public float ContactDamage => contactDamage;
        public GameObject VisualPrefab => visualPrefab;
    }
}
