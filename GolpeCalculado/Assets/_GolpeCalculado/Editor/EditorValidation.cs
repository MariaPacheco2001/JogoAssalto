using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class EditorValidation
{
    static EditorValidation()
    {
        EditorApplication.playModeStateChanged+=state=> {
            if(state==PlayModeStateChange.EnteredEditMode && SessionState.GetBool("GolpeValidation",false))
            {
                SessionState.SetBool("GolpeValidation",false);
                EditorApplication.delayCall+=()=> {
                    var args=System.Environment.GetCommandLineArgs(); int i=System.Array.IndexOf(args,"-golpeValidate");
                    bool success=i>=0 && System.IO.File.Exists(args[i+1]) && !System.IO.File.ReadAllText(args[i+1]).Contains("FAIL");
                    if(success && SessionState.GetBool("GolpeBuildAfterValidation",false)) { SessionState.SetBool("GolpeBuildAfterValidation",false); PrototypeBuilder.Build(); }
                    EditorApplication.Exit(success?0:1);
                };
            }
        };
    }
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/_GolpeCalculado/Scenes/M01_MovimentoConducao.unity");
        SessionState.SetBool("GolpeValidation",true);
        EditorApplication.isPlaying=true;
    }
    public static void RunAndBuild() { SessionState.SetBool("GolpeBuildAfterValidation",true); Run(); }
}
