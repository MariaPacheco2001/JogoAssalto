using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using GolpeCalculado;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

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
    float sceneVision, sceneReaction;

    // US18: três níveis de dificuldade.
    IEnumerator Difficulty()
    {
        var easy = HeistGame.Difficulty.Easy; var normal = HeistGame.Difficulty.Normal; var hard = HeistGame.Difficulty.Hard;
        var table = g.difficulties;
        Check(table.Length == 3 && table[0].name == "Fácil" && table[1].name == "Normal" && table[2].name == "Difícil", "US18 Tabela com Facil Normal e Dificil no HeistGame");
        Check(table[1].visionRange == 15 && table[1].powerReactionDelay == 2 && !table[1].fullMap
            && table[1].visionRange == sceneVision && table[1].powerReactionDelay == sceneReaction,
            "US18 Normal igual ao jogo atual (visao 15 m, reacao 2 s, mapa a revelar)");
        Check(table[0].visionRange == 12 && table[0].powerReactionDelay == 3 && table[0].fullMap, "US18 Facil: 12 m, 3 s, mapa completo");
        Check(table[2].visionRange == 18 && table[2].powerReactionDelay == 1 && !table[2].fullMap, "US18 Dificil: 18 m, 1 s, mapa a revelar");

        // Botoes do briefing ligados no Inspector ao nivel certo.
        bool wired = g.ui.difficultyButtons.Length == 3;
        for (int i = 0; wired && i < 3; i++)
        {
            var click = g.ui.difficultyButtons[i].onClick;
            wired = click.GetPersistentEventCount() == 1 && click.GetPersistentTarget(0) == g && click.GetPersistentMethodName(0) == "UI_SetDifficulty"
                && g.ui.difficultyButtons[i].transform.IsChildOf(g.ui.briefing.transform);
        }
        Check(wired && g.ui.difficultyDetails != null && g.ui.pauseDifficulty != null && g.ui.pauseDifficulty.transform.IsChildOf(g.ui.pause.transform),
            "US18 Botoes Facil Normal Dificil no briefing e nivel na pausa");

        // Novo jogo abre o briefing com o Normal pre-selecionado, mesmo depois de uma partida noutro nivel.
        g.ClearProgress(); g.Show(HeistGame.Page.Menu); g.SetDifficulty(hard);
        g.RequestNewGame(); yield return null;
        Check(g.Current == HeistGame.Page.Briefing && g.Level == normal && !g.ui.difficultyButtons[1].interactable
            && g.ui.difficultyButtons[0].interactable && g.ui.difficultyButtons[2].interactable && g.ui.difficultyDetails.text.StartsWith("Normal"),
            "US18 Novo jogo pre-seleciona Normal no briefing");

        var guard = g.guard; var guardSpot = new Vector3(24, .06f, 34);
        for (int level = 0; level < 3; level++)
        {
            var settings = table[level];
            // Escolha no briefing (pelo botao, como o jogador) e inicio da missao.
            g.ClearProgress(); g.Show(HeistGame.Page.Menu); g.RequestNewGame();
            g.ui.difficultyButtons[level].onClick.Invoke(); yield return null;
            Check(g.Level == (HeistGame.Difficulty)level && !g.ui.difficultyButtons[level].interactable && g.ui.difficultyDetails.text.StartsWith(settings.name),
                "US18 Botao " + settings.name + " escolhe o nivel");
            g.UI_StartHeist(); guard.enabled = false;
            Check(g.Running && guard.visionRange == settings.visionRange && guard.powerReactionDelay == settings.powerReactionDelay && g.FullMapFromStart == settings.fullMap,
                "US18 " + settings.name + " aplica visao " + settings.visionRange + " m, reacao " + settings.powerReactionDelay + " s e mapa");
            g.Show(HeistGame.Page.Pause); yield return null;
            Check(g.ui.pauseDifficulty.text.Contains(settings.name), "US18 Pausa mostra o nivel " + settings.name);
            g.Show(HeistGame.Page.Play);

            // Visao: a 13,5 m so o Normal e o Dificil veem; a 16,5 m so o Dificil. Repetido, da sempre o mesmo.
            bool[] expected = { level >= 1, level >= 2 };
            float[] distances = { 13.5f, 16.5f };
            for (int d = 0; d < 2; d++)
            {
                bool same = true, seen = false;
                for (int repeat = 0; repeat < 3; repeat++)
                {
                    guard.ResetGuard(guardSpot); guard.transform.rotation = Quaternion.Euler(0, 180, 0);
                    g.player.Teleport(new Vector3(24, .1f, 34 - distances[d]), Quaternion.identity); Physics.SyncTransforms();
                    bool now = guard.SeesPlayer();
                    if (repeat == 0) seen = now; else same &= now == seen;
                }
                Check(same && seen == expected[d], "US18 RNF01 " + settings.name + ": jogador a " + distances[d] + " m " + (expected[d] ? "visto" : "nao visto") + " sempre");
            }

            // Falha eletrica: o vigilante parte para o quadro passado o tempo do nivel, sempre igual.
            g.player.Teleport(g.hideout.position + Vector3.up * .1f, Quaternion.identity);
            float first = -1; bool repeatable = true;
            for (int repeat = 0; repeat < 2; repeat++)
            {
                guard.ResetGuard(guardSpot); g.Circuits[0] = true; g.ApplyCircuits();
                g.ToggleCircuit(0); float start = Time.time; float limit = start + settings.powerReactionDelay + 2;
                while (guard.PowerCircuit != 0 && Time.time < limit) yield return null;
                float took = guard.PowerCircuit == 0 ? Time.time - start : -1;
                if (repeat == 0) first = took; else repeatable = took > 0 && Mathf.Abs(took - first) < .1f;
            }
            Check(first > 0 && Mathf.Abs(first - settings.powerReactionDelay) < .1f && repeatable,
                "US18 RNF01 " + settings.name + ": vigilante reage a falha eletrica em " + settings.powerReactionDelay + " s (medido " + first.ToString("0.00") + " s) sempre");
            guard.ResetGuard(guardSpot); g.Circuits[0] = true; g.ApplyCircuits();
        }

        // Gravar e carregar: o nivel fica no checkpoint e volta com Carregar, Continuar, Selecionar checkpoint e Recomecar.
        g.ClearProgress(); g.Show(HeistGame.Page.Menu); g.RequestNewGame(); g.UI_SetDifficulty((int)hard); g.UI_StartHeist(); guard.enabled = false;
        g.SaveCheckpoint(1);
        string cp1 = Path.Combine(g.SaveDirectory, "cp1.json");
        Check(JsonUtility.FromJson<HeistSnapshot>(File.ReadAllText(cp1)).difficulty == (int)hard, "US18 Checkpoint grava o nivel");
        g.SetDifficulty(easy);
        Check(g.LoadCheckpoint(1) && g.Level == hard && guard.visionRange == 18 && guard.powerReactionDelay == 1, "US18 Carregar checkpoint repoe o nivel e os valores");
        g.SetDifficulty(easy); g.Show(HeistGame.Page.Menu); g.UI_Continue();
        Check(g.Running && g.Level == hard && guard.visionRange == 18, "US18 Continuar mantem o nivel da partida");
        g.SetDifficulty(normal); g.Show(HeistGame.Page.Menu); g.UI_OpenCheckpoints(); g.UI_Checkpoint(1);
        if (g.Current == HeistGame.Page.Confirm) g.AcceptConfirmation();
        Check(g.Running && g.Level == hard && guard.powerReactionDelay == 1, "US18 Selecionar checkpoint mantem o nivel da partida");
        g.Show(HeistGame.Page.Pause); g.RequestRestartCheckpoint(); g.AcceptConfirmation();
        Check(g.Running && g.Level == hard, "US18 Recomecar do checkpoint mantem o nivel");
        g.Show(HeistGame.Page.Pause); g.RequestRestartMission(); g.AcceptConfirmation(); guard.enabled = false;
        Check(g.Running && g.Level == hard && guard.visionRange == 18 && g.LatestSave() == 0, "US18 Recomecar missao mantem o nivel");

        // Gravacoes antigas (sem nivel) carregam em Normal; um nivel desconhecido e rejeitado.
        g.SaveCheckpoint(1);
        string json = File.ReadAllText(cp1);
        File.WriteAllText(cp1, Regex.Replace(json, @",\s*""difficulty""\s*:\s*\d+", ""));
        Check(!File.ReadAllText(cp1).Contains("difficulty") && g.LoadCheckpoint(1) && g.Level == normal && guard.visionRange == 15 && guard.powerReactionDelay == 2,
            "US18 Gravacao antiga sem nivel carrega em Normal");
        var bad = JsonUtility.FromJson<HeistSnapshot>(json); bad.difficulty = 7; File.WriteAllText(cp1, JsonUtility.ToJson(bad));
        Check(!g.HasSave(1) && !g.LoadCheckpoint(1), "US18 Nivel desconhecido na gravacao e rejeitado");
        g.ClearProgress();
    }
    // US17: mapa do diario.
    GameObject ZoneImage(string id) => g.mapZones.First(z => z.id == id).mapImage;
    static readonly string[] ZoneIds = { "patio", "cais", "corredorB", "vestiario", "oficina", "escritorio", "arquivo" };
    bool OnlyRevealed(params string[] ids) => ZoneIds.All(id => g.IsRevealed(id) == ids.Contains(id));
    bool OnlyShown(params string[] ids) => ZoneIds.All(id => ZoneImage(id).activeSelf == ids.Contains(id));
    // Estado de tudo o que esta dentro da area do mapa (nome, visivel, posicao, tamanho, rotacao).
    string MapState() => string.Join("|", g.ui.mapArea.GetComponentsInChildren<RectTransform>(true)
        .Select(r => r.name + r.gameObject.activeSelf + r.anchoredPosition + r.sizeDelta + r.localEulerAngles));
    void NewGame(HeistGame.Difficulty level)
    {
        g.ClearProgress(); g.Show(HeistGame.Page.Menu); g.RequestNewGame(); g.UI_SetDifficulty((int)level); g.UI_StartHeist(); g.guard.enabled = false;
    }
    IEnumerator Map()
    {
        var zones = g.mapZones; var guard = g.guard;
        Check(zones.Length == 7 && ZoneIds.All(id => zones.Count(z => z != null && z.id == id) == 1) && zones.All(z => !string.IsNullOrEmpty(z.displayName)
            && z.mapImage != null && z.mapImage.transform.IsChildOf(g.ui.mapArea) && z.GetComponents<BoxCollider>().Length > 0 && z.GetComponents<BoxCollider>().All(b => b.isTrigger)),
            "US17 Sete zonas com nome, caixas trigger e imagem no mapa");
        Check(g.ui.mapArea.IsChildOf(g.ui.notes.transform) && g.ui.mapPlayer.parent == g.ui.mapArea, "US17 Mapa e marcador do jogador no ecra do diario (J)");
        // O centro de cada caixa pertence a essa zona e a mais nenhuma.
        bool exclusive = zones.All(z => z.GetComponents<BoxCollider>().All(b => zones.Count(o => o.Contains(b.bounds.center)) == 1 && z.Contains(b.bounds.center)));
        Check(exclusive, "US17 Zonas nao se sobrepoem");
        // O mapa so tem zonas e o jogador: nada do vigilante, das camaras, dos quadros nem da mala.
        bool onlyZonesAndPlayer = g.ui.mapArea.GetComponentsInChildren<RectTransform>(true).Where(r => r != g.ui.mapArea)
            .All(r => r.IsChildOf(g.ui.mapPlayer) || zones.Any(z => r.IsChildOf(z.mapImage.transform)));
        bool noSecurityNames = !g.ui.mapArea.GetComponentsInChildren<Transform>(true).Any(t => Regex.IsMatch(t.name, "vigil|c.mara|quadro|mala|alarme", RegexOptions.IgnoreCase));
        Check(onlyZonesAndPlayer && noSecurityNames, "US17 Mapa so contem zonas e o jogador (sem vigilante, camaras, quadros ou mala)");

        // Normal: comeca so com o patio, onde o jogador esta.
        NewGame(HeistGame.Difficulty.Normal); yield return null;
        Check(!g.FullMapFromStart && OnlyRevealed("patio"), "US17 Normal: no inicio so o patio esta revelado");
        g.Show(HeistGame.Page.Notes); yield return null;
        Check(OnlyShown("patio") && g.ui.mapPlayer.gameObject.activeInHierarchy, "US17 Diario mostra so as zonas visitadas e o jogador");
        g.Show(HeistGame.Page.Play);
        g.player.Teleport(new Vector3(22, .1f, 10), Quaternion.identity); yield return null;
        Check(OnlyRevealed("patio", "cais"), "US17 Entrar no cais revela o cais e mais nenhuma zona");
        g.Show(HeistGame.Page.Notes); yield return null;
        Check(OnlyShown("patio", "cais"), "US17 Imagem do cais so aparece depois da visita");
        var expected = new Vector2((22 + 25) / 85f * g.ui.mapArea.rect.width, (10 + 43) / 105f * g.ui.mapArea.rect.height);
        Check(Vector2.Distance(g.ui.mapPlayer.anchoredPosition, expected) < 1, "US17 Marcador do jogador na posicao certa (cais)");
        g.Show(HeistGame.Page.Play);
        g.player.Teleport(new Vector3(24, .1f, 30), Quaternion.Euler(0, 90, 0)); yield return null;
        g.Show(HeistGame.Page.Notes); yield return null;
        expected = new Vector2((24 + 25) / 85f * g.ui.mapArea.rect.width, (30 + 43) / 105f * g.ui.mapArea.rect.height);
        Check(OnlyShown("patio", "cais", "corredorB") && Vector2.Distance(g.ui.mapPlayer.anchoredPosition, expected) < 1
            && Mathf.Abs(Mathf.DeltaAngle(g.ui.mapPlayer.localEulerAngles.z, -90)) < 1, "US17 Marcador acompanha posicao e direcao do jogador (corredor B)");

        // Mexer no vigilante, nos quadros e nas camaras nao muda nada no mapa.
        string before = MapState();
        g.Show(HeistGame.Page.Play);
        guard.ResetGuard(new Vector3(45, .06f, 30)); g.ToggleCircuit(0); g.ToggleCircuit(1); yield return null;
        g.Show(HeistGame.Page.Notes); yield return null;
        string afterGuard = MapState();
        g.Show(HeistGame.Page.Play); guard.ResetGuard(new Vector3(24, .06f, 34)); g.ToggleCircuit(0); g.ToggleCircuit(1);
        g.Show(HeistGame.Page.Notes); yield return null;
        Check(before == afterGuard && before == MapState(), "US17 Vigilante e sistema de seguranca nao aparecem nem mudam o mapa");
        g.Show(HeistGame.Page.Play);

        // Gravar e carregar.
        g.SaveCheckpoint(1);
        string cp1 = Path.Combine(g.SaveDirectory, "cp1.json");
        var saved = JsonUtility.FromJson<HeistSnapshot>(File.ReadAllText(cp1)).revealedZones;
        Check(saved.Length == 3 && saved.Contains("patio") && saved.Contains("cais") && saved.Contains("corredorB"), "US17 Checkpoint grava as zonas reveladas");
        g.player.Teleport(new Vector3(46, .1f, 26), Quaternion.identity); yield return null;
        Check(g.IsRevealed("escritorio"), "US17 Entrar no escritorio revela-o");
        Check(g.LoadCheckpoint(1) && OnlyRevealed("patio", "cais", "corredorB"), "US17 Carregar checkpoint repoe as zonas gravadas");
        g.Show(HeistGame.Page.Notes); yield return null;
        Check(OnlyShown("patio", "cais", "corredorB"), "US17 Depois de carregar o diario mostra as zonas gravadas");
        g.Show(HeistGame.Page.Menu); g.UI_Continue();
        Check(g.Running && OnlyRevealed("patio", "cais", "corredorB"), "US17 Continuar repoe as zonas gravadas");
        g.Show(HeistGame.Page.Pause); g.RequestRestartMission(); g.AcceptConfirmation(); guard.enabled = false;
        Check(OnlyRevealed(), "US17 Recomecar missao esconde de novo as zonas");
        yield return null;
        Check(OnlyRevealed("patio"), "US17 Recomecar missao: so o patio volta a aparecer");

        // Gravacoes antigas sem zonas carregam com o mapa por revelar; nomes repetidos sao rejeitados.
        g.player.Teleport(new Vector3(22, .1f, 10), Quaternion.identity); yield return null;
        g.SaveCheckpoint(1);
        string json = File.ReadAllText(cp1);
        File.WriteAllText(cp1, Regex.Replace(json, @",\s*""revealedZones""\s*:\s*\[[^\]]*\]", ""));
        Check(!File.ReadAllText(cp1).Contains("revealedZones") && g.LoadCheckpoint(1) && OnlyRevealed(), "US17 Gravacao antiga sem zonas carrega com o mapa por revelar");
        var bad = JsonUtility.FromJson<HeistSnapshot>(json); bad.revealedZones = new[] { "cais", "cais" }; File.WriteAllText(cp1, JsonUtility.ToJson(bad));
        Check(!g.HasSave(1), "US17 Zonas repetidas na gravacao sao rejeitadas");

        // Facil: mapa completo desde o inicio, tambem ao carregar.
        NewGame(HeistGame.Difficulty.Easy);
        Check(g.FullMapFromStart && OnlyRevealed(ZoneIds), "US17 Facil: todas as zonas reveladas desde o inicio");
        g.Show(HeistGame.Page.Notes); yield return null;
        Check(OnlyShown(ZoneIds), "US17 Facil: diario mostra o mapa completo");
        g.Show(HeistGame.Page.Play); g.SaveCheckpoint(1);
        var easy = JsonUtility.FromJson<HeistSnapshot>(File.ReadAllText(cp1)); easy.revealedZones = new[] { "patio" }; File.WriteAllText(cp1, JsonUtility.ToJson(easy));
        Check(g.LoadCheckpoint(1) && g.Level == HeistGame.Difficulty.Easy && OnlyRevealed(ZoneIds), "US17 Facil: carregar checkpoint mantem o mapa completo");
        NewGame(HeistGame.Difficulty.Hard); yield return null;
        Check(OnlyRevealed("patio"), "US17 Dificil: mapa a revelar como no Normal");
        g.ClearProgress();
    }
    IEnumerator Start()
    {
        yield return null;
        g = FindFirstObjectByType<HeistGame>();
        g.TestMode = true; g.player.ExternalInput = true; g.van.ExternalInput = true;
        // Valores do vigilante tal como estão na cena, antes de qualquer nível ser aplicado (referência do Normal).
        sceneVision = g.guard.visionRange; sceneReaction = g.guard.powerReactionDelay;
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
        g.player.Teleport(new Vector3(24,.1f,34.7f),Quaternion.identity); Physics.SyncTransforms();
        Check(g.guard.SeesPlayer(), "#2 Proximidade: sente jogador encostado nas costas");
        g.player.Teleport(new Vector3(24.7f,.1f,34),Quaternion.identity); Physics.SyncTransforms();
        Check(g.guard.SeesPlayer(), "#2 Proximidade: sente jogador encostado de lado");
        var cameraSpot=new Vector3(24,.1f,20); var bottleSpot=new Vector3(30,.1f,34); var boardSpot=new Vector3(18,.1f,34);
        g.guard.Investigate(cameraSpot,HeistGuard.CameraPriority); g.guard.Investigate(bottleSpot,HeistGuard.NoisePriority);
        Check(g.guard.State==HeistGuard.Mode.Investigate&&g.guard.LastKnown==cameraSpot, "Prioridade: garrafa depois da camara nao a substitui");
        g.guard.ResetGuard(new Vector3(24,.06f,34));
        g.guard.Investigate(bottleSpot,HeistGuard.NoisePriority); g.guard.Investigate(cameraSpot,HeistGuard.CameraPriority);
        Check(g.guard.LastKnown==cameraSpot, "Prioridade: camara depois da garrafa substitui-a");
        g.guard.Investigate(boardSpot,HeistGuard.AlarmPriority);
        Check(g.guard.LastKnown==cameraSpot, "Prioridade: alarme nao substitui a camara");
        g.guard.ResetGuard(new Vector3(24,.06f,34));
        g.guard.Investigate(bottleSpot,HeistGuard.NoisePriority); g.guard.Investigate(boardSpot,HeistGuard.PowerPriority);
        Check(g.guard.LastKnown==boardSpot, "Prioridade: avisos de igual importancia seguem o mais recente");
        g.guard.ResetGuard(new Vector3(24,.06f,34)); g.guard.Investigate(bottleSpot,HeistGuard.NoisePriority);
        Check(g.guard.LastKnown==bottleSpot, "Prioridade: de volta a ronda qualquer aviso conta");
        g.guard.ResetGuard(new Vector3(24,.06f,34));g.guard.transform.rotation=Quaternion.Euler(0,180,0);Physics.SyncTransforms();
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
        // Falha eletrica: o vigilante vai ao quadro depois do tempo de reacao e religa-o.
        g.guard.enabled=false;g.LoadCheckpoint(3);
        float reaction=g.guard.powerReactionDelay;
        var board0=g.Items.First(i=>i.kind==ItemKind.Power&&i.circuit==0);
        NavMesh.SamplePosition(board0.transform.position,out var boardHit,4,NavMesh.AllAreas);
        var boardLights=g.circuitBLights; var boardCameras=g.CamerasOnCircuit(0);
        g.player.Teleport(g.hideout.position+Vector3.up*.1f,Quaternion.identity);
        g.guard.ResetGuard(boardHit.position);g.guard.powerReactionDelay=1;g.guard.enabled=true;
        g.ToggleCircuit(0);yield return new WaitForSeconds(.5f);
        Check(!g.Circuits[0]&&g.guard.PowerCircuit==-1&&g.guard.State==HeistGuard.Mode.Patrol, "Quadro: vigilante espera o tempo de reacao configurado");
        float powerLimit=Time.time+6; while(!g.Circuits[0]&&Time.time<powerLimit) yield return null;
        yield return null; // o LED da camara atualiza no Update seguinte
        Check(g.Circuits[0]&&boardLights.All(l=>l.enabled)&&boardCameras.All(c=>c.led.material.color!=Color.black)&&g.Notes.ContainsKey("guardPower"),
            "Quadro: vigilante religa luzes e camara ao chegar e deixa pista");
        g.guard.powerReactionDelay=reaction;
        g.Circuits[0]=false;g.ApplyCircuits();g.guard.ResetGuard(boardHit.position);
        g.guard.Investigate(boardHit.position,HeistGuard.NoisePriority);yield return new WaitForSeconds(1);
        Check(!g.Circuits[0]&&g.guard.State==HeistGuard.Mode.Investigate, "Quadro: ruido junto ao quadro nao o religa");
        g.guard.ResetGuard(new Vector3(24,.06f,34));g.guard.transform.rotation=Quaternion.Euler(0,180,0);
        g.player.Teleport(new Vector3(24,.1f,29),Quaternion.identity);Physics.SyncTransforms();
        g.guard.InvestigatePower(0,boardHit.position);yield return new WaitForSeconds(.4f);
        Check(g.guard.State==HeistGuard.Mode.Chase&&g.guard.PowerCircuit==-1, "Quadro: ver o jogador a caminho cancela a religacao");
        g.player.Teleport(g.hideout.position+Vector3.up*.1f,Quaternion.identity);g.guard.Agent.Warp(boardHit.position);yield return new WaitForSeconds(1.5f);
        Check(!g.Circuits[0], "Quadro: depois da perseguicao o quadro continua desligado");
        g.guard.enabled=false;g.LoadCheckpoint(3);
        g.player.Teleport(g.van.transform.TransformPoint(new Vector3(0,0,-2.6f)),Quaternion.identity);
        Check(g.LoadCase()&&g.Objective.Contains("esconderijo"), "#13 Carga muda objetivo para regresso");
        g.van.SetOccupied(true);g.van.Body.position=g.hideout.position+Vector3.up*.1f;g.van.transform.position=g.van.Body.position;
        g.van.Body.linearVelocity=Vector3.zero;g.van.Body.angularVelocity=Vector3.zero;Physics.SyncTransforms();
        float limit=Time.realtimeSinceStartup+5;
        while(g.Current!=HeistGame.Page.Victory&&Time.realtimeSinceStartup<limit) yield return null;
        Check(g.Current==HeistGame.Page.Victory&&Time.timeScale==0&&AudioListener.pause&&Cursor.visible, "#20 #39 Entrega confirma vitoria e repoe cursor e audio");
        yield return new WaitForSecondsRealtime(.3f);
        Check(g.Current==HeistGame.Page.Victory, "#20 Chegada repetida conserva vitoria unica");
        g.Show(HeistGame.Page.Menu);Check(g.Current==HeistGame.Page.Menu&&g.HasSave(3), "#39 Regresso ao menu preserva progresso");
        g.Begin();Check(g.Notes.Count==0&&!g.CaseRecovered&&g.Objective.Contains("Estaciona"), "#13 #54 Reinicio repoe fase inicial e diario sem solucoes");
        yield return Difficulty();
        yield return Map();
        g.ClearProgress();
        var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-issueAudit");
        File.WriteAllLines(args[index+1],results);
        SessionState.SetInt("IssueAuditResult",failed?1:0);
        Debug.Log("ISSUE_AUDIT_"+(failed?"FAILED":"PASSED"));
        EditorApplication.isPlaying=false;
    }
}
