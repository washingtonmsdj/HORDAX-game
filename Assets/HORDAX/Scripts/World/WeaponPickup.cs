using UnityEngine;
using HORDAX.CameraSystem;
using HORDAX.Combat;
using HORDAX.Data;

namespace HORDAX.World
{
    public sealed class WeaponPickup : MonoBehaviour
    {
        [SerializeField] private WeaponData weaponData;
        [SerializeField] private WeaponArchetype prototypeWeapon = WeaponArchetype.SMG;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private float rotationSpeed = 80f;
        [SerializeField] private float bobHeight = 0.16f;
        [SerializeField] private float bobSpeed = 2.4f;

        private float baseY;

        public void Configure(WeaponData definition, WeaponArchetype fallback, Transform visual = null)
        {
            weaponData = definition;
            prototypeWeapon = fallback;
            visualRoot = visual;
        }

        private void Start()
        {
            baseY = transform.position.y;
        }

        private void Update()
        {
            if (visualRoot != null)
                visualRoot.Rotate(0f, rotationSpeed * Time.deltaTime, 0f, Space.World);

            Vector3 position = transform.position;
            position.y = baseY + Mathf.Sin(Time.time * bobSpeed) * bobHeight;
            transform.position = position;
        }

        private void OnTriggerEnter(Collider other)
        {
            WeaponController weapon = other.GetComponent<WeaponController>();
            if (weapon == null) return;

            if (weaponData != null)
                weapon.ApplyDefinition(weaponData);
            else
                weapon.ApplyPrototype(prototypeWeapon);

            CombatFxPool.Instance?.PlayGateBreak(transform.position, 0.65f);
            RunnerCamera.Instance?.Shake(0.10f, 0.10f);
            gameObject.SetActive(false);
        }
    }
}
