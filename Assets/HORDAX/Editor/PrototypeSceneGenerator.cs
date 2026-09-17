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
    [InitializeOnLoad]
    internal static class PrototypeSceneGenerator
    {
        private const string SceneFolder = "Assets/HORDAX/Scenes";
        private const string ScenePath = SceneFolder + "/Prototype.unity";

        static PrototypeSceneGenerator()
        {
            EditorApplication.delayCall += EnsurePrototypeScene;
        }

        [MenuItem("HORDAX/Open Prototype Scene")]
        public static void OpenFromMenu()
        {
            if (!File.Exists(ScenePath))
                GenerateScene(false, false);

            EnsureInBuildSettings();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        [MenuItem("HORDAX/Regenerate Prototype Scene (Destructive)")]
        public static void RegenerateFromMenu()
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "Regenerate HORDAX Prototype",
                "This replaces Assets/HORDAX/Scenes/Prototype.unity. Any manual edits made directly to that scene will be lost. Continue?",
                "Regenerate",
                "Cancel");

            if (!confirmed) return;
            GenerateScene(true, true);
        }

        private static void EnsurePrototypeScene()
        {
            if (Application.isBatchMode) return;

            if (!File.Exists(ScenePath))
                GenerateScene(false, false);
            else
                EnsureInBuildSettings();
        }

        private static void GenerateScene(bool overwrite, bool openAfter)
        {
            if (!Directory.Exists(SceneFolder)) Directory.CreateDirectory(SceneFolder);
            if (File.Exists(ScenePath) && !overwrite)
            {
                EnsureInBuildSettings();
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            GameObject root = new GameObject("HORDAX Prototype Bootstrap");
            SceneManager.MoveGameObjectToScene(root, scene);
            root.AddComponent<PrototypeBootstrap>();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorSceneManager.CloseScene(scene, true);

            EnsureInBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (openAfter)
                EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            Debug.Log("HORDAX prototype scene generated at " + ScenePath);
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
