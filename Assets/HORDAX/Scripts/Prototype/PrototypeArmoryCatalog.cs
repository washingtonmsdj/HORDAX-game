using System.Collections.Generic;
using HORDAX.Data;
using UnityEngine;

namespace HORDAX.Prototype
{
    public static class PrototypeArmoryCatalog
    {
        private static List<WeaponUnlockDefinition> weapons;

        public static IReadOnlyList<WeaponUnlockDefinition> All
        {
            get
            {
                Ensure();
                return weapons;
            }
        }

        public static WeaponUnlockDefinition GetById(string id)
        {
            Ensure();
            for (int i = 0; i < weapons.Count; i++)
            {
                WeaponUnlockDefinition definition = weapons[i];
                if (definition != null && definition.WeaponId == id)
                    return definition;
            }

            return weapons.Count > 0 ? weapons[0] : null;
        }

        private static void Ensure()
        {
            if (weapons != null) return;

            weapons = new List<WeaponUnlockDefinition>
            {
                Create("rifle", "RIFLE", WeaponArchetype.Rifle, 0, 5f, 12f, 46f, 60f, 1, 0.4f, 0.055f, 1f),
                Create("smg", "SMG", WeaponArchetype.SMG, 300, 4f, 18f, 44f, 70f, 1, 1.2f, 0.035f, 0.85f),
                Create("shotgun", "SHOTGUN", WeaponArchetype.Shotgun, 650, 3.5f, 4.2f, 36f, 58f, 5, 5.5f, 0.13f, 1.05f),
                Create("minigun", "MINIGUN", WeaponArchetype.Minigun, 1200, 4.5f, 24f, 52f, 78f, 1, 1.5f, 0.045f, 0.8f)
            };
        }

        private static WeaponUnlockDefinition Create(
            string id,
            string label,
            WeaponArchetype type,
            int cost,
            float damage,
            float fireRate,
            float range,
            float bulletSpeed,
            int projectilesPerShot,
            float spreadDegrees,
            float recoilKick,
            float projectileScale)
        {
            WeaponData data = ScriptableObject.CreateInstance<WeaponData>();
            data.name = "Runtime Weapon - " + label;
            data.ConfigureRuntime(
                id,
                label,
                type,
                WeaponRarity.Common,
                damage,
                fireRate,
                range,
                bulletSpeed,
                projectilesPerShot,
                spreadDegrees,
                recoilKick,
                projectileScale);

            WeaponUnlockDefinition definition = ScriptableObject.CreateInstance<WeaponUnlockDefinition>();
            definition.name = "Runtime Armory - " + label;
            definition.ConfigureRuntime(id, label, type, cost, data);
            return definition;
        }
    }
}
