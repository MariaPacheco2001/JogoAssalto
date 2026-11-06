using System;
using UnityEngine;
using UnityEngine.UI;

namespace GolpeCalculado
{
    // Ligação entre o HeistGame e a interface feita no editor (objeto "Interface" na cena).
    // Os ecrãs, botões, textos fixos e cores estão no Canvas; aqui só se mostra o ecrã certo
    // e se atualizam os textos que mudam durante o jogo.
    [Serializable] public class HeistInterface
    {
        [Header("Ecrãs")]
        public GameObject hud;
        public GameObject screens;
        public GameObject menu, briefing, pause, notes, document, keypad, controls, options, credits, checkpoints, confirm, end;
        [Header("HUD")]
        public Text stage; public Text objective; public GameObject promptPanel; public Text prompt;
        public Text status; public GameObject vanPanel; public Text speed; public Text handbrake; public GameObject alarm;
        [Header("Mensagens")]
        public GameObject messagePanel; public Text message;
        [Header("Menu e pausa")]
        public Button continueButton, selectCheckpointButton, pauseRestartCheckpointButton;
        public Text pauseObjective;
        [Header("Diário")]
        public Text notesObjective; public Text notesList; public ScrollRect notesScroll;
        [Header("Documento")]
        public Text documentTitle; public Text documentBody;
        [Header("Terminal")]
        public InputField pin;
        [Header("Opções")]
        public Slider volume, sensitivity, fieldOfView; public Toggle invertY;
        [Header("Checkpoints")]
        public Button[] checkpointButtons=new Button[3];
        [Header("Confirmação e fim")]
        public Text confirmBody; public Text endTitle; public Text endBody; public Button endRestartButton;
    }

    public sealed partial class HeistGame
    {
        public HeistInterface ui = new HeistInterface();
        Page shownPage = (Page)(-1);

        void LateUpdate()
        {
            if (ui.hud == null)
                return;

            if (Current != shownPage)
            {
                shownPage = Current;
                OnPageShown();
            }

            bool playing = Current == Page.Play;
            ui.hud.SetActive(playing);
            ui.screens.SetActive(!playing);
            if (playing)
                RefreshHud();
            else if (Current == Page.Pause)
                ui.pauseObjective.text = Objective;
            else if (Current == Page.Notes)
                ui.notesObjective.text = Objective;

            bool showMessage = Time.unscaledTime < messageUntil;
            ui.messagePanel.SetActive(showMessage);
            if (showMessage)
                ui.message.text = message;
        }

        void RefreshHud()
        {
            if (CaseLoaded || CaseRecovered)
                ui.stage.text = "03 / EXTRAÇÃO";
            else if (Checkpoint > 0)
                ui.stage.text = "02 / INFILTRAÇÃO";
            else
                ui.stage.text = "01 / APROXIMAÇÃO";
            ui.objective.text = Objective;

            string prompt = ActionPrompt();
            ui.promptPanel.SetActive(prompt.Length > 0);
            ui.prompt.text = prompt;

            string status = "";
            if (CarryingCase)
                status += "Q Pousar mala  ·  ";
            if (HasThrowable)
                status += "G Lançar garrafa  ·  ";
            status += "Pistas: " + Notes.Count + "  ·  Itens e registos: " + Inventory.Count;
            ui.status.text = status;

            ui.vanPanel.SetActive(InVan);
            if (InVan)
            {
                ui.speed.text = Mathf.RoundToInt(van.SpeedKmh) + " km/h";
                ui.handbrake.text = van.Handbrake ? "TRAVÃO DE MÃO ATIVO" : "SHIFT TRAVAR";
            }
            ui.alarm.SetActive(Alarm);
        }

        // O que a tecla E faz neste momento.
        string ActionPrompt()
        {
            if (InVan)
                return "E Sair · Espaço Travão de mão";
            var target = Target();
            if (target != null)
                return "E " + target.Prompt;
            if (CanEnter())
                return "E Entrar na carrinha";
            return "";
        }

        GameObject PanelFor(Page page)
        {
            switch (page)
            {
                case Page.Menu: return ui.menu;
                case Page.Briefing: return ui.briefing;
                case Page.Pause: return ui.pause;
                case Page.Notes: return ui.notes;
                case Page.Document: return ui.document;
                case Page.Keypad: return ui.keypad;
                case Page.Controls: return ui.controls;
                case Page.Options: return ui.options;
                case Page.Credits: return ui.credits;
                case Page.Checkpoints: return ui.checkpoints;
                case Page.Confirm: return ui.confirm;
                case Page.Victory:
                case Page.Defeat: return ui.end;
                default: return null;
            }
        }

