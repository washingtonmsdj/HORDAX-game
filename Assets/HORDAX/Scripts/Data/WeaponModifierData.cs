using UnityEngine;

namespace HORDAX.Data
{
    [CreateAssetMenu(fileName = "WeaponModifier", menuName = "HORDAX/Weapon Modifier")]
    public sealed class WeaponModifierData : ScriptableObject
    {
        [SerializeField] private string modifierId = "power";
        [SerializeField] private string displayName = "Power";

        [Header("Stat changes")]
        [SerializeField] private float damageAdd;
        [SerializeField, Min(0.01f)] private float damageMultiplier = 1f;
        [SerializeField, Min(0.01f)] private float fireRateMultiplier = 1f;
        [SerializeField, Min(0.01f)] private float rangeMultiplier = 1f;
        [SerializeField] private int bonusProjectiles;
        [SerializeField, Min(0.01f)] private float spreadMultiplier = 1f;

        public string ModifierId => modifierId;
        public string DisplayName => displayName;
        public float DamageAdd => damageAdd;
        public float DamageMultiplier => damageMultiplier;
        public float FireRateMultiplier => fireRateMultiplier;
        public float RangeMultiplier => rangeMultiplier;
        public int BonusProjectiles => bonusProjectiles;
        public float SpreadMultiplier => spreadMultiplier;
    }
}
