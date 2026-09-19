using UnityEngine;
using HORDAX.CameraSystem;
using HORDAX.Combat;
using HORDAX.Core;
using HORDAX.Data;
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

        [Header("Crowd LOD")]
        [SerializeField] private float nearLogicInterval = 0.045f;
        [SerializeField] private float farLogicInterval = 0.16f;
        [SerializeField] private float farDistance = 22f;

        private RunnerController player;
        private PlayerHealth playerHealth;
        private EnemyPool ownerPool;
        private EnemyRank rank = EnemyRank.Grunt;
        private int coinReward = 1;
        private int scoreReward = 10;
        private float health;
        private float attackTimer;
        private Vector3 baseScale;
        private float punch;
        private float logicTimer;
        private float cachedDistance = float.MaxValue;
        private Vector3 cachedDirection;
        private int bossBarrierOwnerId;
        private bool laneConstrained;
        private float laneTargetX;
        private bool breachedPlayerLine;

        public static EnemyAgent ActiveBoss { get; private set; }

        public EnemyRank Rank => rank;
        public float HealthNormalized => maxHealth <= 0f ? 0f : Mathf.Clamp01(health / maxHealth);
        public override Vector3 TargetPoint => transform.position + Vector3.up * Mathf.Max(0.55f, transform.localScale.y * 0.45f);

        public void Initialize(
            RunnerController runner,
            float healthValue,
            float speedValue,
            float damageValue,
            EnemyPool pool,
            EnemyRank enemyRank = EnemyRank.Grunt,
            int coins = 1,
            int score = 10,
            bool constrainToLane = false,
            float targetLaneX = 0f)
        {
            ReleaseBossBarrier();

            player = runner;
            playerHealth = runner != null ? runner.GetComponent<PlayerHealth>() : null;
            ownerPool = pool;
            rank = enemyRank;
            coinReward = Mathf.Max(0, coins);
            scoreReward = Mathf.Max(0, score);
            laneConstrained = constrainToLane;
            laneTargetX = targetLaneX;
            breachedPlayerLine = false;
            maxHealth = Mathf.Max(1f, healthValue);
            moveSpeed = Mathf.Max(0f, speedValue);
            contactDamage = Mathf.Max(0f, damageValue);
            health = maxHealth;
            attackTimer = Random.Range(0f, attackInterval * 0.5f);
            punch = 0f;
            baseScale = transform.localScale;
            logicTimer = 0f;
            cachedDistance = float.MaxValue;
            cachedDirection = Vector3.zero;
            RefreshSteering();

            if (rank == EnemyRank.Boss && GameManager.Instance != null)
            {
                ActiveBoss = this;
                bossBarrierOwnerId = GetInstanceID();
                float barrierOffset = Mathf.Max(6f, transform.localScale.z * 2.5f);
                GameManager.Instance.RegisterBossBarrier(bossBarrierOwnerId, transform.position.z - barrierOffset);
            }
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            health = maxHealth;
        }

        protected override void OnDisable()
        {
            if (ActiveBoss == this)
                ActiveBoss = null;

            ReleaseBossBarrier();
            base.OnDisable();
        }

        private void Start()
        {
            if (player == null) player = FindAnyObjectByType<RunnerController>();
            if (player != null && playerHealth == null) playerHealth = player.GetComponent<PlayerHealth>();
            if (health <= 0f) health = maxHealth;
            if (baseScale == Vector3.zero) baseScale = transform.localScale;
        }

        private void Update()
        {
            UpdateHitPunch();

            if (player == null || GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;

            if (laneConstrained)
            {
                UpdateLaneMovement();
                return;
            }

            logicTimer -= Time.deltaTime;
            if (logicTimer <= 0f)
                RefreshSteering();

            if (cachedDistance > attackDistance)
            {
                transform.position += cachedDirection * moveSpeed * Time.deltaTime;
            }
            else
            {
                AttackPlayer();
            }
        }

        private void UpdateLaneMovement()
        {
            float lineDistance = transform.position.z - player.transform.position.z;
            float stopDistance = rank == EnemyRank.Boss ? Mathf.Max(2.2f, attackDistance) : 0.45f;

            Vector3 position = transform.position;
            position.x = Mathf.MoveTowards(position.x, laneTargetX, moveSpeed * 0.65f * Time.deltaTime);

            if (lineDistance > stopDistance)
            {
                position.z -= moveSpeed * Time.deltaTime;
                transform.position = position;
                transform.forward = Vector3.back;
                return;
            }

            transform.position = position;

            if (rank == EnemyRank.Boss)
            {
                AttackPlayer();
                return;
            }

            BreachPlayerLine();
        }

        private void AttackPlayer()
        {
            attackTimer -= Time.deltaTime;
            if (attackTimer > 0f) return;

            playerHealth?.Damage(contactDamage);
            attackTimer = attackInterval;
        }

        private void BreachPlayerLine()
        {
            if (breachedPlayerLine) return;
            breachedPlayerLine = true;

            GameManager.Instance?.RegisterEnemyBreach();
            playerHealth?.Damage(contactDamage);
            RunnerCamera.Instance?.Shake(rank == EnemyRank.Elite ? 0.16f : 0.08f, 0.10f);
            CombatFxPool.Instance?.PlayDeath(
                transform.position + Vector3.up * Mathf.Max(0.55f, baseScale.y * 0.45f),
                Mathf.Max(0.35f, baseScale.magnitude * 0.24f),
                rank);

            Die();
        }

        private void RefreshSteering()
        {
            if (laneConstrained)
            {
                cachedDistance = Mathf.Max(0f, transform.position.z - player.transform.position.z);
                cachedDirection = Vector3.back;
                logicTimer = nearLogicInterval;
                return;
            }

            Vector3 toPlayer = player.transform.position - transform.position;
            Vector3 planar = new Vector3(toPlayer.x, 0f, toPlayer.z);
            cachedDistance = planar.magnitude;
            if (planar.sqrMagnitude > 0.001f)
            {
                cachedDirection = planar / Mathf.Max(0.001f, cachedDistance);
                transform.forward = cachedDirection;
            }

            logicTimer = cachedDistance > farDistance ? farLogicInterval : nearLogicInterval;
        }

        private void UpdateHitPunch()
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
        }

        public override void TakeDamage(float amount)
        {
            if (health <= 0f) return;

            health -= Mathf.Max(0f, amount);
            punch = hitPunch;
            if (health > 0f) return;

            GameManager.Instance?.RegisterEnemyKill(rank, coinReward, scoreReward);

            float rankScale = rank == EnemyRank.Boss ? 2f : rank == EnemyRank.Elite ? 1.35f : 1f;
            CombatFxPool.Instance?.PlayDeath(
                transform.position + Vector3.up * Mathf.Max(0.55f, baseScale.y * 0.45f),
                Mathf.Max(0.45f, baseScale.magnitude * 0.38f) * rankScale,
                rank);

            if (rank == EnemyRank.Boss)
                RunnerCamera.Instance?.Shake(0.32f, 0.28f);
            else if (rank == EnemyRank.Elite)
                RunnerCamera.Instance?.Shake(0.12f, 0.10f);

            Die();
        }

        private void Die()
        {
            ReleaseBossBarrier();
            transform.localScale = baseScale;

            if (ownerPool != null)
            {
                ownerPool.Release(this);
                return;
            }

            Destroy(gameObject);
        }

        private void ReleaseBossBarrier()
        {
            if (bossBarrierOwnerId == 0) return;

            GameManager.Instance?.ReleaseBossBarrier(bossBarrierOwnerId);
            bossBarrierOwnerId = 0;
        }
    }
}
