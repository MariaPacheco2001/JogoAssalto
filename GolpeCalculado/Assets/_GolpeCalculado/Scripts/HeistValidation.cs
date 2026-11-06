using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

namespace GolpeCalculado
{
    public sealed partial class HeistValidation : MonoBehaviour
    {
        List<string> results=new List<string>();string output;bool failed;HeistGame g;
        void Check(bool ok,string test){results.Add((ok?"PASS ":"FAIL ")+test);failed|=!ok;Debug.Log(results.Last());}
        public void Capture(string suffix,Vector3 position,Vector3 look)
        {
            var camera=g.player.view;var oldPosition=camera.transform.position;var oldRotation=camera.transform.rotation;
            camera.transform.position=position;camera.transform.LookAt(look);
            var rt=new RenderTexture(1280,720,24);var previous=RenderTexture.active;var previousTarget=camera.targetTexture;
            camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;
            var texture=new Texture2D(1280,720,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,1280,720),0,0);texture.Apply();
            File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(output),Path.GetFileNameWithoutExtension(output)+suffix+".png"),texture.EncodeToPNG());
            camera.targetTexture=previousTarget;RenderTexture.active=previous;rt.Release();Destroy(rt);Destroy(texture);camera.transform.SetPositionAndRotation(oldPosition,oldRotation);
        }
        HeistItem Item(string id)=>g.Items.First(i=>i.id==id);
        IEnumerator Start()
        {
            var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-heistValidate");if(index<0){enabled=false;yield break;}
            output=args[index+1];Directory.CreateDirectory(Path.GetDirectoryName(output));
            g=FindFirstObjectByType<HeistGame>();g.TestMode=true;g.player.ExternalInput=true;g.van.ExternalInput=true;Application.targetFrameRate=60;QualitySettings.vSyncCount=0;Time.maximumDeltaTime=.05f;
            if(args.Contains("-heistResume"))
            {
                yield return null;Check(g.HasSave(3),"Gravação CP3 disponível numa nova execução");
                Check(g.LoadCheckpoint(3),"Continuar lê CP3 do disco");yield return null;
                Check(g.CarryingCase&&!g.CaseLoaded&&g.Has("manifest"),"Continuar restaura mala e inventário");
                Check(g.VaultOpen&&g.VaultPower&&g.Alarm,"Continuar restaura cofre energia e alarme");
                Finish();yield break;
            }
            Check(g.Current==HeistGame.Page.Menu&&Time.timeScale==0,"Menu inicial pausa simulação");
            Check(g.Items.Select(i=>i.id).Distinct().Count()==g.Items.Length,"Identificadores de objetos únicos");
            g.Begin();yield return new WaitForSeconds(1);
            Check(g.guard.Agent.isOnNavMesh,"Vigilante colocado na navegação");
            Capture("-exterior",new Vector3(2,2,3),new Vector3(12,2,22));
            var door=Item("loadingDoor");door.Use(g);Check(!door.opened,"Entrada de cargas exige ferramenta");
            g.player.Teleport(new Vector3(7,.1f,-6.5f),Quaternion.identity);Physics.SyncTransforms();g.player.view.transform.LookAt(Item("crowbar").transform.position);
            var pointed=g.Target();
            Check(pointed==Item("crowbar"),"Ferramenta pode ser apontada e selecionada por raio de interação; alvo="+(pointed==null?"nenhum":pointed.id));
            Item("crowbar").Use(g);door.Use(g);Check(door.opened&&g.Has("crowbar"),"Pé-de-cabra recolhido permite entrada");
            Item("staffKey").Use(g);Item("serviceDoor").Use(g);Check(Item("serviceDoor").opened,"Entrada alternativa abre com chave de serviço");
            g.player.Teleport(new Vector3(15,.1f,10),Quaternion.Euler(0,90,0));yield return new WaitForSeconds(.2f);
            Check(g.HasSave(1),"CP1 criado após entrada");
            Item("officeDoor").Use(g);Check(!Item("officeDoor").opened,"Escritório fechado sem chave");
            Item("officeKey").Use(g);Item("officeDoor").Use(g);Check(Item("officeDoor").opened,"Chave do escritório abre acesso");
            foreach(string doc in new[]{"cais","turno","dia","order","manifest"}){Item(doc).Use(g);g.Show(HeistGame.Page.Play);}
            Check(g.Has("manifest")&&g.Notes.ContainsKey("turno")&&g.Notes.ContainsKey("order"),"Documentos recolhidos e diário atualizado");
            Check(!g.SubmitPin("020713")&&!g.VaultOpen,"Terminal não abre sem energia");
            Item("fuse").Use(g);Item("socket").Use(g);Check(g.VaultPower,"Fusível instala alimentação auxiliar");
            Check(!g.SubmitPin("000000")&&!g.VaultOpen,"Código errado não abre arquivo");
            Check(g.SubmitPin("020713")&&g.vaultDoor.opened,"Código dos documentos abre arquivo");
            yield return new WaitForSeconds(1.2f);
            var route=new NavMeshPath();bool routeExists=NavMesh.CalculatePath(new Vector3(15,.08f,10),new Vector3(47,.08f,53),NavMesh.AllAreas,route);
            Check(routeExists&&route.status==NavMeshPathStatus.PathComplete,"Percurso navegável entre entrada de cargas e arquivo aberto");
            route=new NavMeshPath();routeExists=NavMesh.CalculatePath(new Vector3(14,.08f,49),new Vector3(47,.08f,53),NavMesh.AllAreas,route);
            Check(routeExists&&route.status==NavMeshPathStatus.PathComplete,"Percurso navegável entre entrada de serviço e arquivo aberto");
            g.ToggleCircuit(0);Check(!g.Circuits[0]&&g.circuitBLights.All(l=>!l.enabled),"Quadro desliga iluminação da zona");
            g.ToggleCircuit(0);Check(g.Circuits[0]&&g.circuitBLights.All(l=>l.enabled),"Quadro pode voltar a ligar");
            g.guard.ResetGuard(new Vector3(24,.06f,34));g.Noise(new Vector3(24,.1f,38),12);
            Check(g.guard.State==HeistGuard.Mode.Investigate&&Vector3.Distance(g.guard.LastKnown,new Vector3(24,.1f,38))<.1f,"Ruído comunica origem ao vigilante");
            g.guard.ResetGuard(new Vector3(24,.06f,34));g.guard.transform.rotation=Quaternion.Euler(0,180,0);
            g.player.Teleport(new Vector3(24,.1f,29),Quaternion.identity);yield return new WaitForSeconds(.4f);
            Check(g.guard.State==HeistGuard.Mode.Chase,"Vigilante vê jogador exposto e inicia perseguição");
            g.player.Teleport(new Vector3(36,.1f,34),Quaternion.identity);yield return new WaitForSeconds(1.5f);
            Check(g.guard.State==HeistGuard.Mode.Search&&Vector3.Distance(g.guard.LastKnown,g.player.transform.position)>5,"Perda de visão inicia procura na última posição observada");
            g.player.Teleport(new Vector3(43,.1f,28),Quaternion.identity);yield return new WaitForSeconds(.2f);
            Check(g.HasSave(2),"CP2 criado no interior");
            Capture("-interior",new Vector3(24,1.7f,25),new Vector3(24,1.7f,45));
            g.player.Teleport(new Vector3(47,.1f,53),Quaternion.identity);yield return null;
            g.PickCase(g.caseItem);Check(g.CarryingCase&&g.Alarm&&g.HasSave(3),"Mala recuperada ativa alarme e CP3");
            Check(g.player.SpeedMultiplier<1,"Mala reduz velocidade");
            g.HasThrowable=true;Check(!g.Throw(),"Mala impede lançamento");
            Check(g.DropCase()&&!g.CarryingCase&&g.player.SpeedMultiplier==1,"Pousar mala restaura ações");
            g.PickCase(g.caseItem);Check(g.CarryingCase,"Mala pousada pode ser recuperada");
            g.ToggleCircuit(0);g.Inventory.Clear();
            Check(g.LoadCheckpoint(3),"Checkpoint pode ser restaurado");
            Check(g.CarryingCase&&g.Has("officeKey")&&g.Has("manifest")&&g.Circuits[0],"Checkpoint restaura mala chaves pistas e circuitos");
            Check(FindObjectsByType<HeistItem>(FindObjectsInactive.Include,FindObjectsSortMode.None).Count(i=>i.kind==ItemKind.Case)==1,"Não duplica a mala");
            var p=g.guard.transform.position;g.Show(HeistGame.Page.Pause);yield return new WaitForSecondsRealtime(.4f);
            Check(Vector3.Distance(p,g.guard.transform.position)<.01f&&AudioListener.pause,"Pausa suspende vigilante e áudio");
            g.Show(HeistGame.Page.Play);g.player.Teleport(new Vector3(0,.1f,-27.3f),Quaternion.identity);yield return null;
            Check(g.LoadCase()&&g.CaseLoaded&&!g.CarryingCase,"Mala carregada na carrinha");
            g.player.Teleport(new Vector3(-2.1f,.1f,-23.1f),Quaternion.Euler(0,90,0));yield return null;
            Check(g.EnterVan(),"Entrada no veículo para extração");
            g.van.Body.position=g.hideout.position+Vector3.up*.1f;g.van.Body.linearVelocity=Vector3.zero;g.van.Body.angularVelocity=Vector3.zero;
            yield return new WaitForSecondsRealtime(.5f);
            Check(g.Current==HeistGame.Page.Victory,"Entrega da mala no esconderijo vence missão");
            g.Show(HeistGame.Page.Play);g.TestMode=false;g.Captured();Check(g.Current==HeistGame.Page.Defeat,"Captura apresenta derrota");g.TestMode=true;
            Capture("-arquivo",new Vector3(46.5f,1.8f,47),new Vector3(47,1,55));
            yield return Robustness();
            Finish();
        }
        void Finish()
        {
            File.WriteAllLines(output,results);Debug.Log("HEIST_VALIDATION_"+(failed?"FAILED":"PASSED"));
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
            #else
            Application.Quit(failed?1:0);
            #endif
        }
    }
}
