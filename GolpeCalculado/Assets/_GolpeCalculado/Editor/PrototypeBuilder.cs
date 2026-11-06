using System.IO;
using UnityEditor;

// A cena M01 é editada diretamente no editor. Este ficheiro só guarda a build
// usada pelos testes automáticos da M01 (EditorValidation.RunAndBuild).
public static class PrototypeBuilder
{
    const string Root = "Assets/_GolpeCalculado";

    [MenuItem("Golpe Calculado/Build Windows (M01)")]
    public static void Build()
    {
        var output = System.Environment.GetEnvironmentVariable("GOLPE_BUILD_PATH");
        if (string.IsNullOrEmpty(output)) output = "../Builds/Windows/GolpeCalculado.exe";
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { Root + "/Scenes/M01_MovimentoConducao.unity" },
            locationPathName = output,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        });
        UnityEngine.Debug.Log("MARCO1_BUILD: " + report.summary.result + " errors=" + report.summary.totalErrors);
        if (UnityEngine.Application.isBatchMode)
            EditorApplication.Exit(report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
    }
}
