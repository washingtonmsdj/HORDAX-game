using System;
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

        public event Action Changed;

        private int completionCoins = 75;
        private int completionScore = 500;
        private int twoStarScore = 1400;
        private int threeStarScore = 2200;

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
            int scoreForThreeStars = 0)
        {
            CurrentLevelId = string.IsNullOrWhiteSpace(levelId) ? "prototype_level" : levelId;
            completionCoins = Mathf.Max(0, coinsOnComplete);
            completionScore = Mathf.Max(0, scoreOnComplete);
            RequiredBossKills = Mathf.Max(0, requiredBossKills);
            twoStarScore = Mathf.Max(completionScore, scoreForTwoStars > 0 ? scoreForTwoStars : completionScore * 2);
            threeStarScore = Mathf.Max(twoStarScore, scoreForThreeStars > 0 ? scoreForThreeStars : completionScore * 3);
        }

        public void Begin()
        {
            EnemyKills = 0;
            EliteKills = 0;
            BossKills = 0;
            RunCoins = 0;
            Score = 0;
            EarnedStars = 0;
            State = GameState.Playing;
            Changed?.Invoke();
        }

        public void RegisterEnemyKill()
        {
            RegisterEnemyKill(EnemyRank.Grunt, 1, 10);
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

        public void Win()
        {
            if (State != GameState.Playing || !CanFinish) return;

            RunCoins += completionCoins;
            Score += completionScore;
            EarnedStars = Score >= threeStarScore ? 3 : Score >= twoStarScore ? 2 : 1;
            State = GameState.Won;
            ProgressionService.GetOrCreate().CompleteLevel(
                CurrentLevelId,
                RunCoins,
                Score,
                EnemyKills,
                EarnedStars);
            Changed?.Invoke();
        }

        public void Lose()
        {
            if (State != GameState.Playing) return;
            State = GameState.Lost;
            Changed?.Invoke();
        }

        public void Restart()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
