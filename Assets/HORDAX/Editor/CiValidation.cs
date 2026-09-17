#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

namespace HORDAX.EditorTools
{
    public static class CiValidation
    {
        public static void Run()
        {
            try
            {
                Debug.Log("[HORDAX CI] Starting Unity CLI validation.");
                Debug.Log("[HORDAX CI] Unity version: " + Application.unityVersion);
                Debug.Log("[HORDAX CI] Project: " + Application.dataPath);

                AssetDatabase.Refresh();
                ProjectValidator.ValidateProjectData();

                Debug.Log("[HORDAX CI] Script compilation completed and project validation executed.");
                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError("[HORDAX CI] Validation failed.");
                EditorApplication.Exit(1);
            }
        }
    }
}
#endif
