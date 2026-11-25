using System.Collections;
using System.Collections.Generic;
using System.Linq;
using GolpeCalculado;
using UnityEngine;
using UnityEngine.UI;

// Testes da US15 (opcoes de audio, video e legendas), parte da verificacao de issues (IssueAudit).
public sealed partial class IssueAuditProbe
{
    // US15: opcoes de audio, video e legendas.
    static readonly string[] FloatOptions = { OptionKeys.MasterVolume, OptionKeys.EffectsVolume, OptionKeys.VoiceVolume, OptionKeys.Sensitivity, OptionKeys.FieldOfView };
    // Volume de um grupo da mistura em dB (NaN se a mistura nao responder).
    float Db(string parameter) => g.mixer != null && g.mixer.GetFloat(parameter, out float value) ? value : float.NaN;
    bool Volumes(float master, float effects, float voice) => Mathf.Abs(Db(HeistGame.MasterParameter) - master) < .05f
        && Mathf.Abs(Db(HeistGame.EffectsParameter) - effects) < .05f && Mathf.Abs(Db(HeistGame.VoiceParameter) - voice) < .05f;
    string VolumesText() => " (geral " + Db(HeistGame.MasterParameter) + " dB, efeitos " + Db(HeistGame.EffectsParameter) + " dB, vozes " + Db(HeistGame.VoiceParameter) + " dB)";
    bool Wired(UnityEngine.Events.UnityEventBase e, string method) => e.GetPersistentEventCount() == 1 && e.GetPersistentTarget(0) == g && e.GetPersistentMethodName(0) == method;
    // Controlos que o jogador ve: fica de fora o que esta dentro da lista (Template) de uma Dropdown.
    static bool Visible(Selectable s) => s is Dropdown || s.GetComponentInParent<Dropdown>(true) == null;
    IEnumerator Options()
    {
        var ui = g.ui; var mixer = g.mixer;
        // Os testes usam os mesmos PlayerPrefs do jogo: guardar as opcoes do jogador para as repor no fim.
        var backup = OptionKeys.All.Where(PlayerPrefs.HasKey).ToDictionary(k => k, k => FloatOptions.Contains(k) ? (object)PlayerPrefs.GetFloat(k) : PlayerPrefs.GetInt(k));
        int startQuality = QualitySettings.GetQualityLevel();

        // Mistura e grupos.
        var groups = mixer != null ? mixer.FindMatchingGroups("Master") : new UnityEngine.Audio.AudioMixerGroup[0];
        var master = groups.FirstOrDefault(x => x.name == "Master"); var effects = groups.FirstOrDefault(x => x.name == "Efeitos"); var voices = groups.FirstOrDefault(x => x.name == "Vozes");
        Check(mixer != null && groups.Length == 3 && master != null && effects != null && voices != null, "US15 Mistura com os grupos Master, Efeitos e Vozes");
        Check(effects != null && g.effects.outputAudioMixerGroup == effects && g.van.engine.outputAudioMixerGroup == effects, "US15 Respostas sonoras e motor da carrinha no grupo Efeitos");
        Check(voices != null && g.voice != null && g.voice.outputAudioMixerGroup == voices && !g.voice.playOnAwake && g.voice.spatialBlend == 0, "US15 Fonte das falas do vigilante (2D) no grupo Vozes");
        var sources = FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var loose = sources.Where(s => s.outputAudioMixerGroup == null || s.outputAudioMixerGroup.audioMixer != mixer).Select(s => s.name).ToArray();
        Check(loose.Length == 0, "US15 Todos os " + sources.Length + " sons da cena passam pela mistura" + (loose.Length > 0 ? ": falta " + string.Join(", ", loose) : ""));

        // Controlos no ecra de Opcoes, ligados no Inspector.
        var controls = new Selectable[] { ui.volume, ui.effectsVolume, ui.voiceVolume, ui.sensitivity, ui.fieldOfView, ui.invertY, ui.resolution, ui.quality, ui.fullscreen, ui.subtitles };
        Check(controls.All(c => c != null && c.transform.IsChildOf(ui.options.transform))
            && Wired(ui.volume.onValueChanged, "UI_SetVolume") && Wired(ui.effectsVolume.onValueChanged, "UI_SetEffectsVolume") && Wired(ui.voiceVolume.onValueChanged, "UI_SetVoiceVolume")
            && Wired(ui.sensitivity.onValueChanged, "UI_SetSensitivity") && Wired(ui.fieldOfView.onValueChanged, "UI_SetFieldOfView") && Wired(ui.invertY.onValueChanged, "UI_SetInvertY")
            && Wired(ui.resolution.onValueChanged, "UI_SetResolution") && Wired(ui.quality.onValueChanged, "UI_SetQuality")
            && Wired(ui.fullscreen.onValueChanged, "UI_SetFullscreen") && Wired(ui.subtitles.onValueChanged, "UI_SetSubtitles")
            && ui.effectsVolume.minValue == 0 && ui.effectsVolume.maxValue == 1 && ui.voiceVolume.minValue == 0 && ui.voiceVolume.maxValue == 1,
            "US15 Dez controlos no ecra de Opcoes ligados no Inspector");

        // Cada volume muda so o seu grupo (pelos controlos, como o jogador).
        g.UI_SetVolume(1); g.UI_SetEffectsVolume(1); g.UI_SetVoiceVolume(1);
        g.Show(HeistGame.Page.Menu); yield return null; g.UI_OpenOptions(); yield return null;
        ui.volume.value = .5f;
        Check(Volumes(-6.02f, 0, 0), "US15 Volume geral muda so o grupo Master" + VolumesText());
        ui.effectsVolume.value = .25f;
        Check(Volumes(-6.02f, -12.04f, 0), "US15 Volume dos efeitos muda so o grupo Efeitos" + VolumesText());
        ui.voiceVolume.value = 0;
        Check(Volumes(-6.02f, -12.04f, -80), "US15 Volume das vozes muda so o grupo Vozes (0 = silencio)" + VolumesText());

        // Todas as opcoes com valores diferentes, pelos controlos, e "Guardar e voltar".
        int quality = startQuality == 2 ? 3 : 2;
        bool invert = !g.player.invertY, fullscreen = !g.Fullscreen;
        ui.volume.value = .4f; ui.effectsVolume.value = .3f; ui.voiceVolume.value = .6f;
        ui.sensitivity.value = 3.5f; ui.fieldOfView.value = 88; ui.invertY.isOn = invert;
        ui.subtitles.isOn = false; ui.fullscreen.isOn = fullscreen; ui.quality.value = quality;
        int resolution = g.Resolutions.Count - 1; ui.resolution.value = resolution;
        var size = g.Resolutions[resolution];
        Check(QualitySettings.GetQualityLevel() == quality && g.ScreenSize == size && g.Fullscreen == fullscreen && !g.Subtitles,
            "US15 Qualidade, resolucao, ecra inteiro e legendas mudam pelos controlos");
        g.UI_SaveOptions();
        Check(PlayerPrefs.GetFloat(OptionKeys.MasterVolume) == .4f && PlayerPrefs.GetFloat(OptionKeys.EffectsVolume) == .3f && PlayerPrefs.GetFloat(OptionKeys.VoiceVolume) == .6f
            && PlayerPrefs.GetFloat(OptionKeys.Sensitivity) == 3.5f && PlayerPrefs.GetFloat(OptionKeys.FieldOfView) == 88 && PlayerPrefs.GetInt(OptionKeys.InvertY) == (invert ? 1 : 0)
            && PlayerPrefs.GetInt(OptionKeys.Subtitles) == 0 && PlayerPrefs.GetInt(OptionKeys.Fullscreen) == (fullscreen ? 1 : 0) && PlayerPrefs.GetInt(OptionKeys.Quality) == quality
            && PlayerPrefs.GetInt(OptionKeys.ResolutionWidth) == size.x && PlayerPrefs.GetInt(OptionKeys.ResolutionHeight) == size.y,
            "US15 Guardar e voltar grava as 11 opcoes em PlayerPrefs");

        // Mudar tudo sem gravar e arrancar de novo (LoadOptions no Awake, ApplyOptions no Start).
        g.UI_SetVolume(.9f); g.UI_SetEffectsVolume(.9f); g.UI_SetVoiceVolume(.9f); g.UI_SetSensitivity(1); g.UI_SetFieldOfView(70);
        g.UI_SetInvertY(!invert); g.UI_SetSubtitles(true); g.UI_SetFullscreen(!fullscreen); g.UI_SetQuality(startQuality);
        g.LoadOptions(); g.ApplyOptions();
        Check(g.MasterLevel == .4f && g.EffectsLevel == .3f && g.VoiceLevel == .6f && g.player.sensitivity == 3.5f && g.player.view.fieldOfView == 88
            && g.player.invertY == invert && !g.Subtitles && g.Fullscreen == fullscreen && g.ScreenSize == size && g.QualityLevel == quality,
            "US15 Ao arrancar as opcoes gravadas sao repostas");
        Check(QualitySettings.GetQualityLevel() == quality && Volumes(HeistGame.ToDecibels(.4f), HeistGame.ToDecibels(.3f), HeistGame.ToDecibels(.6f)),
            "US15 Ao arrancar a qualidade grafica e os volumes gravados sao aplicados" + VolumesText());
        g.Show(HeistGame.Page.Menu); yield return null; g.UI_OpenOptions(); yield return null;
        Check(ui.volume.value == .4f && ui.effectsVolume.value == .3f && ui.voiceVolume.value == .6f && ui.sensitivity.value == 3.5f && ui.fieldOfView.value == 88
            && ui.invertY.isOn == invert && !ui.subtitles.isOn && ui.fullscreen.isOn == fullscreen && ui.quality.value == quality
            && ui.quality.options.Count == QualitySettings.names.Length && ui.resolution.value == resolution && ui.resolution.options[resolution].text == HeistGame.ResolutionLabel(size),
            "US15 Ecra de Opcoes mostra os valores gravados (" + ui.quality.options.Count + " niveis de qualidade, " + ui.resolution.options.Count + " resolucoes)");

        // Layout: nada no ecra de Opcoes se sobrepoe nem fica debaixo da barra de mensagens.
        g.Say("Checkpoint 1 guardado."); yield return null;
        var messageRect = ScreenRect((RectTransform)g.ui.messagePanel.transform);
        var parts = ui.options.GetComponentsInChildren<Selectable>(true).Where(Visible).Select(s => (RectTransform)s.transform)
            .Concat(ui.options.GetComponentsInChildren<Text>(true).Where(t => t.GetComponentInParent<Selectable>(true) == null).Select(t => t.rectTransform)).ToArray();
        var clash = new List<string>();
        for (int i = 0; i < parts.Length; i++)
        {
            if (ScreenRect(parts[i]).Overlaps(messageRect)) clash.Add(parts[i].name + "/mensagem");
            for (int j = i + 1; j < parts.Length; j++)
                if (ScreenRect(parts[i]).Overlaps(ScreenRect(parts[j]))) clash.Add(parts[i].name + "/" + parts[j].name);
        }
        Check(clash.Count == 0 && g.ui.messagePanel.activeSelf, "US15 Opcoes sem sobreposicoes nem com a barra de mensagens (" + parts.Length + " elementos)" + (clash.Count > 0 ? ": " + string.Join(", ", clash) : ""));

        // Legendas.
        var lines = g.voiceLines;
        Check(lines != null && lines.Length == 3 && lines.Any(l => l.id == "ouvi" && l.speaker == "Vigilante" && l.text == "ouvi qualquer coisa...")
            && lines.Any(l => l.id == "radioCorredor" && l.speaker == "Rádio" && l.text == "movimento no corredor. Vou verificar.")
            && lines.Any(l => l.id == "energiaReposta" && l.speaker == "Vigilante" && l.text == "energia reposta."),
            "US15 Falas do vigilante e do radio no Inspector (Voice Lines)");
        NewGame(HeistGame.Difficulty.Normal); yield return null;
        g.UI_SetSubtitles(true); g.Say("marca"); g.Speak("ouvi"); yield return null;
        Check(ui.subtitlePanel.activeSelf && ui.subtitle.text == "Vigilante: ouvi qualquer coisa..." && ui.message.text == "marca" && ui.subtitlePanel.transform.IsChildOf(ui.hud.transform),
            "US15 Legendas ligadas: a fala aparece como legenda no HUD");
        var subtitleRect = ScreenRect((RectTransform)ui.subtitlePanel.transform);
        var hudClash = ui.hud.transform.Cast<Transform>().Where(t => t != ui.subtitlePanel.transform && ScreenRect((RectTransform)t).Overlaps(subtitleRect)).Select(t => t.name)
            .Concat(subtitleRect.Overlaps(ScreenRect((RectTransform)ui.messagePanel.transform)) ? new[] { "mensagem" } : new string[0]).ToArray();
        Check(hudClash.Length == 0, "US15 Legenda nao tapa a barra de mensagens nem o HUD" + (hudClash.Length > 0 ? ": " + string.Join(", ", hudClash) : ""));
        g.Show(HeistGame.Page.Pause); yield return null;
        bool hiddenInPause = !ui.subtitlePanel.activeSelf;
        g.Show(HeistGame.Page.Play); g.UI_SetSubtitles(false); yield return null;
        Check(hiddenInPause && !ui.subtitlePanel.activeSelf, "US15 Legenda escondida na pausa e ao desligar as legendas");
        g.Speak("radioCorredor"); yield return null;
        Check(!ui.subtitlePanel.activeSelf && ui.message.text == "Rádio: movimento no corredor. Vou verificar.", "US15 Legendas desligadas e sem som de voz: fica a mensagem atual");
        var heard = lines.First(l => l.id == "ouvi"); heard.clip = AudioClip.Create("voz de teste", 4410, 1, 44100, false);
        g.Say("marca"); g.Speak("ouvi"); yield return null;
        Check(!ui.subtitlePanel.activeSelf && ui.message.text == "marca", "US15 Legendas desligadas com som de voz: so se ouve a fala");
        g.UI_SetSubtitles(true); g.Speak("ouvi"); yield return null;
        Check(ui.subtitlePanel.activeSelf && ui.subtitle.text == "Vigilante: ouvi qualquer coisa...", "US15 Legendas ligadas com som de voz: ouve-se e le-se");
        heard.clip = null;
        // Vigilante religa um quadro: com legendas desligadas a mensagem junta a pista nova, como antes.
        g.UI_SetSubtitles(false); g.Circuits[0] = false; g.ApplyCircuits(); g.RestoreCircuit(0); yield return null;
        Check(g.Circuits[0] && ui.message.text == "Vigilante: energia reposta. Nova pista no diário · J para consultar" && !ui.subtitlePanel.activeSelf,
            "US15 Sem legendas: energia reposta e pista nova na barra de mensagens");
        g.UI_SetSubtitles(true); g.Circuits[0] = false; g.ApplyCircuits(); g.RestoreCircuit(0); yield return null;
        Check(ui.subtitle.text == "Vigilante: energia reposta." && ui.subtitlePanel.activeSelf, "US15 Com legendas: energia reposta aparece como legenda");

        // Repor as opcoes do jogador.
        foreach (var key in OptionKeys.All) PlayerPrefs.DeleteKey(key);
        foreach (var pair in backup)
            if (pair.Value is float f) PlayerPrefs.SetFloat(pair.Key, f); else PlayerPrefs.SetInt(pair.Key, (int)pair.Value);
        PlayerPrefs.Save();
        QualitySettings.SetQualityLevel(startQuality, true);
        g.LoadOptions(); g.ApplyOptions();
        g.ClearProgress();
    }
}
