using UnityEngine;
using HORDAX.CameraSystem;
using HORDAX.Combat;
using HORDAX.Player;

namespace HORDAX.World
{
    public sealed class HazardZone : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float damage = 25f;
        private bool consumed;

        public void Configure(float amount)
        {
            damage = Mathf.Max(0f, amount);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (consumed) return;

            PlayerHealth health = other.GetComponent<PlayerHealth>();
            if (health == null) return;

            consumed = true;
            health.Damage(damage);
            CombatFxPool.Instance?.PlayDeath(transform.position + Vector3.up * 0.2f, 0.7f);
            RunnerCamera.Instance?.Shake(0.16f, 0.12f);
            gameObject.SetActive(false);
        }
    }
}
