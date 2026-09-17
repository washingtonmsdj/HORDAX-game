using UnityEngine;

namespace HORDAX.Data
{
    [CreateAssetMenu(fileName = "PermanentUpgrade", menuName = "HORDAX/Permanent Upgrade")]
    public sealed class PermanentUpgradeDefinition : ScriptableObject
    {
        [SerializeField] private string upgradeId = "health";
        [SerializeField] private string displayName = "Vitality";
        [SerializeField] private PermanentUpgradeType type = PermanentUpgradeType.MaxHealth;
        [SerializeField, Min(1)] private int maxLevel = 10;
        [SerializeField, Min(0)] private int baseCost = 100;
        [SerializeField, Min(1f)] private float costGrowth = 1.45f;
        [SerializeField, Min(0f)] private float valuePerLevel = 0.10f;

        public string UpgradeId => upgradeId;
        public string DisplayName => displayName;
        public PermanentUpgradeType Type => type;
        public int MaxLevel => maxLevel;
        public float ValuePerLevel => valuePerLevel;

        public int GetCost(int currentLevel)
        {
            if (currentLevel >= maxLevel) return 0;
            return Mathf.Max(0, Mathf.RoundToInt(baseCost * Mathf.Pow(costGrowth, Mathf.Max(0, currentLevel))));
        }

        public float GetMultiplier(int level)
        {
            return 1f + Mathf.Max(0, level) * valuePerLevel;
        }

        public void ConfigureRuntime(
            string id,
            string label,
            PermanentUpgradeType upgradeType,
            int levels,
            int initialCost,
            float growth,
            float perLevel)
        {
            upgradeId = id;
            displayName = label;
            type = upgradeType;
            maxLevel = Mathf.Max(1, levels);
            baseCost = Mathf.Max(0, initialCost);
            costGrowth = Mathf.Max(1f, growth);
            valuePerLevel = Mathf.Max(0f, perLevel);
        }
    }
}
