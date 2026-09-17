using UnityEngine;
using HORDAX.CameraSystem;
using HORDAX.Combat;
using HORDAX.Player;

namespace HORDAX.World
{
    public sealed class HealPickup : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float healAmount = 25f;

        public void Configure(float amount)
        {
            healAmount = Mathf.Max(0f, amount);
        }

        private void OnTriggerEnter(Collider other)
        {
            PlayerHealth health = other.GetComponent<PlayerHealth>();
            if (health == null) return;

            health.Heal(healAmount);
            CombatFxPool.Instance?.PlayGateBreak(transform.position, 0.45f);
            RunnerCamera.Instance?.Shake(0.04f, 0.05f);
            gameObject.SetActive(false);
        }
    }
}
