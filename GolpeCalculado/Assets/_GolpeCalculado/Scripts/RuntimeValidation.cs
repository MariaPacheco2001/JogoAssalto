using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace GolpeCalculado
{
    public sealed class RuntimeValidation : MonoBehaviour
    {
        readonly List<string> results = new List<string>();
        string path;
        bool failed;
        void Capture(Camera camera,string suffix)
        {
            var target=new RenderTexture(1280,720,24);
            var previous=RenderTexture.active; var oldTarget=camera.targetTexture;
            camera.targetTexture=target; camera.Render(); RenderTexture.active=target;
            var pixels=new Texture2D(1280,720,TextureFormat.RGB24,false);
            pixels.ReadPixels(new Rect(0,0,1280,720),0,0); pixels.Apply();
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(path),Path.GetFileNameWithoutExtension(path)+suffix+".png"),pixels.EncodeToPNG());
            camera.targetTexture=oldTarget; RenderTexture.active=previous; target.Release(); Destroy(target); Destroy(pixels);
        }
        void Check(bool condition,string name) { results.Add((condition?"PASS ":"FAIL ")+name); if(!condition) failed=true; Debug.Log(results[results.Count-1]); }
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs(); int index=Array.IndexOf(args,"-golpeValidate");
            if(index<0 || index+1>=args.Length) { enabled=false; yield break; }
            path=args[index+1];
            var s=FindFirstObjectByType<PrototypeSession>(); var p=s.player; var v=s.van;
            s.TestMode=true; p.ExternalInput=true; v.ExternalInput=true;
            Application.targetFrameRate=60; QualitySettings.vSyncCount=0; Time.maximumDeltaTime=.05f;
            Check(!s.Started && Time.timeScale==0,"Menu inicial suspende simulação");
            s.Begin(); yield return new WaitForSeconds(1.5f);
            Capture(p.view,"-inicio");
            Vector3 start=p.transform.position; p.MoveInput=Vector2.down;
            float moveElapsed=0;
            for(int frame=0;frame<60;frame++) { yield return null; moveElapsed+=Time.deltaTime; }
            p.MoveInput=Vector2.zero;
            float moved=Vector3.Distance(start,p.transform.position);
            Check(moved>moveElapsed*p.walkSpeed*.75f && moved>.1f,"Movimento a pé distância="+moved.ToString("F2")+" tempo="+moveElapsed.ToString("F2")+" posição="+p.transform.position);
            p.CrouchInput=true; yield return null; Check(p.Body.height<1.2f,"Agachar reduz cápsula");
            p.CrouchInput=false; yield return new WaitForSeconds(.15f); Check(p.Body.height>1.7f,"Levantar restaura cápsula");
            float y=p.transform.position.y; p.JumpInput=true; float peak=y;
            for(int frame=0;frame<30;frame++) { yield return null; peak=Mathf.Max(peak,p.transform.position.y); }
            Check(peak>y+.2f,"Salto altura="+(peak-y).ToString("F2"));
            yield return new WaitForSeconds(.7f);
            p.Teleport(new Vector3(-2.3f,.1f,-23.1f),Quaternion.Euler(0,90,0)); yield return null;
            Check(s.TryEnter(),"Entrar junto à porta com linha de visão");
            Check(!p.Body.enabled && !p.controlled && v.Occupied,"Exclusão dos controlos a pé na carrinha");
            v.SetHandbrake(false); v.ThrottleInput=1;
            yield return new WaitForSeconds(2);
            Check(v.SpeedKmh>5 && v.transform.position.z>-23,"Aceleração e rodas em contacto");
            Capture(p.view,"-conducao");
            Check(!s.TryExit(),"Saída impedida em movimento");
            Vector3 beforePause=v.transform.position; s.SetPause(true);
            yield return new WaitForSecondsRealtime(.4f);
            Check(Vector3.Distance(beforePause,v.transform.position)<.02f && AudioListener.pause,"Pausa congela carrinha e áudio");
            s.SetPause(false); float yaw=v.transform.eulerAngles.y; v.SteerInput=.7f;
            yield return new WaitForSeconds(1);
            Check(Mathf.Abs(Mathf.DeltaAngle(yaw,v.transform.eulerAngles.y))>3,"Direção altera trajetória");
            v.SteerInput=0; v.ThrottleInput=0; v.BrakeInput=true;
            yield return new WaitForSeconds(2); Check(v.SpeedKmh<1,"Travão de serviço imobiliza");
            v.BrakeInput=false; v.ThrottleInput=-1; yield return new WaitForSeconds(1.1f);
            Check(v.SignedSpeed<-.4f,"Marcha atrás"); v.ThrottleInput=0; v.BrakeInput=true;
            yield return new WaitForSeconds(1.5f); v.BrakeInput=false; v.SetHandbrake(true);
            v.Body.position=new Vector3(4,.08f,36); v.Body.rotation=Quaternion.identity; v.Body.linearVelocity=Vector3.zero; v.Body.angularVelocity=Vector3.zero;
            yield return new WaitForSeconds(.8f); Check(s.IsParked(),"Lugar de estacionamento alinhado e travão aplicado");
            var left=GameObject.CreatePrimitive(PrimitiveType.Cube); left.name="Bloqueio teste esquerdo"; left.transform.position=v.leftExit.position+Vector3.up*.65f; left.transform.localScale=new Vector3(1,2,1);
            yield return new WaitForFixedUpdate(); Check(s.TryExit() && p.transform.position.x>v.transform.position.x,"Saída alternativa quando porta esquerda bloqueada");
            Destroy(left); yield return null;
            Check(p.Body.enabled && p.controlled && !v.Occupied && s.Stage==4,"Regresso a pé e conclusão do percurso");
            p.Teleport(v.transform.TransformPoint(new Vector3(-2.1f,.08f,.9f)),Quaternion.Euler(0,90,0)); yield return null;
            Check(s.TryEnter(),"Reentrada após estacionamento");
            var blocks=new GameObject[2]; var markers=new[]{v.leftExit,v.rightExit};
            for(int i=0;i<2;i++) { blocks[i]=GameObject.CreatePrimitive(PrimitiveType.Cube); blocks[i].transform.position=markers[i].position+Vector3.up*.6f; blocks[i].transform.localScale=new Vector3(1,2,1); }
            yield return new WaitForFixedUpdate(); Check(!s.TryExit() && v.Occupied,"Duas saídas bloqueadas mantêm condutor seguro");
            foreach(var b in blocks) Destroy(b); yield return null;
            Check(s.TryExit(),"Saída recupera após remoção dos obstáculos");
            Directory.CreateDirectory(Path.GetDirectoryName(path)); File.WriteAllLines(path,results);
            p.Teleport(new Vector3(-4,.1f,31),Quaternion.Euler(0,53,0));
            Capture(p.view,"-estacionamento");
            yield return new WaitForSecondsRealtime(1);
            Debug.Log("MARCO1_VALIDATION_"+(failed?"FAILED":"PASSED"));
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
            #else
            Application.Quit(failed?1:0);
            #endif
        }
    }
}