        // Chamado uma vez sempre que o ecrã muda: mostra o painel certo e prepara o conteúdo.
        void OnPageShown()
        {
            var panels = new[] { ui.menu, ui.briefing, ui.pause, ui.notes, ui.document, ui.keypad, ui.controls, ui.options, ui.credits, ui.checkpoints, ui.confirm, ui.end };
            foreach (var panel in panels)
                panel.SetActive(false);
            var shown = PanelFor(Current);
            if (shown != null)
                shown.SetActive(true);

            int latest = LatestSave();
            switch (Current)
            {
                case Page.Menu:
                    ui.continueButton.interactable = latest > 0;
                    ui.selectCheckpointButton.interactable = latest > 0;
                    break;
                case Page.Pause:
                    ui.pauseRestartCheckpointButton.interactable = latest > 0;
                    break;
                case Page.Notes:
                    if (Notes.Count == 0)
                        ui.notesList.text = "Ainda não registaste pistas. Observa etiquetas e lê documentos no local.";
                    else
                        ui.notesList.text = string.Join("\n\n", Notes.Values);
                    ui.notesScroll.verticalNormalizedPosition = 1;
                    break;
                case Page.Document:
                    ui.documentTitle.text = documentTitle;
                    ui.documentBody.text = documentText;
                    break;
                case Page.Keypad:
                    ui.pin.SetTextWithoutNotify(pin);
                    ui.pin.ActivateInputField();
                    break;
                case Page.Options:
                    ui.volume.SetValueWithoutNotify(volume);
                    ui.sensitivity.SetValueWithoutNotify(player.sensitivity);
                    ui.fieldOfView.SetValueWithoutNotify(player.view.fieldOfView);
                    ui.invertY.SetIsOnWithoutNotify(player.invertY);
                    break;
                case Page.Checkpoints:
                    for (int i = 0; i < 3; i++)
                        ui.checkpointButtons[i].interactable = HasSave(i + 1);
                    break;
                case Page.Confirm:
                    bool restart = confirm == "novo" || confirm == "missao";
                    ui.confirmBody.text = restart
                        ? "Começar de novo substitui a partida e os checkpoints guardados."
                        : "Vais abandonar o progresso desde o último checkpoint. Queres continuar?";
                    break;
                case Page.Victory:
                case Page.Defeat:
                    ShowEnd(latest);
                    break;
            }
        }

        void ShowEnd(int latestSave)
        {
            bool won = Current == Page.Victory;
            if (won)
            {
                string time = TimeSpan.FromSeconds(ActiveSeconds).ToString(@"mm\:ss");
                ui.endTitle.text = "Entrega confirmada.";
                ui.endBody.text = "A mala NC-17 chegou ao esconderijo. O contrato está concluído.\nTempo ativo: " + time;
            }
            else
            {
                ui.endTitle.text = "Foste capturado.";
                ui.endBody.text = "O vigilante apanhou-te. Usa o que descobriste para alterar o plano.";
            }
            ui.endRestartButton.gameObject.SetActive(!won);
            ui.endRestartButton.interactable = latestSave > 0;
        }

        // Ações dos botões (ligadas no Inspector, em On Click).
        public void UI_NewGame()=>RequestNewGame();
        public void UI_Continue()=>LoadCheckpoint(LatestSave());
        public void UI_StartHeist()=>Begin();
        public void UI_Resume()=>Show(Page.Play);
        public void UI_ShowPause()=>Show(Page.Pause);
        public void UI_ShowMenu()=>Show(Page.Menu);
        public void UI_Back()=>Show(previous);
        public void UI_OpenCheckpoints(){previous=Current;Show(Page.Checkpoints);}
        public void UI_OpenOptions(){previous=Current;Show(Page.Options);}
        public void UI_OpenControls(){previous=Current;Show(Page.Controls);}
        public void UI_OpenCredits(){previous=Current;Show(Page.Credits);}
        public void UI_OpenNotes(){previous=Page.Pause;Show(Page.Notes);}
        public void UI_Checkpoint(int cp)=>RequestCheckpoint(cp);
        public void UI_RestartCheckpoint()=>RequestRestartCheckpoint();
        public void UI_RestartMission()=>RequestRestartMission();
        public void UI_MainMenu()=>RequestMainMenu();
        public void UI_Confirm()=>AcceptConfirmation();
        public void UI_Cancel()=>CancelConfirmation();
        public void UI_RestartFromDefeat()=>LoadCheckpoint(LatestSave());
        public void UI_SubmitPin(){pin=ui.pin.text;SubmitPin(pin);ui.pin.SetTextWithoutNotify(pin);}
        public void UI_SetVolume(float value){volume=value;AudioListener.volume=value;}
        public void UI_SetSensitivity(float value)=>player.sensitivity=value;
        public void UI_SetFieldOfView(float value)=>player.view.fieldOfView=value;
        public void UI_SetInvertY(bool value)=>player.invertY=value;
        public void UI_SaveOptions()
        {
            PlayerPrefs.SetFloat("volume",volume);PlayerPrefs.SetFloat("mouseSensitivity",player.sensitivity);
            PlayerPrefs.SetInt("invertY",player.invertY?1:0);PlayerPrefs.SetFloat("fov",player.view.fieldOfView);PlayerPrefs.Save();Show(previous);
        }
        public void UI_Quit()
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying=false;
            #else
            Application.Quit();
            #endif
        }
    }
}
