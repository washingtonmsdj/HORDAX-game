using UnityEngine;

namespace HORDAX.Data
{
    [CreateAssetMenu(fileName = "WeaponData", menuName = "HORDAX/Weapon Data")]
    public sealed class WeaponData : ScriptableObject
    {
        [SerializeField] private string weaponId = "rifle_basic";
        [SerializeField] private string displayName = "Basic Rifle";
        [SerializeField] private float damage = 5f;
        [SerializeField] private float fireRate = 12f;
        [SerializeField] private float range = 34f;
        [SerializeField] private float bulletSpeed = 45f;
        [SerializeField] private GameObject visualPrefab;
        [SerializeField] private GameObject bulletPrefab;

        public string WeaponId => weaponId;
        public string DisplayName => displayName;
        public float Damage => damage;
        public float FireRate => fireRate;
        public float Range => range;
        public float BulletSpeed => bulletSpeed;
        public GameObject VisualPrefab => visualPrefab;
        public GameObject BulletPrefab => bulletPrefab;
    }
}
