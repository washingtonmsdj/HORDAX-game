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
                if (data.MoveSpeed > 12f) warnings.Add($"Enemy '{name}' move speed is unusually high ({data.MoveSpeed:0.##}).");
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
                            if (step.enemyCount < 1) errors.Add($"Level '{name}' horde '{step.label}' has no enemies.");
                            if (step.columns < 1) errors.Add($"Level '{name}' horde '{step.label}' has invalid column count.");
                            if (step.enemyCount > 150) warnings.Add($"Level '{name}' horde '{step.label}' spawns {step.enemyCount} enemies; profile this on low-end mobile.");
                            break;
                        case LevelStepType.Gate:
                            if (step.gateHitPoints <= 0f) errors.Add($"Level '{name}' gate '{step.label}' has HP <= 0.");
                            break;
                        case LevelStepType.Upgrade:
                            if (step.fireRateMultiplier <= 0f) errors.Add($"Level '{name}' upgrade '{step.label}' has invalid fire-rate multiplier.");
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
    }
}
#endif
