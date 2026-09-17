using UnityEngine;
using HORDAX.CameraSystem;
using HORDAX.Combat;

namespace HORDAX.World
{
    public sealed class UpgradePickup : MonoBehaviour
    {
        [SerializeField] private float damageAdd = 3f;
        [SerializeField] private float fireRateMultiplier = 1.12f;
        [SerializeField] private float rotationSpeed = 90f;
        [SerializeField] private float bobHeight = 0.18f;
        [SerializeField] private float bobSpeed = 3.5f;

        private float baseY;
        private float phase;

        public void Configure(float extraDamage, float cadenceMultiplier)
        {
            damageAdd = extraDamage;
            fireRateMultiplier = cadenceMultiplier;
        }

        private void Start()
        {
            baseY = transform.position.y;
            phase = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.World);
            Vector3 position = transform.position;
            position.y = baseY + Mathf.Sin(Time.time * bobSpeed + phase) * bobHeight;
            transform.position = position;
        }

        private void OnTriggerEnter(Collider other)
        {
            WeaponController weapon = other.GetComponent<WeaponController>();
            if (weapon == null) return;

            weapon.ApplyUpgrade(damageAdd, fireRateMultiplier);
            RunnerCamera.Instance?.Shake(0.12f, 0.12f);
            gameObject.SetActive(false);
        }
    }
}
