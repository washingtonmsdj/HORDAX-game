using System.Collections.Generic;
using UnityEngine;

namespace HORDAX.Data
{
    [CreateAssetMenu(fileName = "WeaponData", menuName = "HORDAX/Weapon Data")]
    public sealed class WeaponData : ScriptableObject
    {
        [SerializeField] private string weaponId = "rifle_basic";
        [SerializeField] private string displayName = "Basic Rifle";
        [SerializeField] private WeaponArchetype archetype = WeaponArchetype.Rifle;
        [SerializeField] private WeaponRarity rarity = WeaponRarity.Common;

        [Header("Combat")]
        [SerializeField, Min(0.1f)] private float damage = 5f;
        [SerializeField, Min(0.1f)] private float fireRate = 12f;
        [SerializeField, Min(1f)] private float range = 34f;
        [SerializeField, Min(1f)] private float bulletSpeed = 45f;
        [SerializeField, Min(1)] private int projectilesPerShot = 1;
        [SerializeField, Min(0f)] private float spreadDegrees = 0.4f;
        [SerializeField, Min(0f)] private float recoilKick = 0.05f;
        [SerializeField, Min(0.1f)] private float projectileScale = 1f;

        [Header("Modifiers")]
        [SerializeField] private List<WeaponModifierData> modifiers = new List<WeaponModifierData>();

        [Header("Presentation")]
        [SerializeField] private GameObject visualPrefab;
        [SerializeField] private GameObject bulletPrefab;

        public string WeaponId => weaponId;
        public string DisplayName => displayName;
        public WeaponArchetype Archetype => archetype;
        public WeaponRarity Rarity => rarity;
        public float Damage => damage;
        public float FireRate => fireRate;
        public float Range => range;
        public float BulletSpeed => bulletSpeed;
        public int ProjectilesPerShot => projectilesPerShot;
        public float SpreadDegrees => spreadDegrees;
        public float RecoilKick => recoilKick;
        public float ProjectileScale => projectileScale;
        public IReadOnlyList<WeaponModifierData> Modifiers => modifiers ?? (modifiers = new List<WeaponModifierData>());
        public GameObject VisualPrefab => visualPrefab;
        public GameObject BulletPrefab => bulletPrefab;
    }
}
