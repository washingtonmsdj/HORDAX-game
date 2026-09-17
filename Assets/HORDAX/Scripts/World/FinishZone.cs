using UnityEngine;
using HORDAX.Core;
using HORDAX.Player;

namespace HORDAX.World
{
    public sealed class FinishZone : MonoBehaviour
    {
        private bool playerReached;
        private PlayerHealth playerHealth;

        private void OnTriggerEnter(Collider other)
        {
            RunnerController runner = other.GetComponent<RunnerController>();
            if (runner == null) return;

            playerReached = true;
            playerHealth = runner.GetComponent<PlayerHealth>();
            TryFinish();
        }

        private void Update()
        {
            if (playerReached)
                TryFinish();
        }

        private void TryFinish()
        {
            if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;
            if (!GameManager.Instance.CanFinish) return;

            GameManager.Instance.Win(playerHealth != null ? playerHealth.Normalized : 1f);
        }
    }
}
