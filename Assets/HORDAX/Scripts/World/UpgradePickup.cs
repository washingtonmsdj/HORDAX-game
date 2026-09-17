using UnityEngine;
using HORDAX.Combat;

namespace HORDAX.World
{
    public sealed class UpgradePickup : MonoBehaviour
    {
        [SerializeField] private float damageAdd = 3f;
        [SerializeField] private float fireRateMultiplier = 1.12f;
        [SerializeField] private float rotationSpeed = 90f;

        public void Configure(float extraDamage, float cadenceMultiplier)
        {
            damageAdd = extraDamage;
            fireRateMultiplier = cadenceMultiplier;
        }

        private void Update()
        {
            transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.World);
        }

        private void OnTriggerEnter(Collider other)
        {
            WeaponController weapon = other.GetComponent<WeaponController>();
            if (weapon == null) return;

            weapon.ApplyUpgrade(damageAdd, fireRateMultiplier);
            gameObject.SetActive(false);
        }
    }
}
