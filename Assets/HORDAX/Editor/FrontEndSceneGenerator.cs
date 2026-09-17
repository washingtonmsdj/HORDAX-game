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
            if (!File.Exists(ScenePath))
                GenerateScene();

            EnsureInBuildSettings();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        [MenuItem("HORDAX/Generate Front End Scene")]
        public static void GenerateFromMenu()
        {
            if (File.Exists(ScenePath))
            {
                bool confirmed = EditorUtility.DisplayDialog(
                    "Regenerate HORDAX Front End",
                    "Replace the generated FrontEnd.unity scene?",
                    "Regenerate",
                    "Cancel");

                if (!confirmed) return;
            }

            GenerateScene();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        private static void GenerateScene()
        {
            if (!Directory.Exists(SceneFolder))
                Directory.CreateDirectory(SceneFolder);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            GameObject root = new GameObject("HORDAX Front End");
            SceneManager.MoveGameObjectToScene(root, scene);
            root.AddComponent<MetaMenuController>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorSceneManager.CloseScene(scene, true);
            EnsureInBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("HORDAX front-end scene generated at " + ScenePath);
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
