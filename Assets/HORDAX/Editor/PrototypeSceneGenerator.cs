#if UNITY_EDITOR
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

        [MenuItem("HORDAX/Generate Prototype Scene")]
        public static void GenerateFromMenu()
        {
            GenerateScene(true);
        }

        private static void EnsurePrototypeScene()
        {
            if (Application.isBatchMode || File.Exists(ScenePath)) return;
            GenerateScene(false);
        }

        private static void GenerateScene(bool forceOpen)
        {
            if (!Directory.Exists(SceneFolder)) Directory.CreateDirectory(SceneFolder);

            if (File.Exists(ScenePath) && !forceOpen)
                return;

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("HORDAX Prototype Bootstrap");
            root.AddComponent<PrototypeBootstrap>();
            EditorSceneManager.SaveScene(scene, ScenePath);

            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeGameObject = root;
            Debug.Log("HORDAX prototype scene generated at " + ScenePath);
        }
    }
}
#endif
