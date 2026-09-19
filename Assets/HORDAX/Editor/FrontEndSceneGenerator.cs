#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using HORDAX.UI;

namespace HORDAX.EditorTools
{
    internal static class FrontEndSceneGenerator
    {
        private const string SceneFolder = "Assets/HORDAX/Scenes";
        private const string ScenePath = SceneFolder + "/FrontEnd.unity";

        [MenuItem("HORDAX/Open Front End")]
        public static void OpenFromMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Stop Play Mode before opening the HORDAX front end.");
                return;
            }

            if (!File.Exists(ScenePath) && !GenerateScene())
                return;

            EnsureInBuildSettings();

            if (SceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        [MenuItem("HORDAX/Open Front End", true)]
        private static bool ValidateOpenFromMenu()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        [MenuItem("HORDAX/Generate Front End Scene")]
        public static void GenerateFromMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Stop Play Mode before generating the HORDAX front end.");
                return;
            }

            if (File.Exists(ScenePath))
            {
                bool confirmed = EditorUtility.DisplayDialog(
                    "Regenerate HORDAX Front End",
                    "Replace the generated FrontEnd.unity scene?",
                    "Regenerate",
                    "Cancel");

                if (!confirmed)
                    return;
            }

            if (!GenerateScene())
                return;

            if (SceneManager.GetActiveScene().path != ScenePath)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        [MenuItem("HORDAX/Generate Front End Scene", true)]
        private static bool ValidateGenerateFromMenu()
        {
            return !EditorApplication.isPlayingOrWillChangePlaymode;
        }

        private static bool GenerateScene()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("HORDAX scene generation is unavailable during Play Mode.");
                return false;
            }

            if (!CanReplaceCurrentScene())
                return false;

            if (!Directory.Exists(SceneFolder))
                Directory.CreateDirectory(SceneFolder);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("HORDAX Front End");
            SceneManager.MoveGameObjectToScene(root, scene);
            root.AddComponent<MetaMenuController>();

            if (!EditorSceneManager.SaveScene(scene, ScenePath))
            {
                Debug.LogError("Failed to save HORDAX front-end scene at " + ScenePath);
                return false;
            }

            EnsureInBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("HORDAX front-end scene generated at " + ScenePath);
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

                EditorBuildSettingsScene frontEnd = new EditorBuildSettingsScene(ScenePath, true);
                scenes.RemoveAt(i);
                scenes.Insert(0, frontEnd);
                EditorBuildSettings.scenes = scenes.ToArray();
                return;
            }

            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}
#endif
