using System.Collections.Generic;
using HORDAX.Data;
using UnityEngine;

namespace HORDAX.Prototype
{
    public static class PrototypeCampaignFactory
    {
        private static CampaignDefinition campaign;

        public static CampaignDefinition CreateCampaign()
        {
            if (campaign != null) return campaign;

            List<LevelDefinition> levels = new List<LevelDefinition>
            {
                MakeLevel("level_001", "DISTRICT 01", 220f, 80, 800, 1),
                MakeLevel("level_002", "DISTRICT 02", 250f, 105, 1100, 2),
                MakeLevel("level_003", "DISTRICT 03", 285f, 135, 1450, 3),
                MakeLevel("level_004", "DISTRICT 04", 320f, 170, 1850, 4),
                MakeLevel("level_005", "DISTRICT 05", 360f, 220, 2400, 5)
            };

            campaign = ScriptableObject.CreateInstance<CampaignDefinition>();
            campaign.name = "Runtime HORDAX Campaign";
            campaign.ConfigureRuntime("prototype_campaign", "HORDAX BLOCKOUT", levels);
            return campaign;
        }

        private static LevelDefinition MakeLevel(
            string id,
            string label,
            float length,
            int completionCoins,
            int completionScore,
            int tier)
        {
            float hp = 4f + tier * 2f;
            float speed = 3.0f + tier * 0.18f;
            float damage = 6f + tier;
            int firstCount = 18 + tier * 6;
            int secondCount = 28 + tier * 10;
            int thirdCount = 38 + tier * 12;
            int eliteCount = Mathf.Min(2 + tier, 6);

            List<LevelStep> steps = new List<LevelStep>
            {
                Horde(28f, firstCount, 6 + tier, hp, speed, damage),
                Gate(54f, 35f + tier * 25f),
                Weapon(64f, tier == 1 ? WeaponArchetype.SMG : WeaponArchetype.Rifle),
                Reward(76f, tier % 2 == 0 ? -2.6f : 2.6f, 15 + tier * 5, 80 + tier * 20),
                Horde(92f, secondCount, 7 + tier, hp * 1.35f, speed + 0.15f, damage + 1f),
                Hazard(108f, tier % 2 == 0 ? 2.4f : -2.4f, 14f + tier * 3f, 2.1f + tier * 0.12f),
                Elite(122f, eliteCount, hp * 4f, speed + 0.3f, damage * 1.7f),
                Heal(134f, tier % 2 == 0 ? -2.8f : 2.8f, 18f + tier * 4f),
                Upgrade(142f, 2f + tier, 1.06f + tier * 0.01f),
                Horde(length - 72f, thirdCount, 8 + tier, hp * 1.7f, speed + 0.28f, damage + 2f),
                Weapon(length - 48f, tier >= 4 ? WeaponArchetype.Minigun : WeaponArchetype.Shotgun),
                Boss(length - 24f, 220f + tier * 120f, 2.3f + tier * 0.08f, 18f + tier * 3f, 2.1f + tier * 0.12f),
                Finish(length)
            };

            int twoStars = completionScore + 450 + tier * 180;
            int threeStars = completionScore + 1050 + tier * 320;
            int totalEnemies = firstCount + secondCount + thirdCount + eliteCount + 1;

            List<LevelObjective> objectives = new List<LevelObjective>
            {
                Objective(
                    "CLEAR THE HORDE",
                    LevelObjectiveType.KillEnemies,
                    Mathf.Max(20, Mathf.RoundToInt(totalEnemies * 0.58f)),
                    20 + tier * 5),
                Objective(
                    "ELITE HUNTER",
                    LevelObjectiveType.KillElites,
                    Mathf.Max(1, eliteCount - 1),
                    25 + tier * 6),
                Objective(
                    "STAY ALIVE",
                    LevelObjectiveType.FinishWithHealthPercent,
                    Mathf.Max(25f, 55f - tier * 4f),
                    30 + tier * 8)
            };

            LevelDefinition level = ScriptableObject.CreateInstance<LevelDefinition>();
            level.name = label;
            level.ConfigureRuntime(
                id,
                label,
                length,
                completionCoins,
                completionScore,
                steps,
                twoStars,
                threeStars,
                objectives);

            return level;
        }

