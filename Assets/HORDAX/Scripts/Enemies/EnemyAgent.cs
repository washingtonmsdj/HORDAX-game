using UnityEngine;
using HORDAX.Combat;
using HORDAX.Core;
using HORDAX.Player;

namespace HORDAX.Enemies
{
    public sealed class EnemyAgent : ShootableTarget
    {
        [SerializeField] private float maxHealth = 5f;
        [SerializeField] private float moveSpeed = 3.2f;
        [SerializeField] private float contactDamage = 8f;
        [SerializeField] private float attackInterval = 0.65f;
        [SerializeField] private float attackDistance = 1.1f;

        private RunnerController player;
        private PlayerHealth playerHealth;
        private float health;
        private float attackTimer;

        public void Initialize(RunnerController runner, float healthValue, float speedValue, float damageValue)
        {
            player = runner;
            playerHealth = runner != null ? runner.GetComponent<PlayerHealth>() : null;
            maxHealth = healthValue;
            moveSpeed = speedValue;
            contactDamage = damageValue;
            health = maxHealth;
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            health = maxHealth;
        }

        private void Start()
        {
            if (player == null) player = FindObjectOfType<RunnerController>();
            if (player != null && playerHealth == null) playerHealth = player.GetComponent<PlayerHealth>();
            if (health <= 0f) health = maxHealth;
        }

        private void Update()
        {
            if (player == null || GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;

            Vector3 toPlayer = player.transform.position - transform.position;
            float planarDistance = new Vector2(toPlayer.x, toPlayer.z).magnitude;

            if (planarDistance > attackDistance)
            {
                Vector3 direction = new Vector3(toPlayer.x, 0f, toPlayer.z).normalized;
                transform.position += direction * moveSpeed * Time.deltaTime;
                if (direction.sqrMagnitude > 0.01f) transform.forward = direction;
            }
            else
            {
                attackTimer -= Time.deltaTime;
                if (attackTimer <= 0f)
                {
                    playerHealth?.Damage(contactDamage);
                    attackTimer = attackInterval;
                }
            }
        }

        public override void TakeDamage(float amount)
        {
            if (health <= 0f) return;

            health -= Mathf.Max(0f, amount);
            if (health > 0f) return;

            if (GameManager.Instance != null) GameManager.Instance.RegisterEnemyKill();
            Destroy(gameObject);
        }
    }
}
