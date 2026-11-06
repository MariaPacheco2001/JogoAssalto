using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using GolpeCalculado;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class IssueAudit
{
    static IssueAudit()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (!SessionState.GetBool("IssueAudit", false)) return;
            if (state == PlayModeStateChange.EnteredPlayMode)
                new GameObject("Verificacao de issues").AddComponent<IssueAuditProbe>();
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool("IssueAudit", false);
                EditorApplication.delayCall += () => EditorApplication.Exit(SessionState.GetInt("IssueAuditResult", 1));
            }
        };
    }
    public static void Run()
    {
        EditorSceneManager.OpenScene(HeistBuilder.ScenePath);
        SessionState.SetBool("IssueAudit", true);
        SessionState.SetInt("IssueAuditResult", 1);
        EditorApplication.isPlaying = true;
    }
}

public sealed class IssueAuditProbe : MonoBehaviour
{
    HeistGame g;
    readonly List<string> results = new List<string>();
    bool failed;
    HeistItem Item(string id) => g.Items.First(i => i.id == id);
    void Check(bool ok, string text) { results.Add((ok ? "PASS " : "FAIL ") + text); failed |= !ok; Debug.Log(results.Last()); }
    IEnumerator Start()
    {
        yield return null;
        g = FindFirstObjectByType<HeistGame>();
        g.TestMode = true; g.player.ExternalInput = true; g.van.ExternalInput = true;
        g.ClearProgress(); g.Begin(); g.guard.enabled = false;
        yield return null;
        Check(g.Checkpoint == 0 && !g.CaseRecovered && g.Objective.Contains("Estaciona"), "#13 Fase inicial coerente");
        g.PickCase(g.caseItem); Check(!g.CarryingCase && !g.CaseRecovered, "#13 Mala recusada antes de abrir arquivo");
        Check(!g.LoadCase() && !g.SubmitPin("020713"), "#13 Carga e terminal recusam acoes antecipadas");
        g.van.SetOccupied(true); g.van.Body.position = g.hideout.position; g.van.Body.linearVelocity = Vector3.zero;
        yield return null;
        Check(g.Current == HeistGame.Page.Play, "#20 Chegada sem mala nao vence");
        g.Begin(); g.player.Teleport(new Vector3(15,.1f,10), Quaternion.identity);
        Item("crowbar").Use(g); Item("loadingDoor").Use(g); Item("officeKey").Use(g); Item("officeDoor").Use(g);
        yield return null;
        Check(g.Checkpoint == 1 && g.Objective.Contains("Explora"), "#13 Entrada muda progresso e objetivo");
        foreach (string id in new[]{"cais","turno","dia","order","manifest"}) { Item(id).Use(g); g.Show(HeistGame.Page.Play); }
        Item("fuse").Use(g); Item("socket").Use(g); g.SubmitPin("020713");
        Check(g.VaultOpen && g.Objective.Contains("Recupera a mala NC-17"), "#13 Arquivo aberto muda objetivo");
        g.player.Teleport(new Vector3(47,.1f,53), Quaternion.identity); g.SaveCheckpoint(2);
        var snap = JsonUtility.FromJson<HeistSnapshot>(File.ReadAllText(Path.Combine(g.SaveDirectory,"cp2.json")));
        Check(snap.version == 1 && snap.items.Length == g.Items.Length && snap.items.Select(i=>i.id).Distinct().Count()==g.Items.Length,
            "#30 Snapshot versionado inclui todos os objetos com IDs unicos");
        Check(snap.inventory.Contains("officeKey") && snap.items.First(i=>i.id=="officeDoor").open && snap.circuits.Length==2 && snap.vaultOpen,
            "#30 Snapshot inclui chaves portas circuitos e progresso");
        Check(snap.playerPosition==g.player.transform.position && snap.casePosition==g.caseItem.transform.position && snap.vanPosition==g.van.transform.position,
            "#30 Snapshot inclui jogador mala e carrinha");
        g.PickCase(g.caseItem); var cp3 = File.ReadAllText(Path.Combine(g.SaveDirectory,"cp3.json"));
        int count = g.Items.Count(i=>i.kind==ItemKind.Case); g.PickCase(g.caseItem);
        Check(File.ReadAllText(Path.Combine(g.SaveDirectory,"cp3.json"))==cp3 && g.Items.Count(i=>i.kind==ItemKind.Case)==count,
            "#13 Repetir recolha nao recria checkpoint nem duplica mala");
        Check(g.Checkpoint==3 && g.CarryingCase && g.Alarm && g.Objective.Contains("traseira"), "#13 #33 Recolha cria CP3 e fase de extracao");
        Check(g.DropCase(), "#33 Mala pode ser pousada antes do restauro");
        Check(g.LoadCheckpoint(3) && g.CarryingCase && !g.CaseLoaded, "#33 Restauro depois de pousar recupera posse inequivoca");
        Check(g.LoadCheckpoint(3) && g.CarryingCase && g.guard.State==HeistGuard.Mode.Patrol && Vector3.Distance(g.guard.transform.position,g.guardResetPosition)<.2f,
            "#30 #33 Restauro transportando mala repoe vigilante no estado definido");
        Check(Vector3.Distance(g.player.transform.position,g.guard.transform.position)>1.2f && !g.guard.SeesPlayer(), "#33 Reaparecimento nao fica em alcance de captura nem visao imediata");
        g.ToggleCircuit(0); Item("officeDoor").SetOpen(false); g.Inventory.Clear(); g.Notes.Clear();
        var temporary = new GameObject("Distração de auditoria"); temporary.AddComponent<ThrownNoise>().game=g;
        var camFields = typeof(HeistCamera).GetFields(BindingFlags.Instance|BindingFlags.NonPublic);
        foreach(var camera in FindObjectsByType<HeistCamera>(FindObjectsSortMode.None))
            foreach(var field in camFields.Where(f=>f.Name=="exposure"||f.Name=="nextReport")) field.SetValue(camera,100f);
        Check(g.LoadCheckpoint(3), "#34 Restauro apos alterar portas inventario pistas circuitos e eventos");
        yield return new WaitForSeconds(2.2f);
        Check(g.CarryingCase && g.Has("officeKey") && Item("officeDoor").opened && g.Circuits[0] && g.Alarm && g.Notes.ContainsKey("manifest"),
            "#34 Jogador mala portas chaves circuitos objetivo e alarme restaurados");
        Check(FindObjectsByType<ThrownNoise>(FindObjectsSortMode.None).Length==0 && g.guard.State==HeistGuard.Mode.Patrol,
            "#34 Eventos transitorios e investigacao eletrica pendente eliminados");
        Check(FindObjectsByType<HeistItem>(FindObjectsInactive.Include,FindObjectsSortMode.None).Count(i=>i.kind==ItemKind.Case)==1,
            "#34 Mala unica apos varios restauros");
        var notes = g.Notes.ToDictionary(p=>p.Key,p=>p.Value);
        g.Notes.Clear(); g.LoadCheckpoint(3);
        Check(notes.Count==g.Notes.Count && notes.All(p=>g.Notes.ContainsKey(p.Key)&&g.Notes[p.Key]==p.Value), "#54 Diario restaurado integralmente do disco");
        g.Show(HeistGame.Page.Notes); Check(Time.timeScale==0 && g.Notes.ContainsKey("caseAlarm"), "#54 Pistas disponiveis durante pausa"); g.Show(HeistGame.Page.Play);
        for(int i=0;i<2;i++)
        {
            g.ToggleCircuit(i); yield return null;
            var lights = i==0?g.circuitBLights:g.circuitCLights;
            var cameras = g.CamerasOnCircuit(i);
            Check(lights.All(l=>!l.enabled)&&cameras.Length>0&&cameras.All(c=>c.led.material.color==Color.black), "#45 Circuito "+i+" desliga luzes e indicador da camara juntos");
            g.SaveCheckpoint(3); g.ToggleCircuit(i); g.LoadCheckpoint(3); yield return null;
            Check(!g.Circuits[i]&&lights.All(l=>!l.enabled)&&cameras.All(c=>c.led.material.color==Color.black), "#45 Restauro conserva circuito desligado "+i);
            g.ToggleCircuit(i); yield return null;
            Check(g.Circuits[i]&&lights.All(l=>l.enabled)&&cameras.All(c=>c.led.material.color!=Color.black), "#45 Religacao recupera luz e camara "+i);
            Check(g.Alarm&&g.alarmLights.All(l=>l.enabled), "#53 Alarme de reserva mantem-se independente do circuito "+i);
        }
        g.SaveCheckpoint(3); g.SetAlarm(false); Check(!g.Alarm&&g.alarmLights.All(l=>!l.enabled), "#53 Silenciar desliga indicadores do alarme");
        g.LoadCheckpoint(3); Check(g.Alarm&&g.alarmLights.All(l=>l.enabled), "#53 Restauro recupera alarme ativo");
        for(int i=0;i<3;i++)
        {
            g.TestMode=false; g.Captured(); g.TestMode=true;
            Check(g.Current==HeistGame.Page.Defeat&&Time.timeScale==0&&AudioListener.pause&&Cursor.visible,
                "#21 #39 Derrota suspende comandos e audio e liberta cursor, tentativa "+i);
            g.TestMode=false; g.Captured(); g.TestMode=true;
            Check(g.Current==HeistGame.Page.Defeat, "#21 Captura repetida nao altera derrota");
            Check(g.LoadCheckpoint(3)&&g.Running&&Time.timeScale==1&&!AudioListener.pause, "#21 #39 Reinicio devolve controlo e CP3");
        }
        g.guard.enabled=true; g.DropCase(); g.guard.ResetGuard(new Vector3(24,.06f,34));g.guard.transform.rotation=Quaternion.Euler(0,180,0);
        g.guard.enabled=false;
        g.player.Teleport(new Vector3(24,.1f,8),Quaternion.identity); Physics.SyncTransforms();
        Check(!g.guard.SeesPlayer(), "#50 Visao rejeita jogador fora do alcance");
        g.player.Teleport(new Vector3(24,.1f,39),Quaternion.identity); Physics.SyncTransforms();
        Check(!g.guard.SeesPlayer(), "#50 Visao rejeita jogador atras do vigilante");
        g.player.Teleport(new Vector3(24,.1f,29),Quaternion.identity); Physics.SyncTransforms();
        Check(g.guard.SeesPlayer(), "#50 Visao aceita jogador exposto dentro do cone");
        var wall=GameObject.CreatePrimitive(PrimitiveType.Cube);wall.transform.position=new Vector3(24,1.5f,31);wall.transform.localScale=new Vector3(3,3,.4f);Physics.SyncTransforms();
        Check(!g.guard.SeesPlayer(), "#50 Parede bloqueia visao dentro do cone");
        Destroy(wall);yield return null;g.guard.enabled=true;
        g.player.Teleport(new Vector3(24,.1f,29),Quaternion.identity);yield return new WaitForSeconds(.4f);
        Check(g.guard.State==HeistGuard.Mode.Chase && g.Current==HeistGame.Page.Play, "#50 Deteccao a distancia inicia perseguicao sem derrota");
        var guardPos=g.guard.transform.position; float time=Time.time; var target=g.guard.LastKnown;
        g.Show(HeistGame.Page.Pause);yield return new WaitForSecondsRealtime(.5f);
        Check(Vector3.Distance(g.guard.transform.position,guardPos)<.01f&&Time.time==time&&g.guard.LastKnown==target,
            "#23 #50 Pausa congela perseguicao posicao alvo e temporizador");
        g.Show(HeistGame.Page.Play);g.player.Teleport(new Vector3(36,.1f,34),Quaternion.identity);yield return new WaitForSeconds(1.5f);
        Check(g.guard.State==HeistGuard.Mode.Search&&Vector3.Distance(g.guard.LastKnown,g.player.transform.position)>5,
            "#50 Fuga por cobertura quebra visao e conserva ultima posicao");
        g.guard.enabled=false;g.LoadCheckpoint(3);
        g.player.Teleport(g.van.transform.TransformPoint(new Vector3(0,0,-2.6f)),Quaternion.identity);
        Check(g.LoadCase()&&g.Objective.Contains("esconderijo"), "#13 Carga muda objetivo para regresso");
        g.van.SetOccupied(true);g.van.Body.position=g.hideout.position;g.van.Body.linearVelocity=Vector3.zero;g.van.Body.angularVelocity=Vector3.zero;
        yield return null;
        Check(g.Current==HeistGame.Page.Victory&&Time.timeScale==0&&AudioListener.pause&&Cursor.visible, "#20 #39 Entrega confirma vitoria e repoe cursor e audio");
        yield return new WaitForSecondsRealtime(.3f);
        Check(g.Current==HeistGame.Page.Victory, "#20 Chegada repetida conserva vitoria unica");
        g.Show(HeistGame.Page.Menu);Check(g.Current==HeistGame.Page.Menu&&g.HasSave(3), "#39 Regresso ao menu preserva progresso");
        g.Begin();Check(g.Notes.Count==0&&!g.CaseRecovered&&g.Objective.Contains("Estaciona"), "#13 #54 Reinicio repoe fase inicial e diario sem solucoes");
        g.ClearProgress();
        var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-issueAudit");
        File.WriteAllLines(args[index+1],results);
        SessionState.SetInt("IssueAuditResult",failed?1:0);
        Debug.Log("ISSUE_AUDIT_"+(failed?"FAILED":"PASSED"));
        EditorApplication.isPlaying=false;
    }
}
