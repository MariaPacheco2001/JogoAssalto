using System;
using System.IO;
using UnityEngine;

namespace GolpeCalculado
{
    public sealed partial class HeistGame
    {
        GUIStyle title,body,small,button,big;
        Vector2 scroll;
        void Styles()
        {
            if(title!=null)return;
            title=new GUIStyle(GUI.skin.label){fontSize=38,fontStyle=FontStyle.Bold,wordWrap=true,normal={textColor=Color.white}};
            big=new GUIStyle(title){fontSize=27};body=new GUIStyle(GUI.skin.label){fontSize=19,wordWrap=true,normal={textColor=new Color(.9f,.93f,.94f)}};
            small=new GUIStyle(body){fontSize=14};button=new GUIStyle(GUI.skin.button){fontSize=18,alignment=TextAnchor.MiddleLeft,padding=new RectOffset(18,12,8,8)};
        }
        void Panel(Rect r,Color c){var old=GUI.color;GUI.color=c;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=old;}
        bool Button(string text,float y,bool enabled=true){GUI.enabled=enabled;bool pressed=GUI.Button(new Rect(65,y,460,43),text,button);GUI.enabled=true;return pressed;}
        void Heading(string text){GUI.Label(new Rect(65,95,1100,85),text,title);}
        void OnGUI()
        {
            Styles();var matrix=GUI.matrix;GUI.matrix=Matrix4x4.Scale(new Vector3(Screen.width/1280f,Screen.height/720f,1));
            Color amber=new Color(.96f,.71f,.31f);
            if(Current==Page.Play)
            {
                Panel(new Rect(25,24,650,103),new Color(.025f,.04f,.05f,.88f));GUI.color=amber;GUI.Label(new Rect(43,35,615,24),CaseLoaded||CaseRecovered?"03 / EXTRAÇÃO":Checkpoint>0?"02 / INFILTRAÇÃO":"01 / APROXIMAÇÃO",small);GUI.color=Color.white;
                GUI.Label(new Rect(43,64,610,55),Objective,body);GUI.Label(new Rect(1060,26,200,28),"J Diário   ·   Esc Pausa",small);
                GUI.Label(new Rect(635,347,20,28),"·",body);
                var target=!InVan?Target():null;string prompt=InVan?"E Sair · Espaço Travão de mão":target!=null?"E "+target.Prompt:CanEnter()?"E Entrar na carrinha":"";
                if(prompt.Length>0){Panel(new Rect(355,580,570,44),new Color(.025f,.04f,.05f,.92f));GUI.Label(new Rect(371,589,535,30),prompt,body);}
                GUI.Label(new Rect(28,686,950,26),(CarryingCase?"Q Pousar mala  ·  ":"")+(HasThrowable?"G Lançar garrafa  ·  ":"")+"Pistas: "+Notes.Count+"  ·  Itens e registos: "+Inventory.Count,small);
                if(InVan){Panel(new Rect(1010,580,245,113),new Color(.025f,.04f,.05f,.9f));GUI.Label(new Rect(1030,590,220,45),Mathf.RoundToInt(van.SpeedKmh)+" km/h",big);GUI.Label(new Rect(1030,641,220,30),van.Handbrake?"TRAVÃO DE MÃO ATIVO":"SHIFT TRAVAR",small);}
                if(Alarm){GUI.color=new Color(1,.35f,.25f);GUI.Label(new Rect(970,70,290,30),"ALARME DE RESERVA ATIVO",small);GUI.color=Color.white;}
            }
            else
            {
                Panel(new Rect(0,0,1280,720),new Color(.018f,.033f,.043f,.96f));GUI.color=amber;GUI.Label(new Rect(65,40,900,26),"GOLPE CALCULADO / CONTRATO NC-17",small);GUI.color=Color.white;
                switch(Current)
                {
                    case Page.Menu:
                        Heading("O que não vês também conta.");
                        GUI.Label(new Rect(660,205,520,220),"Recupera uma mala num armazém vigiado. Explora os acessos, encontra ferramentas e documentos e descobre como a segurança está ligada.\n\nA extração começa quando pensas que o trabalho acabou.",body);
                        if(Button("Novo jogo",200))RequestNewGame();
                        if(Button("Continuar",250,LatestSave()>0))LoadCheckpoint(LatestSave());
                        if(Button("Selecionar checkpoint",300,LatestSave()>0)){previous=Page.Menu;Show(Page.Checkpoints);}
                        if(Button("Opções",350)){previous=Page.Menu;Show(Page.Options);}
                        if(Button("Controlos",400)){previous=Page.Menu;Show(Page.Controls);}
                        if(Button("Créditos",450)){previous=Page.Menu;Show(Page.Credits);}
                        if(Button("Sair",500)){
                            #if UNITY_EDITOR
                            UnityEditor.EditorApplication.isPlaying=false;
                            #else
                            Application.Quit();
                            #endif
                        }
                        GUI.Label(new Rect(660,485,500,100),"Missão em desenvolvimento · PC e Mac\nA duração de cerca de 12 minutos é uma meta que será medida com jogadores novos.",small);
                        break;
                    case Page.Briefing:
                        Heading("Uma mala. Nenhum atalho óbvio.");
                        GUI.Label(new Rect(65,200,1100,340),"O contacto quer a mala NC-17, guardada no arquivo de valores da logística. Tens uma carrinha e a noite pela frente.\n\nO armazém tem uma entrada de cargas e uma porta de serviço. O pessoal deixa documentos espalhados; as regras dos circuitos e dos códigos estão lá, se souberes procurar.\n\nO vigilante reage ao que vê e ouve. Se fores visto, foge e quebra a linha de visão. A captura termina a tentativa.\n\nEntrega a mala no esconderijo onde começaste. O diário (J) guarda o que descobrires.",body);
                        if(Button("Começar o assalto",580))Begin();
                        break;
                    case Page.Pause:
                        Heading("Pausa");
                        if(Button("Retomar",190))Show(Page.Play);
                        if(Button("Objetivos e pistas",240)){previous=Page.Pause;Show(Page.Notes);}
                        if(Button("Controlos",290)){previous=Page.Pause;Show(Page.Controls);}
                        if(Button("Opções",340)){previous=Page.Pause;Show(Page.Options);}
                        if(Button("Recomeçar do checkpoint",390,LatestSave()>0))RequestRestartCheckpoint();
                        if(Button("Recomeçar missão",440))RequestRestartMission();
                        if(Button("Voltar ao menu",490))RequestMainMenu();
                        GUI.Label(new Rect(660,220,500,170),Objective,body);
                        break;
                    case Page.Notes:
                        Heading("O que já sabes");GUI.Label(new Rect(65,181,1080,65),Objective,body);
                        scroll=GUI.BeginScrollView(new Rect(65,255,1130,315),scroll,new Rect(0,0,1080,Mathf.Max(310,Notes.Count*180)));
                        int n=0;foreach(var entry in Notes){GUI.Label(new Rect(0,n*180,1070,172),entry.Value,body);n++;}
                        if(Notes.Count==0)GUI.Label(new Rect(0,0,1050,80),"Ainda não registaste pistas. Observa etiquetas e lê documentos no local.",body);GUI.EndScrollView();
                        if(Button("Voltar",600))Show(Page.Pause);
                        break;
                    case Page.Document:
                        Heading(documentTitle);GUI.Label(new Rect(65,210,1070,310),documentText,body);GUI.Label(new Rect(65,545,1000,30),"Guardado no diário. Podes consultar esta informação com J.",small);
                        if(Button("Guardar documento e continuar",600))Show(Page.Play);
                        break;
                    case Page.Keypad:
                        Heading("Arquivo de valores");GUI.Label(new Rect(65,195,1100,95),"Código de seis algarismos. O aviso junto do terminal indica a ordem dos registos.",body);
                        pin=GUI.TextField(new Rect(65,310,400,62),pin,6,new GUIStyle(GUI.skin.textField){fontSize=34});
                        if(Button("Confirmar código",420))SubmitPin(pin);
                        if(Button("Voltar ao armazém",475))Show(Page.Play);
                        break;
                    case Page.Controls:
                        Heading("Controlos");GUI.Label(new Rect(65,195,1100,365),"A PÉ\nWASD andar · Rato olhar · Shift correr · Ctrl agachar · Espaço saltar\nE interagir / ler / apanhar / abrir · Q pousar a mala · G lançar garrafa\nJ diário de pistas · Esc pausa\n\nCARRINHA\nW acelerar · S travar e marcha-atrás · A/D direção\nShift travão de serviço · Espaço alternar travão de mão\nE sair quando parada · Carrega a mala na traseira antes de entrar.\n\nMENUS\nRato e botão esquerdo. Escreve os seis algarismos no terminal.",body);
                        if(Button("Voltar",600))Show(previous);
                        break;
                    case Page.Options:
                        Heading("Opções");
                        GUI.Label(new Rect(65,205,380,30),"Volume geral",body);volume=GUI.HorizontalSlider(new Rect(490,219,550,25),volume,0,1);AudioListener.volume=volume;
                        GUI.Label(new Rect(65,280,380,30),"Sensibilidade do rato",body);player.sensitivity=GUI.HorizontalSlider(new Rect(490,294,550,25),player.sensitivity,.3f,5);
                        GUI.Label(new Rect(65,355,380,30),"Campo de visão",body);player.view.fieldOfView=GUI.HorizontalSlider(new Rect(490,369,550,25),player.view.fieldOfView,60,100);
                        player.invertY=GUI.Toggle(new Rect(65,430,500,32),player.invertY," Inverter olhar vertical",body);
                        if(Button("Guardar e voltar",600)){PlayerPrefs.SetFloat("volume",volume);PlayerPrefs.SetFloat("mouseSensitivity",player.sensitivity);PlayerPrefs.SetInt("invertY",player.invertY?1:0);PlayerPrefs.SetFloat("fov",player.view.fieldOfView);PlayerPrefs.Save();Show(previous);}
                        break;
                    case Page.Credits:
                        Heading("Rita e Maria");GUI.Label(new Rect(65,205,1070,295),"Golpe Calculado\nProjeto universitário — Invisible Systems\n\nMotor: Unity 6000.6.5f1\nModelos, materiais e áudio provisórios construídos no projeto.\nNenhum dos assets candidatos da Asset Store foi integrado nesta versão.\n\nNomes completos e números de estudante: a preencher pela equipa.",body);
                        if(Button("Voltar",600))Show(previous);
                        break;
                    case Page.Checkpoints:
                        Heading("Checkpoints");
                        for(int i=1;i<=3;i++)if(Button("CP"+i+" — "+(i==1?"Entrada":i==2?"Interior":"Mala recuperada"),190+i*60,HasSave(i)))RequestCheckpoint(i);
                        if(Button("Voltar",600))Show(previous);
                        break;
                    case Page.Confirm:
                        Heading("Confirmar alteração de progresso");GUI.Label(new Rect(65,210,1080,160),confirm=="novo"||confirm=="missao"?"Começar de novo substitui a partida e os checkpoints guardados.":"Vais abandonar o progresso desde o último checkpoint. Queres continuar?",body);
                        if(Button("Confirmar",430))AcceptConfirmation();
                        if(Button("Cancelar",485))CancelConfirmation();
                        break;
                    case Page.Defeat: case Page.Victory:
                        Heading(Current==Page.Victory?"Entrega confirmada.":"Foste capturado.");
                        GUI.Label(new Rect(65,210,1060,150),Current==Page.Victory?"A mala NC-17 chegou ao esconderijo. O contrato está concluído.\nTempo ativo: "+TimeSpan.FromSeconds(ActiveSeconds).ToString(@"mm\:ss"):"O vigilante apanhou-te. Usa o que descobriste para alterar o plano.",body);
                        if(Current==Page.Defeat&&Button("Recomeçar do checkpoint",410,LatestSave()>0))LoadCheckpoint(LatestSave());
                        if(Button("Voltar ao menu",475))Show(Page.Menu);
                        break;
                }
            }
            if(Time.unscaledTime<messageUntil){Panel(new Rect(25,636,900,47),new Color(.015f,.025f,.03f,.94f));GUI.Label(new Rect(40,644,868,36),message,small);}
            GUI.matrix=matrix;
        }
    }
}
