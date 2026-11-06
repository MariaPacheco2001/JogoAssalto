using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Build with -buildTarget OSXUniversal so platform import happens before compilation.
public static class ReleaseBuilder
{
    public static void BuildMac()
    {
        var target = NamedBuildTarget.Standalone;
        string platform = BuildPipeline.GetBuildTargetName(BuildTarget.StandaloneOSX);
        string previousArchitecture = EditorUserBuildSettings.GetPlatformSettings(platform, "Architecture");
        var previousBackend = PlayerSettings.GetScriptingBackend(target);
        bool success = false;
        try
        {
            // macOS uses the platform architecture setting, not PlayerSettings.SetArchitecture.
            EditorUserBuildSettings.SetPlatformSettings(platform, "Architecture", OSArchitecture.x64ARM64.ToString());
            PlayerSettings.SetScriptingBackend(target, ScriptingImplementation.Mono2x);
            Directory.CreateDirectory("../Builds/AssaltoMacOS");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { HeistBuilder.ScenePath,
                    "Assets/_GolpeCalculado/Scenes/M01_MovimentoConducao.unity" },
                target = BuildTarget.StandaloneOSX,
                locationPathName = "../Builds/AssaltoMacOS/GolpeCalculado.app",
                options = BuildOptions.None
            });
            success = report.summary.result == BuildResult.Succeeded;
            Debug.Log("HEIST_MAC_BUILD " + report.summary.result +
                " errors=" + report.summary.totalErrors + " bytes=" + report.summary.totalSize);
        }
        finally
        {
            EditorUserBuildSettings.SetPlatformSettings(platform, "Architecture", previousArchitecture);
            PlayerSettings.SetScriptingBackend(target, previousBackend);
            AssetDatabase.SaveAssets();
        }
        if (Application.isBatchMode) EditorApplication.Exit(success ? 0 : 1);
    }
}
