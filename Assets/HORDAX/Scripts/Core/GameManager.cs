using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using HORDAX.Data;

namespace HORDAX.Core
{
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState State { get; private set; } = GameState.Booting;
        public int EnemyKills { get; private set; }
        public int EliteKills { get; private set; }
        public int BossKills { get; private set; }
        public int RunCoins { get; private set; }
        public int Score { get; private set; }
        public float FinishZ { get; set; } = 165f;
        public string CurrentLevelId { get; private set; } = "prototype_level";
        public int RequiredBossKills { get; private set; }
        public bool CanFinish => BossKills >= RequiredBossKills;
        public int EarnedStars { get; private set; }
        public int ObjectiveCompletedCount { get; private set; }
        public int ObjectiveBonusCoins { get; private set; }
        public int ObjectiveTotal => objectives.Count;

        public event Action Changed;

        private readonly List<LevelObjective> objectives = new List<LevelObjective>();
        private readonly Dictionary<int, float> activeBossBarriers = new Dictionary<int, float>();
        private int completionCoins = 75;
        private int completionScore = 500;
        private int twoStarScore = 1400;
        private int threeStarScore = 2200;
        private float finalHealthNormalized = 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void ConfigureLevel(
            string levelId,
            int coinsOnComplete,
            int scoreOnComplete,
            int requiredBossKills = 0,
            int scoreForTwoStars = 0,
            int scoreForThreeStars = 0,
            IReadOnlyList<LevelObjective> levelObjectives = null)
        {
            CurrentLevelId = string.IsNullOrWhiteSpace(levelId) ? "prototype_level" : levelId;
            completionCoins = Mathf.Max(0, coinsOnComplete);
            completionScore = Mathf.Max(0, scoreOnComplete);
            RequiredBossKills = Mathf.Max(0, requiredBossKills);
            twoStarScore = Mathf.Max(completionScore, scoreForTwoStars > 0 ? scoreForTwoStars : completionScore * 2);
            threeStarScore = Mathf.Max(twoStarScore, scoreForThreeStars > 0 ? scoreForThreeStars : completionScore * 3);

            objectives.Clear();
            if (levelObjectives != null)
            {
                for (int i = 0; i < levelObjectives.Count; i++)
                {
                    LevelObjective objective = levelObjectives[i];
                    if (objective != null)
                        objectives.Add(objective);
                }
            }
        }

        public void Begin()
        {
            EnemyKills = 0;
            EliteKills = 0;
            BossKills = 0;
            RunCoins = 0;
            Score = 0;
            EarnedStars = 0;
            ObjectiveCompletedCount = 0;
            ObjectiveBonusCoins = 0;
            finalHealthNormalized = 1f;
            activeBossBarriers.Clear();
            State = GameState.Playing;
            Changed?.Invoke();
        }

        public void RegisterEnemyKill()
        {
            RegisterEnemyKill(EnemyRank.Grunt, 1, 10);
        }

        public void RegisterBossBarrier(int ownerId, float barrierZ)
        {
            if (ownerId == 0) return;
            activeBossBarriers[ownerId] = Mathf.Max(0f, barrierZ);
        }

        public void ReleaseBossBarrier(int ownerId)
        {
            if (ownerId == 0) return;
            activeBossBarriers.Remove(ownerId);
        }

        public bool TryGetActiveBossBarrier(out float barrierZ)
        {
            barrierZ = float.PositiveInfinity;
            if (activeBossBarriers.Count == 0) return false;

            foreach (KeyValuePair<int, float> entry in activeBossBarriers)
                barrierZ = Mathf.Min(barrierZ, entry.Value);

            return !float.IsPositiveInfinity(barrierZ);
        }

        public void RegisterEnemyKill(EnemyRank rank, int coinReward, int scoreReward)
        {
            EnemyKills++;
            RunCoins += Mathf.Max(0, coinReward);
            Score += Mathf.Max(0, scoreReward);

            if (rank == EnemyRank.Elite) EliteKills++;
            if (rank == EnemyRank.Boss) BossKills++;

            Changed?.Invoke();
        }

