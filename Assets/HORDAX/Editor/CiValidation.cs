#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Compilation;
using UnityEngine;

namespace HORDAX.EditorTools
{
    public static class CiValidation
    {
        private const string Prefix = "[HORDAX CI]";

        public static void Run()
        {
            try
            {
                Debug.Log(Prefix + " Starting Unity CLI validation.");
                Debug.Log(Prefix + " Unity version: " + Application.unityVersion);
                Debug.Log(Prefix + " Project: " + Application.dataPath);

                if (EditorApplication.isCompiling)
                {
                    Debug.LogError(Prefix + " Unity is still compiling when validation was invoked.");
                    EditorApplication.Exit(2);
                    return;
                }

                Assembly[] assemblies = CompilationPipeline.GetAssemblies(AssembliesType.PlayerWithoutTestAssemblies);
                Debug.Log(Prefix + " Player assemblies discovered: " + assemblies.Length);

                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

                ProjectValidator.ValidationReport report = ProjectValidator.RunValidation();
                ProjectValidator.LogReport(report);

                if (!report.IsValid)
                {
                    Debug.LogError(
                        $"{Prefix} Data validation failed with {report.ErrorCount} error(s) and {report.WarningCount} warning(s).");
                    EditorApplication.Exit(3);
                    return;
                }

                Debug.Log(
                    $"{Prefix} PASS. Compilation loaded, asset refresh completed, and project data has no blocking errors. " +
                    $"Warnings: {report.WarningCount}.");

                EditorApplication.Exit(0);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Debug.LogError(Prefix + " Validation crashed.");
                EditorApplication.Exit(10);
            }
        }
    }
}
#endif
