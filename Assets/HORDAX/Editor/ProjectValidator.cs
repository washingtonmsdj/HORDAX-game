#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using HORDAX.Data;

namespace HORDAX.EditorTools
{
    public static class ProjectValidator
    {
        [MenuItem("HORDAX/Validate Project Data")]
        public static void ValidateProjectData()
        {
            List<string> errors = new List<string>();
            List<string> warnings = new List<string>();

            ValidateWeapons(errors, warnings);
            ValidateEnemies(errors, warnings);
            ValidateLevels(errors, warnings);
            ValidateCampaigns(errors, warnings);

            for (int i = 0; i < warnings.Count; i++)
                Debug.LogWarning("[HORDAX] " + warnings[i]);

            for (int i = 0; i < errors.Count; i++)
                Debug.LogError("[HORDAX] " + errors[i]);

            if (errors.Count == 0)
                Debug.Log($"[HORDAX] Validation complete: no blocking errors. {warnings.Count} warning(s).");
            else
                Debug.LogError($"[HORDAX] Validation failed with {errors.Count} error(s) and {warnings.Count} warning(s).");
        }

        private static void ValidateWeapons(List<string> errors, List<string> warnings)
        {
            string[] guids = AssetDatabase.FindAssets("t:WeaponData");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                WeaponData data = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
                if (data == null) continue;

                string name = string.IsNullOrWhiteSpace(data.DisplayName) ? path : data.DisplayName;
                if (data.Damage <= 0f) errors.Add($"Weapon '{name}' has damage <= 0.");
                if (data.FireRate <= 0f) errors.Add($"Weapon '{name}' has fire rate <= 0.");
                if (data.Range <= 0f) errors.Add($"Weapon '{name}' has range <= 0.");
                if (data.BulletSpeed <= 0f) errors.Add($"Weapon '{name}' has bullet speed <= 0.");
                if (data.ProjectilesPerShot < 1) errors.Add($"Weapon '{name}' must fire at least one projectile.");
                if (data.ProjectilesPerShot > 12) warnings.Add($"Weapon '{name}' fires {data.ProjectilesPerShot} projectiles per shot; profile this on mobile.");
                if (data.FireRate * data.ProjectilesPerShot > 80f) warnings.Add($"Weapon '{name}' can emit more than 80 projectiles/sec; verify pool pressure on device.");

                for (int modifierIndex = 0; modifierIndex < data.Modifiers.Count; modifierIndex++)
                {
                    WeaponModifierData modifier = data.Modifiers[modifierIndex];
                    if (modifier == null)
                        warnings.Add($"Weapon '{name}' contains a null modifier at index {modifierIndex}.");
                }
            }
        }

