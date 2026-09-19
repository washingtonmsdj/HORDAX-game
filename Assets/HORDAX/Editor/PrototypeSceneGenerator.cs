#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using HORDAX.Prototype;

namespace HORDAX.EditorTools
{
    internal static class PrototypeSceneGenerator
    {
        private const string SceneFolder = "Assets/HORDAX/Scenes";
        private const string ScenePath = SceneFolder + "/Prototype.unity";

        [MenuItem("HORDAX/Open Prototype Scene")]
        public static void OpenFromMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Stop Play Mode before opening the HORDAX prototype scene.");
                return;
            }

            if (!File.Exists(ScenePath) && !GenerateScene(false))
                return;

            EnsureInBuildSettings();

            if (SceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        [MenuItem("HORDAX/Open Prototype Scene", true)]
        private static bool ValidateOpenFromMenu()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        [MenuItem("HORDAX/Regenerate Prototype Scene (Destructive)")]
        public static void RegenerateFromMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Stop Play Mode before regenerating the HORDAX prototype scene.");
                return;
            }

            bool confirmed = EditorUtility.DisplayDialog(
                "Regenerate HORDAX Prototype",
                "This replaces Assets/HORDAX/Scenes/Prototype.unity. Any manual edits made directly to that scene will be lost. Continue?",
                "Regenerate",
                "Cancel");

            if (!confirmed || !GenerateScene(true))
                return;

            if (SceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        [MenuItem("HORDAX/Regenerate Prototype Scene (Destructive)", true)]
        private static bool ValidateRegenerateFromMenu()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        private static bool GenerateScene(bool overwrite)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("HORDAX scene generation is unavailable during Play Mode.");
                return false;
            }

            if (File.Exists(ScenePath) && !overwrite)
            {
                EnsureInBuildSettings();
                return true;
            }

            if (!CanReplaceCurrentScene())
                return false;

            if (!Directory.Exists(SceneFolder))
                Directory.CreateDirectory(SceneFolder);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("HORDAX Prototype Bootstrap");
            SceneManager.MoveGameObjectToScene(root, scene);
            root.AddComponent<PrototypeBootstrap>();

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                Debug.LogError("Failed to save HORDAX prototype scene at " + ScenePath);
                return false;
            }

            EnsureInBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("HORDAX prototype scene generated at " + ScenePath);
            return true;
        }

        private static bool CanReplaceCurrentScene()
        {
            Scene active = SceneManager.GetActiveScene();
            if (!active.IsValid())
                return true;

            if (string.IsNullOrEmpty(active.path) && IsDefaultUntitledScene(active))
                return true;

            return EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        }

        private static bool IsDefaultUntitledScene(Scene scene)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            if (roots.Length != 2)
                return false;

            bool hasMainCamera = false;
            bool hasDirectionalLight = false;

            for (int i = 0; i < roots.Length; i++)
            {
                if (roots[i].name == "Main Camera")
                    hasMainCamera = true;
                else if (roots[i].name == "Directional Light")
                    hasDirectionalLight = true;
            }

            return hasMainCamera && hasDirectionalLight;
        }

        private static void EnsureInBuildSettings()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            for (int i = 0; i < scenes.Count; i++)
            {
                if (scenes[i].path != ScenePath) continue;
                if (!scenes[i].enabled)
                {
                    scenes[i] = new EditorBuildSettingsScene(ScenePath, true);
                    EditorBuildSettings.scenes = scenes.ToArray();
                }
                return;
            }

            scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
