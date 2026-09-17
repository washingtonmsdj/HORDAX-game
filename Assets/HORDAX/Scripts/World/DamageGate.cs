using UnityEngine;
using HORDAX.CameraSystem;
using HORDAX.Combat;
using HORDAX.Player;

namespace HORDAX.World
{
    public sealed class DamageGate : ShootableTarget
    {
        [SerializeField] private float maxHealth = 50f;
        [SerializeField] private float hitPunch = 0.04f;

        private float health;
        private TextMesh label;
        private Vector3 baseScale;
        private float punch;

        public override Vector3 TargetPoint => transform.position + Vector3.up * 1.3f;

        public void Initialize(float hitPoints, TextMesh valueLabel)
        {
            maxHealth = hitPoints;
            health = maxHealth;
            label = valueLabel;
            baseScale = transform.localScale;
            RefreshLabel();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (health <= 0f) health = maxHealth;
        }

        private void Update()
        {
            if (punch <= 0f) return;
            punch = Mathf.MoveTowards(punch, 0f, 4f * Time.deltaTime);
            transform.localScale = baseScale * (1f + punch);
        }

        public override void TakeDamage(float amount)
        {
            if (health <= 0f) return;

            health = Mathf.Max(0f, health - Mathf.Max(0f, amount));
            punch = hitPunch;
            RefreshLabel();
            RunnerCamera.Instance?.Shake(0.025f, 0.035f);

            if (health <= 0f)
            {
                RunnerCamera.Instance?.Shake(0.22f, 0.16f);
                gameObject.SetActive(false);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (health <= 0f) return;
            PlayerHealth player = other.GetComponent<PlayerHealth>();
            if (player != null)
            {
                RunnerCamera.Instance?.Shake(0.4f, 0.25f);
                player.Damage(9999f);
            }
        }

        private void RefreshLabel()
        {
            if (label != null) label.text = Mathf.CeilToInt(health).ToString();
        }
    }
}
