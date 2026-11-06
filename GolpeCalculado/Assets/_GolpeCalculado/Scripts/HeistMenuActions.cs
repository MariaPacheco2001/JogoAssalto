namespace GolpeCalculado
{
    // Ações dos menus que podem fazer perder progresso: pedem sempre confirmação antes.
    public sealed partial class HeistGame
    {
        Page confirmationReturn;
        int selectedCheckpoint;

        public void RequestNewGame()
        {
            if (LatestSave() > 0)
                Confirm("novo");
            else
                Show(Page.Briefing);
        }

        public bool RequestCheckpoint(int cp)
        {
            if (!HasSave(cp))
            {
                Say("Esse checkpoint não está disponível.");
                return false;
            }
            selectedCheckpoint = cp;

            // Voltar a um checkpoint anterior, ou fazê-lo a meio do jogo, abandona progresso.
            if (Current != Page.Menu || cp < LatestSave())
            {
                Confirm("selecionar");
                return true;
            }
            return LoadCheckpoint(cp);
        }

        public void RequestRestartCheckpoint()
        {
            if (LatestSave() > 0)
                Confirm("checkpoint");
        }

        public void RequestRestartMission()
        {
            Confirm("missao");
        }

        public void RequestMainMenu()
        {
            Confirm("menu");
        }

        void Confirm(string action)
        {
            confirm = action;
            confirmationReturn = Current;
            Show(Page.Confirm);
        }

        public void CancelConfirmation()
        {
            Show(confirmationReturn);
        }

        public void AcceptConfirmation()
        {
            if (Current != Page.Confirm)
                return;

            switch (confirm)
            {
                case "novo":
                case "missao":
                    if (!ClearProgress())
                        return;
                    if (confirm == "novo")
                        Show(Page.Briefing);
                    else
                        Begin();
                    break;
                case "checkpoint":
                    LoadCheckpoint(LatestSave());
                    break;
                case "selecionar":
                    LoadCheckpoint(selectedCheckpoint);
                    break;
                default:
                    Show(Page.Menu);
                    break;
            }
        }

        // Tecla Esc.
        public void Back()
        {
            if (Current == Page.Confirm)
            {
                CancelConfirmation();
                return;
            }

            if (Current == Page.Play)
                Show(Page.Pause);
            else if (Current == Page.Pause)
                Show(Page.Play);
            else if (Current == Page.Briefing)
                Show(Page.Menu);
            else if (Current != Page.Menu && Current != Page.Victory && Current != Page.Defeat)
                Show(previous == Page.Menu ? Page.Menu : Page.Pause);
        }
    }
}
