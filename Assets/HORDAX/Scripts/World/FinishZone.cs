using UnityEngine;
using HORDAX.Core;
using HORDAX.Player;

namespace HORDAX.World
{
    public sealed class FinishZone : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponent<RunnerController>() == null) return;
            if (GameManager.Instance != null) GameManager.Instance.Win();
        }
    }
}
