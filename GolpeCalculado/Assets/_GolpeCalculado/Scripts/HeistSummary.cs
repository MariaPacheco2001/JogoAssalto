using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GolpeCalculado
{
    // Resumo final (vertente de serious game): no ecrã de vitória, o jogador vê que ligações do
    // sistema invisível percebeu e aprende, com uma explicação curta, as que lhe escaparam.
    // O painel, o título, a frase final e as cores estão no Canvas (Interface > Ecrãs > Vitória ou derrota > Resumo do sistema).
    public sealed partial class HeistGame
    {
        // As sete ligações do sistema, pela ordem do resumo. Cada uma é uma pista do diário (Notes) com este id.
        public static readonly string[] SystemLinks = { "circuit0", "circuit1", "camara", "ruido", "guardPower", "caseAlarm", "darkCrouch" };

        // O que aparece no resumo para uma ligação que o jogador não descobriu.
        static readonly Dictionary<string, string> MissedLinks = new Dictionary<string, string>
        {
            { "circuit0", "O quadro do Corredor B alimenta as luzes e a câmara dessa zona. Desligá-lo apaga-as, mas a falha leva o vigilante ao quadro." },
            { "circuit1", "O quadro da Oficina C faz o mesmo na oficina: sem energia, luzes e câmara apagam-se, e o vigilante vai ver o que se passa." },
            { "camara", "As câmaras não te apanham: avisam o vigilante da última posição onde te viram. Paredes e falta de energia cegam-nas." },
            { "ruido", "Portas metálicas e garrafas lançadas fazem barulho e desviam o vigilante para a origem do som." },
            { "guardPower", "O vigilante volta a ligar os quadros que encontra desligados: cortar a energia só dá uma janela de tempo." },
            { "caseAlarm", "O sensor da mala tem alimentação de reserva: tirá-la dispara o alarme, mesmo com as luzes desligadas." },
            { "darkCrouch", "Agachada no Corredor B com o quadro B desligado, o vigilante só te vê muito perto." },
        };

        public const string DarkCrouchNote = "Agachada no Corredor B às escuras, o vigilante só te vê muito perto.";
        const string FoundMark = "<color=#F2B047>✔</color>";
        const string MissedMark = "<color=#8C979C>○</color>";

        // Capturas desta partida (o tempo de jogo é ActiveSeconds). Gravadas nos checkpoints.
        public int Captures { get; private set; }

        public int DiscoveredLinks => SystemLinks.Count(Notes.ContainsKey);

        // Agachada dentro do Corredor B (zona do mapa) com o quadro B desligado: o vigilante vê muito menos
        // (HeistGuard.SeesPlayer usa esta mesma condição) e o diário regista a pista "darkCrouch".
        public bool CrouchedInDarkCorridor => !InVan && player.IsCrouching && !Circuits[0]
            && InMapZone(HeistGuard.DarkCorridorZone, player.transform.position);

        // Chamado a cada frame a pé. Discover só regista a pista da primeira vez.
        void CheckDarkCrouch()
        {
            if (CrouchedInDarkCorridor)
                Discover("darkCrouch", DarkCrouchNote);
        }

        public string PlayTime
        {
            get
            {
                var time = TimeSpan.FromSeconds(ActiveSeconds);
                return (int)time.TotalMinutes + ":" + time.Seconds.ToString("00");
            }
        }

        public string SummaryCountText => "Descobriste " + DiscoveredLinks + " de " + SystemLinks.Length + " ligações";

        // Uma linha por ligação: ✔ e o texto do diário, ou ○ e a explicação do que escapou.
        public string SummaryListText
        {
            get
            {
                var lines = SystemLinks.Select(id => Notes.TryGetValue(id, out string note)
                    ? FoundMark + "  " + note
                    : MissedMark + "  <color=#AEB7BB>" + MissedLinks[id] + "</color>");
                return string.Join("\n", lines);
            }
        }

        // O resumo só aparece na vitória. O ecrã de captura fica como estava.
        void ShowSystemSummary(bool won)
        {
            if (ui.summaryPanel == null)
                return;
            ui.summaryPanel.SetActive(won);
            if (!won)
                return;
            ui.summaryCount.text = SummaryCountText;
            ui.summaryList.text = SummaryListText;
        }
    }
}
