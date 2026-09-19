using UnityEngine;

namespace HORDAX.Core
{
    /// <summary>
    /// Keeps development playtests progressing even when the Unity Editor is
    /// minimized or another application has focus.
    /// Shipping builds can opt back into platform-default background behavior.
    /// </summary>
    internal static class BackgroundExecution
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Configure()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Application.runInBackground = true;
            Application.targetFrameRate = 60;
#endif
        }
    }
}
