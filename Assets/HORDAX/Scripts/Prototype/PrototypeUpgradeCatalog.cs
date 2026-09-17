using System.Collections.Generic;
using HORDAX.Data;
using UnityEngine;

namespace HORDAX.Prototype
{
    public static class PrototypeUpgradeCatalog
    {
        private static List<PermanentUpgradeDefinition> upgrades;

        public static IReadOnlyList<PermanentUpgradeDefinition> All
        {
            get
            {
                Ensure();
                return upgrades;
            }
        }

        public static PermanentUpgradeDefinition Get(PermanentUpgradeType type)
        {
            Ensure();
            for (int i = 0; i < upgrades.Count; i++)
                if (upgrades[i].Type == type) return upgrades[i];

            return null;
        }

        private static void Ensure()
        {
            if (upgrades != null) return;

            upgrades = new List<PermanentUpgradeDefinition>
            {
                Create("health", "VITALITY", PermanentUpgradeType.MaxHealth, 10, 90, 1.45f, 0.10f),
                Create("damage", "POWER", PermanentUpgradeType.WeaponDamage, 10, 120, 1.48f, 0.08f),
                Create("fire_rate", "FIRE RATE", PermanentUpgradeType.FireRate, 10, 140, 1.50f, 0.06f)
            };
        }

        private static PermanentUpgradeDefinition Create(
            string id,
            string label,
            PermanentUpgradeType type,
            int maxLevel,
            int baseCost,
            float costGrowth,
            float valuePerLevel)
        {
            PermanentUpgradeDefinition definition = ScriptableObject.CreateInstance<PermanentUpgradeDefinition>();
            definition.name = "Runtime Upgrade - " + label;
            definition.ConfigureRuntime(id, label, type, maxLevel, baseCost, costGrowth, valuePerLevel);
            return definition;
        }
    }
}
