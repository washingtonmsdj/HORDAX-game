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
        [SerializeField] private float hitPunch = 0.16f;
        [SerializeField] private float hitPunchRecovery = 14f;

        private RunnerController player;
        private PlayerHealth playerHealth;
        private EnemyPool ownerPool;
        private float health;
        private float attackTimer;
        private Vector3 baseScale;
        private float punch;

        public override Vector3 TargetPoint => transform.position + Vector3.up * 0.55f;

        public void Initialize(RunnerController runner, float healthValue, float speedValue, float damageValue, EnemyPool pool)
        {
            player = runner;
            playerHealth = runner != null ? runner.GetComponent<PlayerHealth>() : null;
            ownerPool = pool;
            maxHealth = Mathf.Max(1f, healthValue);
            moveSpeed = Mathf.Max(0f, speedValue);
            contactDamage = Mathf.Max(0f, damageValue);
            health = maxHealth;
            attackTimer = Random.Range(0f, attackInterval * 0.5f);
            punch = 0f;
            baseScale = transform.localScale;
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
            if (baseScale == Vector3.zero) baseScale = transform.localScale;
        }

        private void Update()
        {
            if (punch > 0f)
            {
                punch = Mathf.MoveTowards(punch, 0f, hitPunchRecovery * Time.deltaTime);
                transform.localScale = baseScale * (1f + punch);
            }
            else if (transform.localScale != baseScale)
            {
                transform.localScale = baseScale;
            }

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
            punch = hitPunch;
            if (health > 0f) return;

            if (GameManager.Instance != null) GameManager.Instance.RegisterEnemyKill();
            Die();
        }

        private void Die()
        {
            if (ownerPool != null)
            {
                ownerPool.Release(this);
                return;
            }

            Destroy(gameObject);
        }
    }
}