        public void AddRunReward(int coins, int score)
        {
            if (State != GameState.Playing) return;

            RunCoins += Mathf.Max(0, coins);
            Score += Mathf.Max(0, score);
            Changed?.Invoke();
        }

        public void Win(float healthNormalized = 1f)
        {
            if (State != GameState.Playing || !CanFinish) return;

            finalHealthNormalized = Mathf.Clamp01(healthNormalized);
            RunCoins += completionCoins;
            Score += completionScore;

            EvaluateObjectives();
            RunCoins += ObjectiveBonusCoins;

            EarnedStars = Score >= threeStarScore ? 3 : Score >= twoStarScore ? 2 : 1;
            State = GameState.Won;

            ProgressionService.GetOrCreate().CompleteLevel(
                CurrentLevelId,
                RunCoins,
                Score,
                EnemyKills,
                EarnedStars,
                ObjectiveCompletedCount);

            Changed?.Invoke();
        }

        public void Lose()
        {
            if (State != GameState.Playing) return;
            State = GameState.Lost;
            Changed?.Invoke();
        }

        public string GetObjectiveResultSummary()
        {
            if (objectives.Count == 0) return "NO OPTIONAL OBJECTIVES";
            return $"OBJECTIVES {ObjectiveCompletedCount}/{objectives.Count}   BONUS +{ObjectiveBonusCoins}";
        }

        public string GetObjectiveLabel(int index)
        {
            if (index < 0 || index >= objectives.Count) return string.Empty;

            LevelObjective objective = objectives[index];
            string label = string.IsNullOrWhiteSpace(objective.label) ? objective.type.ToString() : objective.label;
            return $"{label} ({FormatObjectiveProgress(objective)})";
        }

        public bool IsObjectiveComplete(int index)
        {
            if (index < 0 || index >= objectives.Count) return false;
            return EvaluateObjective(objectives[index]);
        }

        private void EvaluateObjectives()
        {
            ObjectiveCompletedCount = 0;
            ObjectiveBonusCoins = 0;

            for (int i = 0; i < objectives.Count; i++)
            {
                LevelObjective objective = objectives[i];
                if (!EvaluateObjective(objective)) continue;

                ObjectiveCompletedCount++;
                ObjectiveBonusCoins += Mathf.Max(0, objective.bonusCoins);
            }
        }

        private bool EvaluateObjective(LevelObjective objective)
        {
            if (objective == null) return false;

            float target = Mathf.Max(1f, objective.target);
            switch (objective.type)
            {
                case LevelObjectiveType.KillEnemies:
                    return EnemyKills >= Mathf.CeilToInt(target);

                case LevelObjectiveType.KillElites:
                    return EliteKills >= Mathf.CeilToInt(target);

                case LevelObjectiveType.CollectRunCoins:
                    return RunCoins >= Mathf.CeilToInt(target);

                case LevelObjectiveType.ReachScore:
                    return Score >= Mathf.CeilToInt(target);

                case LevelObjectiveType.FinishWithHealthPercent:
                    return finalHealthNormalized * 100f >= target;

                default:
                    return false;
            }
        }

        private string FormatObjectiveProgress(LevelObjective objective)
        {
            float target = Mathf.Max(1f, objective.target);
            switch (objective.type)
            {
                case LevelObjectiveType.KillEnemies:
                    return $"{EnemyKills}/{Mathf.CeilToInt(target)}";

                case LevelObjectiveType.KillElites:
                    return $"{EliteKills}/{Mathf.CeilToInt(target)}";

                case LevelObjectiveType.CollectRunCoins:
                    return $"{RunCoins}/{Mathf.CeilToInt(target)} coins";

                case LevelObjectiveType.ReachScore:
                    return $"{Score}/{Mathf.CeilToInt(target)} score";

                case LevelObjectiveType.FinishWithHealthPercent:
                    return $"{Mathf.RoundToInt(finalHealthNormalized * 100f)}/{Mathf.CeilToInt(target)}% HP";

                default:
                    return string.Empty;
            }
        }

        public void Restart()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
