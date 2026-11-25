using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;

namespace GolpeCalculado
{
    // Uma fala do vigilante ou mensagem de rádio (US15). O texto e o som de voz ficam no Inspector.
    [Serializable]
    public class VoiceLine
    {
        [Tooltip("Identificador usado no código (ex.: ouvi, radioCorredor, energiaReposta).")] public string id;
        [Tooltip("Quem fala: Vigilante ou Rádio.")] public string speaker = "Vigilante";
        [TextArea] public string text;
        [Tooltip("Som de voz (opcional). Sem som, a fala aparece sempre em texto.")] public AudioClip clip;
    }

    // Nomes das opções gravadas em PlayerPrefs. "volume", "mouseSensitivity", "invertY" e "fov" já existiam.
    public static class OptionKeys
    {
        public const string MasterVolume = "volume";
        public const string EffectsVolume = "volumeEfeitos";
        public const string VoiceVolume = "volumeVozes";
        public const string Sensitivity = "mouseSensitivity";
        public const string InvertY = "invertY";
        public const string FieldOfView = "fov";
        public const string ResolutionWidth = "resolucaoLargura";
        public const string ResolutionHeight = "resolucaoAltura";
        public const string Fullscreen = "ecraInteiro";
        public const string Quality = "qualidade";
        public const string Subtitles = "legendas";

        public static readonly string[] All = { MasterVolume, EffectsVolume, VoiceVolume, Sensitivity, InvertY, FieldOfView,
            ResolutionWidth, ResolutionHeight, Fullscreen, Quality, Subtitles };
    }

    // Opções do jogador (US15): volumes por canal, vídeo e legendas.
    // São lidas no Awake, aplicadas no Start (arranque do jogo) e gravadas em "Guardar e voltar".
    public sealed partial class HeistGame
    {
        [Header("Opções de áudio")]
        [Tooltip("Mistura (Audio/Mistura) com os grupos Master, Efeitos e Vozes. Cada grupo expõe o volume com o nome abaixo.")]
        public AudioMixer mixer;
        public const string MasterParameter = "VolumeGeral";
        public const string EffectsParameter = "VolumeEfeitos";
        public const string VoiceParameter = "VolumeVozes";

        [Header("Falas do vigilante e rádio")]
        [Tooltip("Fonte 2D das falas, ligada ao grupo Vozes da mistura.")] public AudioSource voice;
        [Tooltip("Falas que o código pede pelo id. Com legendas ligadas aparecem no ecrã; desligadas, só se ouve o som.")] public VoiceLine[] voiceLines;
        [Tooltip("Segundos que uma legenda fica no ecrã.")] public float subtitleSeconds = 4;

        // Valores atuais das opções.
        public float MasterLevel { get; private set; } = .7f;
        public float EffectsLevel { get; private set; } = 1;
        public float VoiceLevel { get; private set; } = 1;
        public bool Subtitles { get; private set; } = true;
        public bool Fullscreen { get; private set; }
        public int QualityLevel { get; private set; }
        public Vector2Int ScreenSize { get; private set; }

        // Resoluções do ecrã mostradas na lista (sem repetir a mesma largura × altura).
        public List<Vector2Int> Resolutions { get; private set; } = new List<Vector2Int>();

        string subtitle = "";
        float subtitleUntil;

        // Legenda visível neste momento ("" se não houver).
        public string Subtitle => Time.unscaledTime < subtitleUntil ? subtitle : "";

