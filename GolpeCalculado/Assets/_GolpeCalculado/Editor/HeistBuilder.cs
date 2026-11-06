using System.IO;
using UnityEditor;
using UnityEngine;

// A cena M02 é editada diretamente no editor. Este ficheiro só guarda o caminho
// da cena e a build Windows, usados pelos testes automáticos e pelo ReleaseBuilder.
public static class HeistBuilder
{
    const string Root = "Assets/_GolpeCalculado";
    public const string ScenePath = Root + "/Scenes/M02_AssaltoArmazem.unity";

    [MenuItem("Golpe Calculado/Build Windows (missão)")]
    public static void Build()
    {
        Directory.CreateDirectory("../Builds/AssaltoWindows");
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { ScenePath, Root + "/Scenes/M01_MovimentoConducao.unity" },
            target = BuildTarget.StandaloneWindows64,
            locationPathName = "../Builds/AssaltoWindows/GolpeCalculado.exe",
            options = BuildOptions.None
        });
        Debug.Log("HEIST_BUILD " + report.summary.result + " errors=" + report.summary.totalErrors);
        if (Application.isBatchMode)
            EditorApplication.Exit(report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded ? 0 : 1);
    }
}
