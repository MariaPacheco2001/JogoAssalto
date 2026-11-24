using System;
using UnityEngine;

namespace GolpeCalculado
{
    // Valores de um nível de dificuldade (US18). As regras do jogo são iguais nos três níveis:
    // só mudam estes números, que o HeistGame aplica ao vigilante ao começar ou carregar uma partida.
    [Serializable]
    public class DifficultySettings
    {
        public string name;
        [Tooltip("Distância de deteção do vigilante, em metros (passa para HeistGuard.visionRange).")] public float visionRange;
        [Tooltip("Segundos até o vigilante reagir a uma falha elétrica (passa para HeistGuard.powerReactionDelay).")] public float powerReactionDelay;
        [Tooltip("Mapa do diário (US17) completo desde o início. O mapa ainda não existe: ligar quando for feito, com HeistGame.FullMapFromStart.")] public bool fullMap;
    }

    // Nível de dificuldade da partida: escolha no briefing, aplicação ao vigilante e texto para a interface.
    public sealed partial class HeistGame
    {
        public enum Difficulty { Easy, Normal, Hard }

        [Header("Dificuldade (US18)")]
        [Tooltip("Valores de cada nível, por esta ordem: Fácil, Normal, Difícil. O Normal é o jogo de referência.")]
        public DifficultySettings[] difficulties =
        {
            new DifficultySettings { name = "Fácil", visionRange = 12, powerReactionDelay = 3, fullMap = true },
            new DifficultySettings { name = "Normal", visionRange = 15, powerReactionDelay = 2, fullMap = false },
            new DifficultySettings { name = "Difícil", visionRange = 18, powerReactionDelay = 1, fullMap = false },
        };

        // Nível da partida atual. Fica gravado em cada checkpoint.
        public Difficulty Level { get; private set; } = Difficulty.Normal;
        public DifficultySettings LevelSettings => difficulties[(int)Level];

        // US17: quando o mapa existir, mostra-o completo desde o início se isto for verdadeiro.
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

        // Passa os valores do nível para o vigilante. Chamado em cada novo jogo e restauro de checkpoint.
        void ApplyDifficulty()
        {
            var settings = LevelSettings;
            guard.visionRange = settings.visionRange;
            guard.powerReactionDelay = settings.powerReactionDelay;
        }

        // Novo jogo: abre o briefing com o Normal pré-selecionado.
        void ShowBriefing()
        {
            SetDifficulty(Difficulty.Normal);
            Show(Page.Briefing);
        }

        public string DifficultyDescription(Difficulty level)
        {
            var s = difficulties[(int)level];
            return s.name + ": o vigilante deteta-te até " + s.visionRange.ToString("0.#") + " m e reage a uma falha elétrica em "
                + s.powerReactionDelay.ToString("0.#") + " s.";
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
