using System;
using System.Collections.Generic;
using UnityEngine;

namespace HORDAX.Data
{
    public enum LevelStepType
    {
        Horde,
        Gate,
        Upgrade,
        Weapon,
        Finish
    }

    [Serializable]
    public sealed class LevelStep
    {
        public LevelStepType type = LevelStepType.Horde;
        public string label = "Step";
        [Min(0f)] public float z = 20f;

        [Header("Horde")]
        [Min(1)] public int enemyCount = 20;
        [Min(1)] public int columns = 6;
        [Min(1f)] public float enemyHealth = 5f;
        [Min(0f)] public float enemySpeed = 3.2f;
        [Min(0f)] public float enemyDamage = 8f;
        public EnemyData enemyData;

        [Header("Gate")]
        [Min(1f)] public float gateHitPoints = 50f;

        [Header("Upgrade")]
        public float damageAdd = 3f;
        [Min(0.01f)] public float fireRateMultiplier = 1.12f;
        public string upgradeLabel = "+POWER";

        [Header("Weapon")]
        public WeaponData weaponData;
        public WeaponArchetype prototypeWeapon = WeaponArchetype.SMG;
        public string weaponLabel = "NEW WEAPON";
    }

    [CreateAssetMenu(fileName = "LevelDefinition", menuName = "HORDAX/Level Definition")]
    public sealed class LevelDefinition : ScriptableObject
    {
        [SerializeField] private string levelId = "level_001";
        [SerializeField] private string displayName = "Level 1";
        [SerializeField, Min(20f)] private float length = 185f;
        [SerializeField] private List<LevelStep> steps = new List<LevelStep>();

        public string LevelId => levelId;
        public string DisplayName => displayName;
        public float Length => length;
        public IReadOnlyList<LevelStep> Steps => steps;
    }
}