        private static void ValidateEnemies(List<string> errors, List<string> warnings)
        {
            string[] guids = AssetDatabase.FindAssets("t:EnemyData");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                EnemyData data = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
                if (data == null) continue;

                string name = string.IsNullOrWhiteSpace(data.DisplayName) ? path : data.DisplayName;
                if (data.Health <= 0f) errors.Add($"Enemy '{name}' has health <= 0.");
                if (data.MoveSpeed < 0f) errors.Add($"Enemy '{name}' has negative move speed.");
                if (data.ContactDamage < 0f) errors.Add($"Enemy '{name}' has negative contact damage.");
                if (data.ScaleMultiplier <= 0f) errors.Add($"Enemy '{name}' has invalid scale multiplier.");
                if (data.CoinReward < 0 || data.ScoreReward < 0) errors.Add($"Enemy '{name}' has a negative reward.");
                if (data.MoveSpeed > 12f) warnings.Add($"Enemy '{name}' move speed is unusually high ({data.MoveSpeed:0.##}).");
                if (data.Rank == EnemyRank.Boss && data.Health < 100f) warnings.Add($"Boss '{name}' has less than 100 HP; confirm this is intentional.");
                if (data.Rank == EnemyRank.Boss && data.ScaleMultiplier < 1.5f) warnings.Add($"Boss '{name}' is close to grunt scale; visual readability may be weak.");
            }
        }

        private static void ValidateLevels(List<string> errors, List<string> warnings)
        {
            string[] guids = AssetDatabase.FindAssets("t:LevelDefinition");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                LevelDefinition level = AssetDatabase.LoadAssetAtPath<LevelDefinition>(path);
                if (level == null) continue;

                string name = string.IsNullOrWhiteSpace(level.DisplayName) ? path : level.DisplayName;
                float previousZ = -1f;
                int finishCount = 0;

                if (level.CompletionCoins < 0 || level.CompletionScore < 0)
                    errors.Add($"Level '{name}' has negative completion rewards.");
                if (level.TwoStarScore < level.CompletionScore)
                    warnings.Add($"Level '{name}' two-star threshold is below completion score.");
                if (level.ThreeStarScore < level.TwoStarScore)
                    errors.Add($"Level '{name}' three-star threshold is below the two-star threshold.");

                for (int objectiveIndex = 0; objectiveIndex < level.Objectives.Count; objectiveIndex++)
                {
                    LevelObjective objective = level.Objectives[objectiveIndex];
                    if (objective == null)
                    {
                        warnings.Add($"Level '{name}' contains a null objective at index {objectiveIndex}.");
                        continue;
                    }

                    if (objective.target <= 0f)
                        errors.Add($"Level '{name}' objective '{objective.label}' has target <= 0.");

                    if (objective.bonusCoins < 0)
                        errors.Add($"Level '{name}' objective '{objective.label}' has negative bonus coins.");

                    if (objective.type == LevelObjectiveType.FinishWithHealthPercent && objective.target > 100f)
                        warnings.Add($"Level '{name}' health objective '{objective.label}' is above 100%.");
                }

                for (int stepIndex = 0; stepIndex < level.Steps.Count; stepIndex++)
                {
                    LevelStep step = level.Steps[stepIndex];
                    if (step == null)
                    {
                        warnings.Add($"Level '{name}' contains a null step at index {stepIndex}.");
                        continue;
                    }

                    if (step.z < previousZ)
                        warnings.Add($"Level '{name}' step {stepIndex} is behind the previous step ({step.z:0.#} < {previousZ:0.#}).");
                    previousZ = step.z;

                    if (step.z > level.Length + 0.1f)
                        errors.Add($"Level '{name}' step '{step.label}' is beyond level length ({step.z:0.#} > {level.Length:0.#}).");

                    switch (step.type)
                    {
                        case LevelStepType.Horde:
                            ValidateEncounter(level, step, name, errors, warnings, false, false);
                            break;

                        case LevelStepType.Elite:
                            ValidateEncounter(level, step, name, errors, warnings, true, false);
                            if (step.enemyCount > 8)
                                warnings.Add($"Level '{name}' elite step '{step.label}' has {step.enemyCount} units; bootstrap clamps fallback elites to 8.");
                            break;

                        case LevelStepType.Boss:
                            ValidateEncounter(level, step, name, errors, warnings, false, true);
                            if (step.enemyCount != 1)
                                warnings.Add($"Level '{name}' boss step '{step.label}' ignores enemyCount and spawns one boss.");
                            break;

                        case LevelStepType.Gate:
                            if (step.gateHitPoints <= 0f) errors.Add($"Level '{name}' gate '{step.label}' has HP <= 0.");
                            break;

                        case LevelStepType.Upgrade:
                            if (step.fireRateMultiplier <= 0f) errors.Add($"Level '{name}' upgrade '{step.label}' has invalid fire-rate multiplier.");
                            break;

                        case LevelStepType.Heal:
                            if (step.healAmount <= 0f) errors.Add($"Level '{name}' heal '{step.label}' has amount <= 0.");
                            break;

                        case LevelStepType.Reward:
                            if (step.eventCoins <= 0 && step.eventScore <= 0)
                                warnings.Add($"Level '{name}' reward '{step.label}' grants neither coins nor score.");
                            break;

                        case LevelStepType.Hazard:
                            if (step.hazardDamage <= 0f) errors.Add($"Level '{name}' hazard '{step.label}' has damage <= 0.");
                            if (step.eventWidth <= 0f) errors.Add($"Level '{name}' hazard '{step.label}' has invalid width.");
                            break;

                        case LevelStepType.Finish:
                            finishCount++;
                            break;
                    }
                }

                if (finishCount > 1)
                    warnings.Add($"Level '{name}' contains {finishCount} finish steps. Usually only one is intended.");
            }
        }

        private static void ValidateEncounter(
            LevelDefinition level,
            LevelStep step,
            string levelName,
            List<string> errors,
            List<string> warnings,
            bool elite,
            bool boss)
        {
            if (step.enemyCount < 1) errors.Add($"Level '{levelName}' encounter '{step.label}' has no enemies.");
            if (step.columns < 1) errors.Add($"Level '{levelName}' encounter '{step.label}' has invalid column count.");
            if (step.enemyHealth <= 0f && step.enemyData == null) errors.Add($"Level '{levelName}' encounter '{step.label}' has HP <= 0.");
            if (step.scaleMultiplier <= 0f && step.enemyData == null) errors.Add($"Level '{levelName}' encounter '{step.label}' has invalid scale.");
            if (step.coinReward < 0 || step.scoreReward < 0) errors.Add($"Level '{levelName}' encounter '{step.label}' has negative rewards.");

            if (!elite && !boss && step.enemyCount > 150)
                warnings.Add($"Level '{levelName}' horde '{step.label}' spawns {step.enemyCount} enemies; profile this on low-end mobile.");

            if (boss && step.enemyData == null && step.enemyHealth < 100f)
                warnings.Add($"Level '{levelName}' boss '{step.label}' has low fallback HP; runtime raises the prototype minimum.");
        }

        private static void ValidateCampaigns(List<string> errors, List<string> warnings)
        {
            string[] guids = AssetDatabase.FindAssets("t:CampaignDefinition");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                CampaignDefinition campaign = AssetDatabase.LoadAssetAtPath<CampaignDefinition>(path);
                if (campaign == null) continue;

                string name = string.IsNullOrWhiteSpace(campaign.DisplayName) ? path : campaign.DisplayName;
                HashSet<string> levelIds = new HashSet<string>();

                for (int levelIndex = 0; levelIndex < campaign.Levels.Count; levelIndex++)
                {
                    LevelDefinition level = campaign.Levels[levelIndex];
                    if (level == null)
                    {
                        warnings.Add($"Campaign '{name}' contains a null level at index {levelIndex}.");
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(level.LevelId) && !levelIds.Add(level.LevelId))
                        errors.Add($"Campaign '{name}' contains duplicate level id '{level.LevelId}'.");
                }
            }
        }
    }
}
#endif
