using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GolpeCalculado;
using UnityEngine;
using UnityEngine.UI;

// Testes do resumo final das ligações descobertas (ecrã de vitória), parte da verificação de issues (IssueAudit).
public sealed partial class IssueAuditProbe
{
    const string Closing = "Num sistema, cada peça afeta as outras: perceber as ligações vale mais do que a força.";
    // Textos reais do diário para cada ligação (no ruído, o mais longo dos dois), para medir o pior caso.
    static readonly Dictionary<string, string> RealNotes = new Dictionary<string, string>
    {
        { "circuit0", "O quadro Corredor B alimenta as luzes e a câmara dessa zona. Uma falha pode levar o vigilante ao quadro." },
        { "circuit1", "O quadro Oficina C alimenta as luzes e a câmara dessa zona. Uma falha pode levar o vigilante ao quadro." },
        { "camara", "As câmaras comunicam a última posição observada. Paredes e perda de energia interrompem a observação." },
        { "ruido", "Os impactos atraem o vigilante ao local do som; a ronda fica temporariamente livre." },
        { "guardPower", "O vigilante volta a ligar os quadros que encontra desligados: cortar a energia só dá uma janela de tempo." },
        { "caseAlarm", "O sensor da mala usa alimentação de reserva. Desligar as luzes não impede o alarme de extração." },
        { "darkCrouch", HeistGame.DarkCrouchNote },
    };

    // Põe o jogador no ponto, agachado ou de pé, com o quadro B ligado ou desligado, e deixa o jogo correr uns frames.
    IEnumerator StandAt(Vector3 spot, bool crouch, bool boardOn)
    {
        g.Circuits[0] = boardOn; g.ApplyCircuits();
        g.player.CrouchInput = crouch;
        g.player.Teleport(spot, Quaternion.identity); Physics.SyncTransforms();
        for (int i = 0; i < 4; i++) yield return null;
    }

    // Mostra o ecrã de vitória de novo (o conteúdo prepara-se quando o ecrã muda).
    IEnumerator ShowVictory()
    {
        g.Show(HeistGame.Page.Play); yield return null;
        g.Show(HeistGame.Page.Victory); yield return null;
    }

    string[] SummaryLines() => g.ui.summaryList.text.Split('\n');
    bool Found(int line, string id) => SummaryLines()[line].Contains("✔") && !SummaryLines()[line].Contains("○") && SummaryLines()[line].Contains(g.Notes[id]);
    bool Missed(int line) => SummaryLines()[line].Contains("○") && !SummaryLines()[line].Contains("✔");
    static bool Fits(Text t) => t.preferredHeight <= t.rectTransform.rect.height + .5f;

    // O painel do resumo cabe no ecrã, não tapa o título, o texto e os botões, nem fica debaixo da barra de mensagens.
    string LayoutProblems()
    {
        var ui = g.ui; var problems = new List<string>();
        var panel = ScreenRect((RectTransform)ui.summaryPanel.transform);
        var canvas = ScreenRect((RectTransform)ui.end.transform);
        if (panel.xMin < canvas.xMin - .5f || panel.xMax > canvas.xMax + .5f || panel.yMin < canvas.yMin - .5f || panel.yMax > canvas.yMax + .5f) problems.Add("fora do ecra");
        if (panel.Overlaps(ScreenRect((RectTransform)ui.messagePanel.transform))) problems.Add("barra de mensagens");
        foreach (var other in new Component[] { ui.endTitle, ui.endBody }.Concat(ui.end.GetComponentsInChildren<Selectable>(true)))
            if (panel.Overlaps(ScreenRect((RectTransform)other.transform))) problems.Add(other.name);
        foreach (var text in ui.summaryPanel.GetComponentsInChildren<Text>(true))
            if (!Fits(text)) problems.Add(text.name + " nao cabe (" + Mathf.RoundToInt(text.preferredHeight) + " > " + Mathf.RoundToInt(text.rectTransform.rect.height) + ")");
        return string.Join(", ", problems);
    }

