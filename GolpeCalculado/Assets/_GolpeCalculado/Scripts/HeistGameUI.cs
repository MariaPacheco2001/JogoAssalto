using System;
using System.Collections.Generic;
using System.Linq;
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
        public HeistInterface ui=new HeistInterface();
        Page shownPage=(Page)(-1);

        void LateUpdate()
        {
            if(ui.hud==null)return;
            if(Current!=shownPage){shownPage=Current;OnPageShown();}
            bool playing=Current==Page.Play;
            ui.hud.SetActive(playing);ui.screens.SetActive(!playing);
            if(playing)RefreshHud();
            else if(Current==Page.Pause)ui.pauseObjective.text=Objective;
            else if(Current==Page.Notes)ui.notesObjective.text=Objective;
            bool showMessage=Time.unscaledTime<messageUntil;
            ui.messagePanel.SetActive(showMessage);if(showMessage)ui.message.text=message;
        }

        void RefreshHud()
        {
            ui.stage.text=CaseLoaded||CaseRecovered?"03 / EXTRAÇÃO":Checkpoint>0?"02 / INFILTRAÇÃO":"01 / APROXIMAÇÃO";
            ui.objective.text=Objective;
            var target=!InVan?Target():null;
            string text=InVan?"E Sair · Espaço Travão de mão":target!=null?"E "+target.Prompt:CanEnter()?"E Entrar na carrinha":"";
            ui.promptPanel.SetActive(text.Length>0);ui.prompt.text=text;
            ui.status.text=(CarryingCase?"Q Pousar mala  ·  ":"")+(HasThrowable?"G Lançar garrafa  ·  ":"")+"Pistas: "+Notes.Count+"  ·  Itens e registos: "+Inventory.Count;
            ui.vanPanel.SetActive(InVan);
            if(InVan){ui.speed.text=Mathf.RoundToInt(van.SpeedKmh)+" km/h";ui.handbrake.text=van.Handbrake?"TRAVÃO DE MÃO ATIVO":"SHIFT TRAVAR";}
            ui.alarm.SetActive(Alarm);
        }

        void OnPageShown()
        {
            var pages=new Dictionary<Page,GameObject>{
                {Page.Menu,ui.menu},{Page.Briefing,ui.briefing},{Page.Pause,ui.pause},{Page.Notes,ui.notes},{Page.Document,ui.document},
                {Page.Keypad,ui.keypad},{Page.Controls,ui.controls},{Page.Options,ui.options},{Page.Credits,ui.credits},
                {Page.Checkpoints,ui.checkpoints},{Page.Confirm,ui.confirm},{Page.Victory,ui.end},{Page.Defeat,ui.end}};
            foreach(var page in pages.Values.Distinct())page.SetActive(false);
            if(pages.TryGetValue(Current,out var shown))shown.SetActive(true);
            int latest=LatestSave();
            switch(Current)
            {
                case Page.Menu:ui.continueButton.interactable=latest>0;ui.selectCheckpointButton.interactable=latest>0;break;
                case Page.Pause:ui.pauseRestartCheckpointButton.interactable=latest>0;break;
                case Page.Notes:
                    ui.notesList.text=Notes.Count==0?"Ainda não registaste pistas. Observa etiquetas e lê documentos no local.":string.Join("\n\n",Notes.Values);
                    ui.notesScroll.verticalNormalizedPosition=1;break;
                case Page.Document:ui.documentTitle.text=documentTitle;ui.documentBody.text=documentText;break;
                case Page.Keypad:ui.pin.SetTextWithoutNotify(pin);ui.pin.ActivateInputField();break;
                case Page.Options:
                    ui.volume.SetValueWithoutNotify(volume);ui.sensitivity.SetValueWithoutNotify(player.sensitivity);
                    ui.fieldOfView.SetValueWithoutNotify(player.view.fieldOfView);ui.invertY.SetIsOnWithoutNotify(player.invertY);break;
                case Page.Checkpoints:for(int i=0;i<3;i++)ui.checkpointButtons[i].interactable=HasSave(i+1);break;
                case Page.Confirm:
                    ui.confirmBody.text=confirm=="novo"||confirm=="missao"?"Começar de novo substitui a partida e os checkpoints guardados.":"Vais abandonar o progresso desde o último checkpoint. Queres continuar?";break;
                case Page.Victory:case Page.Defeat:
                    bool won=Current==Page.Victory;
                    ui.endTitle.text=won?"Entrega confirmada.":"Foste capturado.";
                    ui.endBody.text=won?"A mala NC-17 chegou ao esconderijo. O contrato está concluído.\nTempo ativo: "+TimeSpan.FromSeconds(ActiveSeconds).ToString(@"mm\:ss"):"O vigilante apanhou-te. Usa o que descobriste para alterar o plano.";
                    ui.endRestartButton.gameObject.SetActive(!won);ui.endRestartButton.interactable=latest>0;break;
            }
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
