using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace GolpeCalculado
{
    public sealed partial class HeistValidation
    {
        IEnumerator Robustness()
        {
            var files=new Dictionary<string,string>();
            for(int cp=1;cp<=3;cp++){string path=Path.Combine(g.SaveDirectory,"cp"+cp+".json");files[path]=File.ReadAllText(path);}
            string cp3=Path.Combine(g.SaveDirectory,"cp3.json"),valid=files[cp3];
            try
            {
                g.LoadCheckpoint(3);g.Show(HeistGame.Page.Pause);
                var beforePosition=g.player.transform.position;int beforeInventory=g.Inventory.Count;
                var invalid=new Dictionary<string,string>{{"JSON truncado","{"},{"campos em falta","{}"},{"valor nulo","null"}};
                Action<string,Action<HeistSnapshot>> bad=(name,change)=>{
                    var s=JsonUtility.FromJson<HeistSnapshot>(valid);change(s);invalid[name]=JsonUtility.ToJson(s);
                };
                bad("versão desconhecida",s=>s.version=99);
                bad("checkpoint diferente do ficheiro",s=>s.checkpoint=1);
                bad("inventário ausente",s=>s.inventory=null);
                bad("inventário duplicado",s=>s.inventory=new[]{"officeKey","officeKey"});
                bad("pistas desalinhadas",s=>s.noteValues=new string[0]);
                bad("circuitos incompletos",s=>s.circuits=new[]{true});
                bad("objetos ausentes",s=>s.items=new ItemSnapshot[0]);
                bad("ID duplicado",s=>s.items[1].id=s.items[0].id);
                bad("ID desconhecido",s=>s.items[0].id="unknown");
                bad("mala carregada e transportada",s=>{s.loaded=true;s.carrying=true;});
                bad("rotação inválida",s=>s.playerRotation=new Quaternion(0,0,0,0));
                bad("posição fora dos limites",s=>s.playerPosition=Vector3.one*20000);
                foreach(var pair in invalid)
                {
                    File.WriteAllText(cp3,pair.Value);
                    Check(!g.HasSave(3)&&g.LatestSave()==2&&!g.LoadCheckpoint(3)&&g.LastSaveError.Length>0&&
                        g.Current==HeistGame.Page.Pause&&g.Inventory.Count==beforeInventory&&
                        Vector3.Distance(g.player.transform.position,beforePosition)<.01f,
                        "#35 #36 Rejeita "+pair.Key+" sem alterar sessão e conserva CP2 válido");
                }
                File.Delete(cp3);
                Check(!g.HasSave(3)&&!g.LoadCheckpoint(3),"#35 #36 Gravação ausente não bloqueia sessão");
                File.WriteAllText(cp3,valid);Check(g.LoadCheckpoint(3),"#35 Gravação válida recupera após casos inválidos");
                g.Show(HeistGame.Page.Menu);g.RequestNewGame();
                Check(g.Current==HeistGame.Page.Confirm,"#38 Novo jogo pede confirmação com progresso");
                g.CancelConfirmation();
                Check(g.Current==HeistGame.Page.Menu&&File.ReadAllText(cp3)==valid&&g.CarryingCase,"#38 Cancelar Novo jogo preserva ficheiro e mundo");
                g.Show(HeistGame.Page.Checkpoints);g.RequestCheckpoint(1);
                Check(g.Current==HeistGame.Page.Confirm&&g.Checkpoint==3,"#37 Selecionar CP1 pede confirmação antes de abandonar CP3");
                g.Back();Check(g.Current==HeistGame.Page.Checkpoints&&g.Checkpoint==3,"#37 Esc cancela seleção e regressa à lista");
                g.RequestCheckpoint(1);g.AcceptConfirmation();
                Check(g.Checkpoint==1&&!g.CarryingCase&&g.Current==HeistGame.Page.Play,"#37 Confirmar seleção restaura CP1");
                Check(!g.RequestCheckpoint(4),"#37 Checkpoint inexistente recusado");
                g.LoadCheckpoint(3);g.Show(HeistGame.Page.Pause);g.RequestRestartMission();g.CancelConfirmation();
                Check(g.Current==HeistGame.Page.Pause&&g.CarryingCase&&File.ReadAllText(cp3)==valid,"#38 Cancelar reinício conserva progresso");
                g.RequestMainMenu();g.CancelConfirmation();Check(g.Current==HeistGame.Page.Pause,"#38 Cancelar regresso ao menu mantém pausa");
                g.RequestMainMenu();g.AcceptConfirmation();
                Check(g.Current==HeistGame.Page.Menu&&File.ReadAllText(cp3)==valid,"#38 Regresso ao menu conserva checkpoints");
                g.RequestNewGame();g.AcceptConfirmation();
                Check(g.Current==HeistGame.Page.Briefing&&g.LatestSave()==0,"#38 Confirmar Novo jogo substitui gravações e abre briefing");
                g.Begin();yield return null;
                Check(g.Checkpoint==0&&!g.CarryingCase&&!g.CaseLoaded&&!g.Alarm&&!g.VaultOpen&&
                    g.Inventory.Count==0&&g.Notes.Count==0&&g.Circuits.All(c=>c)&&Item("crowbar").gameObject.activeSelf,
                    "#22 Novo jogo repõe objetos inventário circuitos e objetivo");
                Check(!g.RequestCheckpoint(3),"#37 CP3 indisponível numa partida nova");
                Item("crowbar").Use(g);g.Show(HeistGame.Page.Menu);g.RequestNewGame();g.Begin();
                Check(!g.Has("crowbar")&&Item("crowbar").gameObject.activeSelf,
                    "#22 Novo jogo antes de CP1 não reutiliza sessão abandonada");
                g.player.Teleport(new Vector3(7,.1f,-6.5f),Quaternion.identity);Physics.SyncTransforms();
                g.player.view.transform.LookAt(Item("crowbar").transform.position);
                Check(g.Target()==Item("crowbar"),"#25 Seleção de objeto baixo ignora a cápsula do jogador");
                var obstruction=GameObject.CreatePrimitive(PrimitiveType.Cube);
                obstruction.transform.position=Vector3.Lerp(g.player.view.transform.position,Item("crowbar").transform.position,.5f);
                obstruction.transform.localScale=Vector3.one*.5f;Physics.SyncTransforms();
                Check(g.Target()==null,"#25 Parede bloqueia interação com objeto");
                obstruction.SetActive(false);Destroy(obstruction);
                g.player.Teleport(new Vector3(7,.1f,-10),Quaternion.identity);g.player.view.transform.LookAt(Item("crowbar").transform.position);Physics.SyncTransforms();
                Check(g.Target()==null,"#25 Objeto fora do alcance não é selecionado");
                Item("officeDoor").Use(g);Check(!Item("officeDoor").opened&&g.Feedback.Contains("chave"),"#27 Porta explica chave em falta");
                Item("staffKey").Use(g);Item("officeDoor").Use(g);Check(!Item("officeDoor").opened,"#27 Chave diferente não abre escritório");
                Item("officeKey").Use(g);int count=g.Inventory.Count;Item("officeKey").Use(g);Item("officeDoor").Use(g);
                Check(g.Inventory.Count==count&&Item("officeDoor").opened&&!Item("officeKey").gameObject.activeSelf,"#27 Chave única abre acesso correto");
                foreach(var pair in files)File.WriteAllText(pair.Key,pair.Value);
                g.LoadCheckpoint(3);Check(g.Has("officeKey")&&!Item("officeKey").gameObject.activeSelf,"#27 Checkpoint restaura chave sem a duplicar");
                g.HasThrowable=true;Check(!g.Throw()&&g.Feedback.Contains("Pousa"),"#18 Limitação de lançamento explica como recuperar a ação");
                g.player.Teleport(new Vector3(0,.1f,-20),Quaternion.identity);Physics.SyncTransforms();
                Check(!g.LoadCase()&&g.CarryingCase,"#19 Frente da carrinha não aceita carga");
                g.player.Teleport(new Vector3(0,.1f,-27.3f),Quaternion.identity);Physics.SyncTransforms();
                Check(g.LoadCase()&&g.CaseLoaded&&!g.caseItem.gameObject.activeSelf,"#19 Traseira carrega a mala e remove objeto transportado");
                Check(!g.LoadCase()&&g.CaseLoaded,"#19 Repetir carga não duplica a mala");
                g.player.Teleport(new Vector3(-2.1f,.1f,-23.1f),Quaternion.Euler(0,90,0));g.player.view.transform.localRotation=Quaternion.identity;Physics.SyncTransforms();
                Check(g.EnterVan()&&g.CaseLoaded,"#19 Carga mantém-se ao entrar no veículo");
                g.van.SetHandbrake(false);g.van.Body.linearVelocity=g.van.transform.forward*3;
                yield return new WaitForSeconds(.15f);g.Show(HeistGame.Page.Pause);
                var vanPosition=g.van.Body.position;float active=g.ActiveSeconds;var guardPosition=g.guard.transform.position;
                yield return new WaitForSecondsRealtime(.5f);
                Check(Vector3.Distance(vanPosition,g.van.Body.position)<.01f&&Vector3.Distance(guardPosition,g.guard.transform.position)<.01f&&
                    Mathf.Abs(active-g.ActiveSeconds)<.001f&&AudioListener.pause,"#23 Pausa durante condução congela veículo vigilante tempo ativo e áudio");
                g.Show(HeistGame.Page.Play);yield return new WaitForSeconds(.15f);
                Check(g.ActiveSeconds>active&&!AudioListener.pause,"#23 Retoma recupera simulação e áudio");
                g.van.Body.linearVelocity=Vector3.zero;g.van.Body.angularVelocity=Vector3.zero;g.van.SetHandbrake(true);
                Check(g.ExitVan()&&g.CaseLoaded,"#19 Carga mantém-se ao sair do veículo");
                g.LoadCheckpoint(3);g.Show(HeistGame.Page.Pause);g.RequestRestartCheckpoint();g.CancelConfirmation();
                Check(g.Current==HeistGame.Page.Pause&&g.CarryingCase,"#38 Cancelar reinício de checkpoint preserva sessão");
                g.RequestRestartCheckpoint();g.AcceptConfirmation();
                Check(g.Current==HeistGame.Page.Play&&g.Checkpoint==3&&g.CarryingCase,"#38 Confirmar reinício recupera CP3");
                g.Show(HeistGame.Page.Pause);g.RequestRestartMission();g.AcceptConfirmation();yield return null;
                Check(g.Current==HeistGame.Page.Play&&g.LatestSave()==0&&g.Checkpoint==0&&g.Inventory.Count==0,"#38 Confirmar reinício de missão repõe mundo e remove checkpoints");
            }
            finally
            {
                foreach(var pair in files)File.WriteAllText(pair.Key,pair.Value);
                g.LoadCheckpoint(3);g.Show(HeistGame.Page.Pause);
            }
        }
    }
}
