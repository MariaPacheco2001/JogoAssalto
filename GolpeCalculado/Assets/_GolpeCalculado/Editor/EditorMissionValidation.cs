using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class EditorMissionValidation
{
    static EditorMissionValidation()
    {
        EditorApplication.playModeStateChanged+=state=>{
            if(state!=PlayModeStateChange.EnteredEditMode||!SessionState.GetBool("HeistValidation",false))return;
            SessionState.SetBool("HeistValidation",false);
            EditorApplication.delayCall+=()=>{
                var args=Environment.GetCommandLineArgs();int i=Array.IndexOf(args,"-heistValidate");
                bool success=i>=0&&File.Exists(args[i+1])&&!File.ReadAllText(args[i+1]).Contains("FAIL");
                if(success&&SessionState.GetBool("HeistBuild",false)){SessionState.SetBool("HeistBuild",false);HeistBuilder.Build();}
                EditorApplication.Exit(success?0:1);
            };
        };
    }
    public static void Run(){EditorSceneManager.OpenScene(HeistBuilder.ScenePath);SessionState.SetBool("HeistValidation",true);EditorApplication.isPlaying=true;}
    public static void RunAndBuild(){SessionState.SetBool("HeistBuild",true);Run();}
}
