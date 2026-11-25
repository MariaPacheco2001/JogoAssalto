using System;
using UnityEngine;

namespace GolpeCalculado
{
    // Valores de um nível de dificuldade (US18). As regras do jogo são iguais nos três níveis:
    // só mudam estes números, que o HeistGame aplica ao vigilante e às câmaras ao começar ou carregar uma partida.
    [Serializable]
    public class DifficultySettings
    {
        public string name;
        [Tooltip("Distância de deteção do vigilante, em metros (passa para HeistGuard.visionRange).")] public float visionRange;
        [Tooltip("Segundos até o vigilante reagir a uma falha elétrica (passa para HeistGuard.powerReactionDelay).")] public float powerReactionDelay;
        [Tooltip("Mapa do diário (US17) com todas as zonas reveladas desde o início (HeistGame.FullMapFromStart).")] public bool fullMap;
        [Tooltip("Segundos a ver o jogador até uma câmara avisar o vigilante (passa para HeistCamera.exposureToReport, em todas as câmaras).")] public float cameraReportTime;
        [Tooltip("Velocidade do vigilante a perseguir, em m/s (passa para HeistGuard.chaseSpeed). Nunca passa de HeistGame.MaxChaseSpeed.")] public float chaseSpeed;
    }

    // Nível de dificuldade da partida: escolha no briefing, aplicação ao vigilante e texto para a interface.
    public sealed partial class HeistGame
    {
        public enum Difficulty { Easy, Normal, Hard }

        [Header("Dificuldade (US18)")]
        [Tooltip("Valores de cada nível, por esta ordem: Fácil, Normal, Difícil. O Normal é o jogo de referência.")]
        public DifficultySettings[] difficulties =
        {
            new DifficultySettings { name = "Fácil", visionRange = 12, powerReactionDelay = 3, fullMap = true, cameraReportTime = 1.2f, chaseSpeed = 3.8f },
            new DifficultySettings { name = "Normal", visionRange = 15, powerReactionDelay = 2, fullMap = false, cameraReportTime = .7f, chaseSpeed = 4.3f },
            new DifficultySettings { name = "Difícil", visionRange = 18, powerReactionDelay = 1, fullMap = false, cameraReportTime = .4f, chaseSpeed = 4.8f },
        };

        // O jogador a correr anda a 5,5 m/s: a perseguição nunca passa disto, para a fuga continuar possível.
        public const float MaxChaseSpeed = 4.8f;

        // Nível da partida atual. Fica gravado em cada checkpoint.
        public Difficulty Level { get; private set; } = Difficulty.Normal;
        public DifficultySettings LevelSettings => difficulties[(int)Level];

        // US17: com isto verdadeiro, o mapa do diário mostra todas as zonas desde o início (ver HeistMap).
        public bool FullMapFromStart => LevelSettings.fullMap;

        static bool ValidDifficulty(int level)
        {
            return level >= (int)Difficulty.Easy && level <= (int)Difficulty.Hard;
        }

        public void SetDifficulty(Difficulty level)
        {
            Level = level;
            ApplyDifficulty();
        }

        // Passa os valores do nível para os vigilantes e para todas as câmaras. Chamado em cada novo jogo e restauro de checkpoint.
        void ApplyDifficulty()
        {
            var settings = LevelSettings;
            bool hard = Level == Difficulty.Hard;

            // No Difícil entra o segundo vigilante; os dois usam os mesmos valores.
            if (secondGuard != null)
                secondGuard.gameObject.SetActive(hard);
            foreach (var g in Guards)
            {
                g.visionRange = settings.visionRange;
                g.powerReactionDelay = settings.powerReactionDelay;
                g.chaseSpeed = Mathf.Min(settings.chaseSpeed, MaxChaseSpeed);
            }

            // Aspeto que muda com o nível (carrinha do Difícil).
            foreach (var visual in FindObjectsByType<DifficultyVisual>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                visual.Apply(hard);
            foreach (var camera in FindObjectsByType<HeistCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                camera.exposureToReport = settings.cameraReportTime;
        }

        // Novo jogo: abre o briefing com o Normal pré-selecionado.
        void ShowBriefing()
        {
            SetDifficulty(Difficulty.Normal);
            Show(Page.Briefing);
        }

        // Linha do briefing. Câmaras e perseguição são descritas em comparação com o Normal.
        public string DifficultyDescription(Difficulty level)
        {
            var s = difficulties[(int)level];
            var normal = difficulties[(int)Difficulty.Normal];
            string text = s.name + ": deteção a " + s.visionRange.ToString("0.#") + " m, quadros em " + s.powerReactionDelay.ToString("0.#") + " s";
            if (Mathf.Approximately(s.cameraReportTime, normal.cameraReportTime) && Mathf.Approximately(s.chaseSpeed, normal.chaseSpeed))
                return text + ", câmaras a avisar em " + s.cameraReportTime.ToString("0.#") + " s e perseguição a " + s.chaseSpeed.ToString("0.#") + " m/s.";
            string cameras = s.cameraReportTime > normal.cameraReportTime ? "câmaras mais lentas a avisar"
                : s.cameraReportTime < normal.cameraReportTime ? "câmaras mais rápidas a avisar" : "câmaras normais";
            string chase = s.chaseSpeed < normal.chaseSpeed ? "vigilante mais lento a perseguir"
                : s.chaseSpeed > normal.chaseSpeed ? "vigilante mais rápido a perseguir" : "perseguição normal";
            return text + ", " + cameras + " e " + chase + ".";
        }

        // Botões Fácil, Normal e Difícil do briefing (On Click, com o número do nível: 0, 1 ou 2).
        public void UI_SetDifficulty(int level)
        {
            if (ValidDifficulty(level))
                SetDifficulty((Difficulty)level);
        }

        // O botão do nível escolhido fica desativado (cor "Disabled" do botão, no Inspector) e o texto explica o nível.
        void RefreshDifficultyChoice()
        {
            for (int i = 0; i < ui.difficultyButtons.Length; i++)
                ui.difficultyButtons[i].interactable = i != (int)Level;
            ui.difficultyDetails.text = DifficultyDescription(Level);
        }
    }
}