        // Lê as opções gravadas. Sem nada gravado, fica o que o jogo já usa (volume 70 %, sem mudar o vídeo).
        public void LoadOptions()
        {
            MasterLevel = PlayerPrefs.GetFloat(OptionKeys.MasterVolume, .7f);
            EffectsLevel = PlayerPrefs.GetFloat(OptionKeys.EffectsVolume, 1);
            VoiceLevel = PlayerPrefs.GetFloat(OptionKeys.VoiceVolume, 1);
            player.sensitivity = PlayerPrefs.GetFloat(OptionKeys.Sensitivity, 2);
            player.invertY = PlayerPrefs.GetInt(OptionKeys.InvertY, 0) == 1;
            player.view.fieldOfView = PlayerPrefs.GetFloat(OptionKeys.FieldOfView, 75);
            Subtitles = PlayerPrefs.GetInt(OptionKeys.Subtitles, 1) == 1;
            Fullscreen = PlayerPrefs.GetInt(OptionKeys.Fullscreen, Screen.fullScreen ? 1 : 0) == 1;
            QualityLevel = Mathf.Clamp(PlayerPrefs.GetInt(OptionKeys.Quality, QualitySettings.GetQualityLevel()), 0, QualitySettings.names.Length - 1);
            ScreenSize = new Vector2Int(PlayerPrefs.GetInt(OptionKeys.ResolutionWidth, Screen.width), PlayerPrefs.GetInt(OptionKeys.ResolutionHeight, Screen.height));
            RefreshResolutions();
        }

        // Aplica as opções lidas: volumes na mistura e, se já foram gravadas, o vídeo.
        public void ApplyOptions()
        {
            ApplyVolumes();
            if (PlayerPrefs.HasKey(OptionKeys.Quality))
                QualitySettings.SetQualityLevel(QualityLevel, true);
            if (PlayerPrefs.HasKey(OptionKeys.ResolutionWidth))
                ApplyScreen();
        }

        public void SaveOptions()
        {
            PlayerPrefs.SetFloat(OptionKeys.MasterVolume, MasterLevel);
            PlayerPrefs.SetFloat(OptionKeys.EffectsVolume, EffectsLevel);
            PlayerPrefs.SetFloat(OptionKeys.VoiceVolume, VoiceLevel);
            PlayerPrefs.SetFloat(OptionKeys.Sensitivity, player.sensitivity);
            PlayerPrefs.SetInt(OptionKeys.InvertY, player.invertY ? 1 : 0);
            PlayerPrefs.SetFloat(OptionKeys.FieldOfView, player.view.fieldOfView);
            PlayerPrefs.SetInt(OptionKeys.Subtitles, Subtitles ? 1 : 0);
            PlayerPrefs.SetInt(OptionKeys.Fullscreen, Fullscreen ? 1 : 0);
            PlayerPrefs.SetInt(OptionKeys.Quality, QualityLevel);
            PlayerPrefs.SetInt(OptionKeys.ResolutionWidth, ScreenSize.x);
            PlayerPrefs.SetInt(OptionKeys.ResolutionHeight, ScreenSize.y);
            PlayerPrefs.Save();
        }

        // ---------- Áudio ----------

        // Volume do controlo (0 a 1) em decibéis para a mistura (0 dB = volume original, -80 dB = silêncio).
        public static float ToDecibels(float level)
        {
            return level <= .0001f ? -80 : Mathf.Log10(level) * 20;
        }

        void ApplyVolumes()
        {
            if (mixer == null)
            {
                // Sem mistura só há o volume geral.
                AudioListener.volume = MasterLevel;
                return;
            }
            mixer.SetFloat(MasterParameter, ToDecibels(MasterLevel));
            mixer.SetFloat(EffectsParameter, ToDecibels(EffectsLevel));
            mixer.SetFloat(VoiceParameter, ToDecibels(VoiceLevel));
        }

        // ---------- Vídeo ----------

        void RefreshResolutions()
        {
            Resolutions = Screen.resolutions.Select(r => new Vector2Int(r.width, r.height)).Distinct().ToList();
            if (!Resolutions.Contains(ScreenSize))
                Resolutions.Add(ScreenSize);
            Resolutions = Resolutions.OrderBy(r => r.x).ThenBy(r => r.y).ToList();
        }