        private static LevelObjective Objective(
            string label,
            LevelObjectiveType type,
            float target,
            int bonusCoins)
        {
            return new LevelObjective
            {
                label = label,
                type = type,
                target = target,
                bonusCoins = bonusCoins
            };
        }

        private static LevelStep Horde(float z, int count, int columns, float hp, float speed, float damage)
        {
            return new LevelStep
            {
                type = LevelStepType.Horde,
                label = "HORDE",
                z = z,
                enemyCount = count,
                columns = columns,
                enemyHealth = hp,
                enemySpeed = speed,
                enemyDamage = damage,
                coinReward = 1,
                scoreReward = 10
            };
        }

        private static LevelStep Elite(float z, int count, float hp, float speed, float damage)
        {
            return new LevelStep
            {
                type = LevelStepType.Elite,
                label = "ELITE",
                z = z,
                enemyCount = count,
                columns = Mathf.Min(count, 4),
                enemyHealth = hp,
                enemySpeed = speed,
                enemyDamage = damage,
                coinReward = 10,
                scoreReward = 100,
                scaleMultiplier = 1.4f
            };
        }

        private static LevelStep Boss(float z, float hp, float speed, float damage, float scale)
        {
            return new LevelStep
            {
                type = LevelStepType.Boss,
                label = "BOSS",
                z = z,
                enemyCount = 1,
                columns = 1,
                enemyHealth = hp,
                enemySpeed = speed,
                enemyDamage = damage,
                coinReward = 100,
                scoreReward = 1200,
                scaleMultiplier = scale
            };
        }

        private static LevelStep Gate(float z, float hp)
        {
            return new LevelStep
            {
                type = LevelStepType.Gate,
                label = "GATE",
                z = z,
                gateHitPoints = hp
            };
        }

        private static LevelStep Upgrade(float z, float damageAdd, float fireRateMultiplier)
        {
            return new LevelStep
            {
                type = LevelStepType.Upgrade,
                label = "UPGRADE",
                z = z,
                damageAdd = damageAdd,
                fireRateMultiplier = fireRateMultiplier,
                upgradeLabel = "+POWER"
            };
        }

        private static LevelStep Weapon(float z, WeaponArchetype weapon)
        {
            return new LevelStep
            {
                type = LevelStepType.Weapon,
                label = weapon + " PICKUP",
                z = z,
                prototypeWeapon = weapon,
                weaponLabel = weapon.ToString().ToUpperInvariant()
            };
        }

        private static LevelStep Heal(float z, float laneX, float amount)
        {
            return new LevelStep
            {
                type = LevelStepType.Heal,
                label = "MEDKIT",
                z = z,
                laneX = laneX,
                healAmount = amount,
                eventLabel = "HEAL"
            };
        }

        private static LevelStep Reward(float z, float laneX, int coins, int score)
        {
            return new LevelStep
            {
                type = LevelStepType.Reward,
                label = "REWARD",
                z = z,
                laneX = laneX,
                eventCoins = coins,
                eventScore = score,
                eventLabel = $"+{coins}"
            };
        }

        private static LevelStep Hazard(float z, float laneX, float damage, float width)
        {
            return new LevelStep
            {
                type = LevelStepType.Hazard,
                label = "HAZARD",
                z = z,
                laneX = laneX,
                hazardDamage = damage,
                eventWidth = width,
                eventLabel = "DANGER"
            };
        }

        private static LevelStep Finish(float z)
        {
            return new LevelStep
            {
                type = LevelStepType.Finish,
                label = "FINISH",
                z = z
            };
        }
    }
}
