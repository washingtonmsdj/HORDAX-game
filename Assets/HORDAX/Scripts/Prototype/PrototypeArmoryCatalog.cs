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
                Create("rifle", "RIFLE", WeaponArchetype.Rifle, 0),
                Create("smg", "SMG", WeaponArchetype.SMG, 300),
                Create("shotgun", "SHOTGUN", WeaponArchetype.Shotgun, 650),
                Create("minigun", "MINIGUN", WeaponArchetype.Minigun, 1200)
            };
        }

        private static WeaponUnlockDefinition Create(string id, string label, WeaponArchetype type, int cost)
        {
            WeaponUnlockDefinition definition = ScriptableObject.CreateInstance<WeaponUnlockDefinition>();
            definition.name = "Runtime Armory - " + label;
            definition.ConfigureRuntime(id, label, type, cost);
            return definition;
        }
    }
}