    IEnumerator Summary()
    {
        var ui = g.ui;
        var texts = ui.summaryPanel != null ? ui.summaryPanel.GetComponentsInChildren<Text>(true) : new Text[0];
        Check(ui.summaryPanel != null && ui.summaryPanel.transform.parent == ui.end.transform && ui.summaryCount != null && ui.summaryList != null
            && ui.summaryCount.transform.IsChildOf(ui.summaryPanel.transform) && ui.summaryList.transform.IsChildOf(ui.summaryPanel.transform)
            && texts.Any(t => t.text == Closing) && texts.Any(t => t.text.ToUpperInvariant() == "O SISTEMA QUE DESCOBRISTE")
            && ui.summaryPanel.GetComponentsInChildren<Selectable>(true).Length == 0,
            "Resumo: painel no Canvas do ecra de vitoria com titulo, contagem, lista e frase final");
        Check(HeistGame.SystemLinks.SequenceEqual(new[] { "circuit0", "circuit1", "camara", "ruido", "guardPower", "caseAlarm", "darkCrouch" }),
            "Resumo: as sete ligacoes pela ordem pedida");

        // ---------- Pista darkCrouch (pelo ciclo do jogo, como o jogador) ----------
        NewGame(HeistGame.Difficulty.Normal); yield return null;
        Vector3 corridor = new Vector3(24, .1f, 30), dock = new Vector3(22, .1f, 10);
        Check(g.InMapZone(HeistGuard.DarkCorridorZone, corridor) && !g.InMapZone(HeistGuard.DarkCorridorZone, dock), "darkCrouch: pontos de teste dentro e fora do Corredor B");
        yield return StandAt(corridor, true, true);
        bool lit = !g.Notes.ContainsKey("darkCrouch") && g.player.IsCrouching;
        yield return StandAt(corridor, false, false);
        bool standing = !g.Notes.ContainsKey("darkCrouch") && !g.player.IsCrouching;
        yield return StandAt(dock, true, false);
        bool outside = !g.Notes.ContainsKey("darkCrouch") && g.player.IsCrouching;
        Check(lit && standing && outside, "darkCrouch: nao aparece com o quadro B ligado, de pe ou fora do Corredor B");
        yield return StandAt(corridor, true, false);
        Check(g.Notes.TryGetValue("darkCrouch", out string note) && note == "Agachada no Corredor B às escuras, o vigilante só te vê muito perto."
            && g.Feedback.Contains("Nova pista"), "darkCrouch: agachada no Corredor B com o quadro B desligado regista a pista no diario");
        int notes = g.Notes.Count; g.Say("");
        yield return StandAt(dock, false, false); yield return StandAt(corridor, true, false);
        Check(g.Notes.Count == notes && g.Notes["darkCrouch"] == note && !g.Feedback.Contains("Nova pista"), "darkCrouch: so da primeira vez");
        g.player.CrouchInput = false; g.Circuits[0] = true; g.ApplyCircuits();
        yield return null;

        // ---------- Contagem com 0, algumas e as 7 ligações ----------
        g.Notes.Clear(); g.Notes["manifest"] = "Documento lido (nao e uma ligacao do sistema).";
        yield return ShowVictory();
        Check(ui.summaryPanel.activeInHierarchy && g.DiscoveredLinks == 0 && ui.summaryCount.text == "Descobriste 0 de 7 ligações"
            && SummaryLines().Length == 7 && Enumerable.Range(0, 7).All(Missed), "Resumo: 0 ligacoes, todas com o e explicacao (documentos nao contam)");
        string zeroLayout = LayoutProblems();
        Check(zeroLayout.Length == 0, "Resumo: com 0 ligacoes cabe no ecra de vitoria sem tapar nada" + (zeroLayout.Length > 0 ? ": " + zeroLayout : ""));
        foreach (string id in new[] { "circuit0", "ruido", "darkCrouch" }) g.Discover(id, RealNotes[id]);
        yield return ShowVictory();
        Check(g.DiscoveredLinks == 3 && ui.summaryCount.text == "Descobriste 3 de 7 ligações" && SummaryLines().Length == 7
            && Found(0, "circuit0") && Found(3, "ruido") && Found(6, "darkCrouch") && new[] { 1, 2, 4, 5 }.All(Missed),
            "Resumo: 3 ligacoes, descobertas com o texto do diario e as outras com o");
        foreach (var pair in RealNotes) g.Discover(pair.Key, pair.Value);
        g.Say("Checkpoint 1 guardado.");
        yield return ShowVictory();
        Check(g.DiscoveredLinks == 7 && ui.summaryCount.text == "Descobriste 7 de 7 ligações" && SummaryLines().Length == 7
            && Enumerable.Range(0, 7).All(i => Found(i, HeistGame.SystemLinks[i])), "Resumo: 7 ligacoes, todas com o texto do diario");
        string fullLayout = LayoutProblems();
        Check(fullLayout.Length == 0 && ui.messagePanel.activeSelf, "Resumo: com 7 ligacoes cabe no ecra de vitoria sem tapar nada nem a barra de mensagens" + (fullLayout.Length > 0 ? ": " + fullLayout : ""));
        Check(texts.Where(t => t.text == Closing).All(t => t.transform.IsChildOf(ui.summaryPanel.transform) && t.gameObject.activeInHierarchy), "Resumo: frase final por baixo da lista");

        // ---------- Ecrã de captura igual ----------
        g.Show(HeistGame.Page.Play); yield return null;
        g.TestMode = false; g.Captured(); g.TestMode = true; yield return null;
        Check(g.Current == HeistGame.Page.Defeat && !ui.summaryPanel.activeInHierarchy && ui.endTitle.text == "Foste capturado."
            && ui.endBody.text == "O vigilante apanhou-te. Usa o que descobriste para alterar o plano.", "Resumo: ecra de captura sem resumo e com o texto de sempre");

        // ---------- Gravar e carregar um checkpoint ----------
        NewGame(HeistGame.Difficulty.Normal); yield return null;
        Check(g.Captures == 0 && g.DiscoveredLinks == 0, "Resumo: novo jogo comeca com 0 capturas e 0 ligacoes");
        foreach (string id in new[] { "circuit1", "camara", "darkCrouch" }) g.Discover(id, RealNotes[id]);
        g.SaveCheckpoint(1);
        var saved = JsonUtility.FromJson<HeistSnapshot>(File.ReadAllText(Path.Combine(g.SaveDirectory, "cp1.json")));
        g.Notes.Clear(); g.Discover("ruido", RealNotes["ruido"]);
        bool loaded = g.LoadCheckpoint(1);
        Check(loaded && saved.noteKeys.Contains("darkCrouch") && g.DiscoveredLinks == 3 && new[] { "circuit1", "camara", "darkCrouch" }.All(g.Notes.ContainsKey)
            && !g.Notes.ContainsKey("ruido") && g.Notes["darkCrouch"] == HeistGame.DarkCrouchNote, "Resumo: ligacoes descobertas sobrevivem a gravar e carregar o checkpoint");
        g.TestMode = false; g.Captured(); g.TestMode = true;
        g.LoadCheckpoint(1); g.SaveCheckpoint(1);
        saved = JsonUtility.FromJson<HeistSnapshot>(File.ReadAllText(Path.Combine(g.SaveDirectory, "cp1.json")));
        Check(g.Captures == 1 && saved.captures == 1, "Resumo: captura conta e voltar ao checkpoint nao a apaga");
        // Como se o jogo tivesse sido fechado: capturas e tempo vêm do disco.
        typeof(HeistGame).GetProperty("Captures").GetSetMethod(true).Invoke(g, new object[] { 0 });
        g.Notes.Clear(); g.LoadCheckpoint(1);
        yield return ShowVictory();
        Check(g.Captures == 1 && g.DiscoveredLinks == 3 && ui.summaryCount.text == "Descobriste 3 de 7 ligações"
            && ui.endBody.text.Contains("Tempo de jogo: " + g.PlayTime) && ui.endBody.text.Contains("Capturas: 1"),
            "Resumo: capturas e ligacoes lidas do disco aparecem na vitoria com o tempo de jogo");
        g.Begin(); yield return null;
        Check(g.Captures == 0 && g.DiscoveredLinks == 0 && g.ActiveSeconds < 1, "Resumo: novo jogo repoe capturas, tempo e ligacoes");
        g.guard.enabled = false;
    }
}
