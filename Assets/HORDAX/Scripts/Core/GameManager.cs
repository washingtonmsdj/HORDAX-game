using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HORDAX.Core
{
    public sealed class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public GameState State { get; private set; } = GameState.Booting;
        public int EnemyKills { get; private set; }
        public float FinishZ { get; set; } = 165f;

        public event Action Changed;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void Begin()
        {
            EnemyKills = 0;
            State = GameState.Playing;
            Changed?.Invoke();
        }

        public void RegisterEnemyKill()
        {
            EnemyKills++;
            Changed?.Invoke();
        }

        public void Win()
        {
            if (State != GameState.Playing) return;
            State = GameState.Won;
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
