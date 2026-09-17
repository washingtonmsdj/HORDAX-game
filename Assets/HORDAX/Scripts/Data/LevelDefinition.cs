using System;
using System.Collections.Generic;
using UnityEngine;

namespace HORDAX.Data
{
    public enum LevelStepType
    {
        Horde,
        Elite,
        Boss,
        Gate,
        Upgrade,
        Weapon,
        Heal,
        Reward,
        Hazard,
        Finish
    }

    public enum LevelObjectiveType
    {
        KillEnemies,
        KillElites,
        CollectRunCoins,
        ReachScore,
        FinishWithHealthPercent
    }

    [Serializable]
    public sealed class LevelObjective
    {
        public string label = "OBJECTIVE";
        public LevelObjectiveType type = LevelObjectiveType.KillEnemies;
        [Min(1f)] public float target = 10f;
        [Min(0)] public int bonusCoins = 20;
    }

    [Serializable]
    public sealed class LevelStep
    {
        public LevelStepType type = LevelStepType.Horde;
        public string label = "Step";
        [Min(0f)] public float z = 20f;

        [Header("Encounter")]
        [Min(1)] public int enemyCount = 20;
        [Min(1)] public int columns = 6;
        [Min(1f)] public float enemyHealth = 5f;
        [Min(0f)] public float enemySpeed = 3.2f;
        [Min(0f)] public float enemyDamage = 8f;
        public EnemyRank enemyRank = EnemyRank.Grunt;
        [Min(0)] public int coinReward = 1;
        [Min(0)] public int scoreReward = 10;
        [Min(0.1f)] public float scaleMultiplier = 1f;
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

        [Header("Track Event")]
        [Range(-5.2f, 5.2f)] public float laneX;
        [Min(0f)] public float healAmount = 25f;
        [Min(0)] public int eventCoins = 20;
        [Min(0)] public int eventScore = 100;
        [Min(0f)] public float hazardDamage = 25f;
        [Min(0.5f)] public float eventWidth = 2.4f;
        public string eventLabel = "EVENT";
    }

    [CreateAssetMenu(fileName = "LevelDefinition", menuName = "HORDAX/Level Definition")]
    public sealed class LevelDefinition : ScriptableObject
    {
        [SerializeField] private string levelId = "level_001";
        [SerializeField] private string displayName = "Level 1";
        [SerializeField, Min(20f)] private float length = 185f;

        [Header("Completion rewards")]
        [SerializeField, Min(0)] private int completionCoins = 75;
        [SerializeField, Min(0)] private int completionScore = 500;

        [Header("Rating")]
        [SerializeField, Min(0)] private int twoStarScore = 1400;
        [SerializeField, Min(0)] private int threeStarScore = 2200;

        [Header("Optional objectives")]
        [SerializeField] private List<LevelObjective> objectives = new List<LevelObjective>();

        [Header("Sequence")]
        [SerializeField] private List<LevelStep> steps = new List<LevelStep>();

        public string LevelId => levelId;
        public string DisplayName => displayName;
        public float Length => length;
        public int CompletionCoins => completionCoins;
        public int CompletionScore => completionScore;
        public int TwoStarScore => twoStarScore;
        public int ThreeStarScore => threeStarScore;
        public IReadOnlyList<LevelObjective> Objectives => objectives ?? (objectives = new List<LevelObjective>());
        public IReadOnlyList<LevelStep> Steps => steps ?? (steps = new List<LevelStep>());

        public void ConfigureRuntime(
            string id,
            string label,
            float levelLength,
            int coins,
            int score,
            IEnumerable<LevelStep> sequence,
            int scoreForTwoStars = 0,
            int scoreForThreeStars = 0,
            IEnumerable<LevelObjective> optionalObjectives = null)
        {
            levelId = id;
            displayName = label;
            length = Mathf.Max(20f, levelLength);
            completionCoins = Mathf.Max(0, coins);
            completionScore = Mathf.Max(0, score);
            twoStarScore = Mathf.Max(completionScore, scoreForTwoStars > 0 ? scoreForTwoStars : completionScore * 2);
            threeStarScore = Mathf.Max(twoStarScore, scoreForThreeStars > 0 ? scoreForThreeStars : completionScore * 3);
            objectives = optionalObjectives != null ? new List<LevelObjective>(optionalObjectives) : new List<LevelObjective>();
            steps = sequence != null ? new List<LevelStep>(sequence) : new List<LevelStep>();
        }
    }
}