        void ApplyScreen()
        {
            Screen.SetResolution(ScreenSize.x, ScreenSize.y, Fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
        }

        public static string ResolutionLabel(Vector2Int size)
        {
            return size.x + " × " + size.y;
        }

        // Nome do nível de qualidade em português (os níveis são os do Project Settings > Quality).
        public static string QualityLabel(int level)
        {
            string name = QualitySettings.names[level];
            switch (name)
            {
                case "Very Low": return "Muito baixa";
                case "Low": return "Baixa";
                case "Medium": return "Média";
                case "High": return "Alta";
                case "Very High": return "Muito alta";
                default: return name;
            }
        }

        // ---------- Falas e legendas ----------

        // O vigilante (ou o rádio) diz a fala com este id.
        // Legendas ligadas: aparece a legenda. Desligadas: só se ouve a voz; sem som de voz, fica a mensagem de sempre
        // (com messageSuffix no fim, quando a mensagem tem mais alguma coisa a dizer).
        public void Speak(string id, string messageSuffix = "")
        {
            var line = voiceLines?.FirstOrDefault(l => l.id == id);
            if (line == null)
            {
                Debug.LogWarning("Sem fala com o id " + id);
                return;
            }
            bool hasVoice = line.clip != null && voice != null;
            if (hasVoice)
                voice.PlayOneShot(line.clip);

            string text = line.speaker + ": " + line.text;
            if (Subtitles)
            {
                subtitle = text;
                subtitleUntil = Time.unscaledTime + subtitleSeconds;
            }
            else if (!hasVoice)
                Say(text + messageSuffix);
        }

        // ---------- Ações do ecrã de Opções (ligadas no Inspector, em On Value Changed) ----------

        public void UI_SetVolume(float value) { MasterLevel = value; ApplyVolumes(); }
        public void UI_SetEffectsVolume(float value) { EffectsLevel = value; ApplyVolumes(); }
        public void UI_SetVoiceVolume(float value) { VoiceLevel = value; ApplyVolumes(); }
        public void UI_SetSensitivity(float value) => player.sensitivity = value;
        public void UI_SetFieldOfView(float value) => player.view.fieldOfView = value;
        public void UI_SetInvertY(bool value) => player.invertY = value;
        public void UI_SetSubtitles(bool value)
        {
            Subtitles = value;
            if (!value)
                subtitleUntil = 0;
        }
        public void UI_SetFullscreen(bool value) { Fullscreen = value; ApplyScreen(); }
        public void UI_SetResolution(int index)
        {
            if (index < 0 || index >= Resolutions.Count)
                return;
            ScreenSize = Resolutions[index];
            ApplyScreen();
        }
        public void UI_SetQuality(int level)
        {
            QualityLevel = Mathf.Clamp(level, 0, QualitySettings.names.Length - 1);
            QualitySettings.SetQualityLevel(QualityLevel, true);
        }
        public void UI_SaveOptions()
        {
            SaveOptions();
            Show(previous);
        }

        // Põe os controlos do ecrã de Opções com os valores atuais (chamado ao abrir o ecrã).
        void ShowOptions()
        {
            ui.volume.SetValueWithoutNotify(MasterLevel);
            ui.effectsVolume.SetValueWithoutNotify(EffectsLevel);
            ui.voiceVolume.SetValueWithoutNotify(VoiceLevel);
            ui.sensitivity.SetValueWithoutNotify(player.sensitivity);
            ui.fieldOfView.SetValueWithoutNotify(player.view.fieldOfView);
            ui.invertY.SetIsOnWithoutNotify(player.invertY);
            ui.subtitles.SetIsOnWithoutNotify(Subtitles);
            ui.fullscreen.SetIsOnWithoutNotify(Fullscreen);

            RefreshResolutions();
            ui.resolution.ClearOptions();
            ui.resolution.AddOptions(Resolutions.Select(ResolutionLabel).ToList());
            ui.resolution.SetValueWithoutNotify(Resolutions.IndexOf(ScreenSize));
            ui.resolution.RefreshShownValue();

            ui.quality.ClearOptions();
            ui.quality.AddOptions(Enumerable.Range(0, QualitySettings.names.Length).Select(QualityLabel).ToList());
            ui.quality.SetValueWithoutNotify(QualityLevel);
            ui.quality.RefreshShownValue();
        }
    }
}
