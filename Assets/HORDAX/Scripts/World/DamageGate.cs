using UnityEngine;
using HORDAX.Combat;
using HORDAX.Player;

namespace HORDAX.World
{
    public sealed class DamageGate : ShootableTarget
    {
        [SerializeField] private float maxHealth = 50f;
        private float health;
        private TextMesh label;

        public override Vector3 TargetPoint => transform.position + Vector3.up * 1.3f;

        public void Initialize(float hitPoints, TextMesh valueLabel)
        {
            maxHealth = hitPoints;
            health = maxHealth;
            label = valueLabel;
            RefreshLabel();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            if (health <= 0f) health = maxHealth;
        }

        public override void TakeDamage(float amount)
        {
            if (health <= 0f) return;

            health = Mathf.Max(0f, health - Mathf.Max(0f, amount));
            RefreshLabel();

            if (health <= 0f)
                gameObject.SetActive(false);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (health <= 0f) return;
            PlayerHealth player = other.GetComponent<PlayerHealth>();
            if (player != null) player.Damage(9999f);
        }

        private void RefreshLabel()
        {
            if (label != null) label.text = Mathf.CeilToInt(health).ToString();
        }
    }
}
