using UnityEngine;

namespace HORDAX.Data
{
    [CreateAssetMenu(fileName = "WeaponUnlock", menuName = "HORDAX/Weapon Unlock")]
    public sealed class WeaponUnlockDefinition : ScriptableObject
    {
        [SerializeField] private string weaponId = "rifle";
        [SerializeField] private string displayName = "Rifle";
        [SerializeField] private WeaponArchetype prototypeWeapon = WeaponArchetype.Rifle;
        [SerializeField, Min(0)] private int unlockCost;
        [SerializeField] private WeaponData weaponData;

        public string WeaponId => weaponId;
        public string DisplayName => displayName;
        public WeaponArchetype PrototypeWeapon => prototypeWeapon;
        public int UnlockCost => unlockCost;
        public WeaponData WeaponData => weaponData;

        public void ConfigureRuntime(
            string id,
            string label,
            WeaponArchetype archetype,
            int cost,
            WeaponData data = null)
        {
            weaponId = id;
            displayName = label;
            prototypeWeapon = archetype;
            unlockCost = Mathf.Max(0, cost);
            weaponData = data;
        }
    }
}
