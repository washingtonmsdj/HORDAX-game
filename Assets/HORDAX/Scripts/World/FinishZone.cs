using UnityEngine;
using HORDAX.Core;
using HORDAX.Player;

namespace HORDAX.World
{
    public sealed class FinishZone : MonoBehaviour
    {
        private bool playerReached;

        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<RunnerController>() == null) return;
            playerReached = true;
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

            GameManager.Instance.Win();
        }
    }
}
