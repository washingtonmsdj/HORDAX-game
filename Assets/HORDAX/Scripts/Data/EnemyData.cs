using UnityEngine;

namespace HORDAX.Data
{
    [CreateAssetMenu(fileName = "EnemyData", menuName = "HORDAX/Enemy Data")]
    public sealed class EnemyData : ScriptableObject
    {
        [SerializeField] private string enemyId = "grunt";
        [SerializeField] private string displayName = "Grunt";
        [SerializeField] private EnemyRank rank = EnemyRank.Grunt;

        [Header("Combat")]
        [SerializeField, Min(1f)] private float health = 5f;
        [SerializeField, Min(0f)] private float moveSpeed = 3.2f;
        [SerializeField, Min(0f)] private float contactDamage = 8f;
        [SerializeField, Min(0.1f)] private float scaleMultiplier = 1f;

        [Header("Rewards")]
        [SerializeField, Min(0)] private int coinReward = 1;
        [SerializeField, Min(0)] private int scoreReward = 10;

        [Header("Presentation")]
        [SerializeField] private GameObject visualPrefab;

        public string EnemyId => enemyId;
        public string DisplayName => displayName;
        public EnemyRank Rank => rank;
        public float Health => health;
        public float MoveSpeed => moveSpeed;
        public float ContactDamage => contactDamage;
        public float ScaleMultiplier => scaleMultiplier;
        public int CoinReward => coinReward;
        public int ScoreReward => scoreReward;
        public GameObject VisualPrefab => visualPrefab;
    }
}
