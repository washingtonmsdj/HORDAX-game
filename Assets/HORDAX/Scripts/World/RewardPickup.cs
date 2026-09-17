using UnityEngine;
using HORDAX.CameraSystem;
using HORDAX.Combat;
using HORDAX.Core;
using HORDAX.Player;

namespace HORDAX.World
{
    public sealed class RewardPickup : MonoBehaviour
    {
        [SerializeField, Min(0)] private int coins = 20;
        [SerializeField, Min(0)] private int score = 100;

        public void Configure(int coinAmount, int scoreAmount)
        {
            coins = Mathf.Max(0, coinAmount);
            score = Mathf.Max(0, scoreAmount);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<RunnerController>() == null) return;

            GameManager.Instance?.AddRunReward(coins, score);
            CombatFxPool.Instance?.PlayGateBreak(transform.position, 0.55f);
            RunnerCamera.Instance?.Shake(0.05f, 0.06f);
            gameObject.SetActive(false);
        }
    }
}
