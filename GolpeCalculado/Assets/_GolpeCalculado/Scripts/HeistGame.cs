using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GolpeCalculado
{
    [Serializable] public class ItemSnapshot { public string id; public bool used,open; public Vector3 position; }
    [Serializable] public class HeistSnapshot
    {
        public int version=1,checkpoint; public Vector3 playerPosition,vanPosition,casePosition; public Quaternion playerRotation,vanRotation;
        public bool carrying,loaded,recovered,vaultPower,vaultOpen,alarm,throwable;
        public bool[] circuits; public string[] inventory,noteKeys,noteValues; public ItemSnapshot[] items; public float seconds;
    }
    public sealed partial class HeistGame : MonoBehaviour
    {
        public PlayerMotor player; public VanMotor van; public HeistGuard guard;
        public HeistItem caseItem,vaultDoor; public Transform hideout; public Light[] circuitBLights,circuitCLights,alarmLights;
        public Vector3 guardResetPosition;
        public bool[] Circuits={true,true};
        public bool VaultPower,VaultOpen,CarryingCase,CaseLoaded,CaseRecovered,HasThrowable,Alarm;
        public bool TestMode;
        public int Checkpoint {get;private set;}
        public float ActiveSeconds {get;private set;}
        public enum Page { Menu,Briefing,Play,Pause,Notes,Options,Controls,Credits,Defeat,Victory,Checkpoints,Confirm,Keypad,Document }
        public Page Current {get;private set;}=Page.Menu;
        public bool Running=>Current==Page.Play;
        public bool InVan=>van.Occupied;
        public readonly HashSet<string> Inventory=new HashSet<string>();
        public readonly Dictionary<string,string> Notes=new Dictionary<string,string>();
        public HeistItem[] Items {get;private set;}
        string message="",documentTitle="",documentText="",pin="",confirm="";
        float messageUntil,cabinYaw,cabinPitch,stepNoise;
        Page previous=Page.Menu;
        float volume=.7f;
        public string SaveDirectory=>Path.Combine(Application.persistentDataPath,TestMode?"GolpeCalculado-Testes":"GolpeCalculado");
        public string Objective
        {
            get {
                if(CaseLoaded)return "Conduz até ao esconderijo e imobiliza a carrinha";
                if(CarryingCase)return "Leva a mala à traseira da carrinha · E para carregar";
                if(CaseRecovered)return "Recupera a mala onde a pousaste";
                if(VaultOpen)return "Recupera a mala NC-17 no arquivo de valores";
                if(VaultPower)return "Descobre o código de seis algarismos nos documentos";
                if(Has("fuse"))return "Instala o fusível na alimentação auxiliar do arquivo";
                if(Has("officeKey"))return "Explora o arquivo e procura o fusível na manutenção";
                if(Checkpoint>0)return "Procura acessos, ferramentas e documentos no armazém";
                return "Estaciona, observa a segurança e escolhe uma entrada";
            }
        }
        void Awake()
        {
            Items=FindObjectsByType<HeistItem>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            foreach(var item in Items)item.Initialize();
            volume=PlayerPrefs.GetFloat("volume",.7f);AudioListener.volume=volume;
            player.sensitivity=PlayerPrefs.GetFloat("mouseSensitivity",2);
            player.invertY=PlayerPrefs.GetInt("invertY",0)==1;
            player.view.fieldOfView=PlayerPrefs.GetFloat("fov",75);
            Show(Page.Menu);
        }
        void Start()
        {
            initialState=CaptureSnapshot(0);
            ApplyCircuits();
        }
        void OnDestroy(){Time.timeScale=1;AudioListener.pause=false;Cursor.lockState=CursorLockMode.None;Cursor.visible=true;}
        public void Show(Page page)
        {
            Current=page;bool playing=page==Page.Play;
            Time.timeScale=playing?1:0;AudioListener.pause=!playing;
            Cursor.lockState=playing&&!TestMode?CursorLockMode.Locked:CursorLockMode.None;Cursor.visible=!playing||TestMode;
        }
        public void Begin(){if(initialState!=null)ApplySnapshot(initialState);Show(Page.Play);Say("Contrato NC-17: observa o armazém. A carrinha será o teu ponto de extração.");}
        public bool Has(string id)=>Inventory.Contains(id);
        public void Take(string id){Inventory.Add(id);}
        public string KeyName(string id)=>id=="officeKey"?"a chave do escritório":id=="crowbar"?"o pé-de-cabra":id=="staffKey"?"a chave de serviço":id=="manifest"?"identificar a mala no manifesto":id;
        public void Say(string text){message=text;messageUntil=Time.unscaledTime+5;}
        public void Discover(string id,string text){if(Notes.ContainsKey(id))return;Notes.Add(id,text);Say("Nova pista no diário · J para consultar");}
        public void ReadDocument(string id,string label,string text){Take(id);Notes[id]=text;documentTitle=label;documentText=text;previous=Page.Pause;Show(Page.Document);Tone(580);}
        public void Tone(float hz)
        {
            var clip=AudioClip.Create("Resposta",2205,1,22050,false);var data=new float[2205];
            for(int i=0;i<data.Length;i++)data[i]=Mathf.Sin(2*Mathf.PI*hz*i/22050)*.12f*(1-(float)i/data.Length);
            clip.SetData(data,0);AudioSource.PlayClipAtPoint(clip,player.view.transform.position,.6f);Destroy(clip,1);
        }
        public void Noise(Vector3 position,float range){if(Vector3.Distance(guard.transform.position,position)<range)guard.Investigate(position);}
        public void ToggleCircuit(int index)
        {
            Circuits[index]=!Circuits[index];ApplyCircuits();Tone(380);
            Say(Circuits[index]?"Circuito ligado: luzes e câmara com energia.":"Circuito desligado: luzes e câmara sem energia.");
            Discover("circuit"+index,"O quadro "+(index==0?"Corredor B":"Oficina C")+" alimenta as luzes e a câmara dessa zona. Uma falha pode levar o vigilante ao quadro.");
            if(!Circuits[index]){var board=Items.FirstOrDefault(i=>i.kind==ItemKind.Power&&i.circuit==index);if(board!=null)StartCoroutine(InvestigatePower(board.transform.position));}
        }
        System.Collections.IEnumerator InvestigatePower(Vector3 point){yield return new WaitForSeconds(2);guard.Investigate(point);}
        public void ApplyCircuits(){foreach(var light in circuitBLights)light.enabled=Circuits[0];foreach(var light in circuitCLights)light.enabled=Circuits[1];foreach(var light in alarmLights)light.enabled=Alarm;}
        public void SetAlarm(bool value){Alarm=value;ApplyCircuits();if(value){Tone(250);guard.Investigate(caseItem.transform.position);Say("Alarme de reserva ativo. Procura cobertura e prepara a saída.");}}
        public void OpenKeypad(HeistItem item){pin="";previous=Page.Pause;Show(Page.Keypad);}
        public bool SubmitPin(string value)
        {
            if(value!="020713"){pin="";Say("Código incorreto. Lê a ordem de expedição e os três registos.");Tone(130);return false;}
            if(!VaultPower)return false;
            VaultOpen=true;Take("vaultAccess");vaultDoor.SetOpen(true);Show(Page.Play);Say("Arquivo aberto. Identifica e recupera a mala NC-17.");Tone(800);return true;
        }
        public void PickCase(HeistItem item)
        {
            if(CarryingCase||CaseLoaded)return;
            if(!VaultOpen){Say("O arquivo ainda está fechado.");return;}
            if(!Has("manifest")){Say("Consulta o manifesto para confirmar a referência NC-17.");return;}
            CarryingCase=true;caseItem=item;
            var col=item.GetComponent<Collider>();if(col!=null)col.enabled=false;
            item.transform.SetParent(player.view.transform,false);item.transform.localPosition=new Vector3(.42f,-.55f,.9f);item.transform.localRotation=Quaternion.identity;
            player.SpeedMultiplier=.62f;
            if(!CaseRecovered){CaseRecovered=true;SetAlarm(true);Discover("caseAlarm","O sensor da mala usa alimentação de reserva. Desligar as luzes não impede o alarme de extração.");SaveCheckpoint(3);}
            Say("Mala recuperada. Q para pousar. Não podes lançar objetos enquanto a transportas.");
        }
        public bool DropCase()
        {
            if(!CarryingCase)return false;
            var from=player.transform.position+Vector3.up*.6f;var point=from+player.transform.forward*.9f;
            if(Physics.Linecast(from,point,~0,QueryTriggerInteraction.Ignore)){Say("Sem espaço para pousar a mala.");return false;}
            if(!Physics.Raycast(point+Vector3.up,Vector3.down,out var hit,3,~0,QueryTriggerInteraction.Ignore)){Say("Procura chão livre.");return false;}
            caseItem.transform.SetParent(null);caseItem.transform.position=hit.point+Vector3.up*.23f;caseItem.transform.rotation=Quaternion.Euler(0,player.transform.eulerAngles.y,0);
            caseItem.GetComponent<Collider>().enabled=true;CarryingCase=false;player.SpeedMultiplier=1;Say("Mala pousada. Terás de voltar para a recuperar.");return true;
        }
        public bool LoadCase()
        {
            if(!CarryingCase){Say(CaseLoaded?"A mala já está carregada.":"Traz a mala até à traseira da carrinha.");return false;}
            if(Vector3.Distance(player.transform.position,van.transform.TransformPoint(new Vector3(0,0,-2.6f)))>3){Say("Aproxima-te da traseira da carrinha.");return false;}
            CarryingCase=false;CaseLoaded=true;player.SpeedMultiplier=1;caseItem.transform.SetParent(null);caseItem.gameObject.SetActive(false);Say("Mala carregada. Entra e regressa ao esconderijo, onde começaste.");Tone(900);return true;
        }
        public bool CanEnter()=>!InVan&&!CarryingCase&&van.SpeedKmh<1&&Vector3.Distance(player.transform.position,van.entry.position)<2.3f&&Physics.Raycast(player.view.transform.position,player.view.transform.forward,out var hit,2.7f,~0,QueryTriggerInteraction.Ignore)&&hit.transform.IsChildOf(van.transform);
        public bool EnterVan()
        {
            if(!CanEnter())return false;player.controlled=false;player.Body.enabled=false;
            player.view.transform.SetParent(van.seat,false);player.view.transform.localPosition=Vector3.zero;player.view.transform.localRotation=Quaternion.identity;
            cabinPitch=cabinYaw=0;van.SetOccupied(true);Say("Espaço: travão de mão. Shift: travar. E: sair quando parada.");return true;
        }
        public bool ExitVan()
        {
            if(!InVan)return false;if(van.SpeedKmh>1){Say("Para a carrinha antes de sair.");return false;}
            foreach(var marker in new[]{van.leftExit,van.rightExit})
            {
                if(!Physics.Raycast(marker.position+Vector3.up*1.5f,Vector3.down,out var ground,3,~0,QueryTriggerInteraction.Ignore)||ground.normal.y<.8f||ground.point.y>van.transform.position.y+.4f)continue;
                var feet=ground.point+Vector3.up*.06f;
                if(Physics.CheckCapsule(feet+Vector3.up*.35f,feet+Vector3.up*1.48f,.32f,~0,QueryTriggerInteraction.Ignore))continue;
                var origin=new Vector3(van.transform.position.x,feet.y,van.transform.position.z)+van.transform.forward*.9f;var direction=feet-origin;
                if(Physics.CapsuleCastAll(origin+Vector3.up*.35f,origin+Vector3.up*1.48f,.3f,direction.normalized,direction.magnitude,~0,QueryTriggerInteraction.Ignore).Any(h=>!h.transform.IsChildOf(van.transform)&&!h.transform.IsChildOf(player.transform)))continue;
                van.SetOccupied(false);player.view.transform.SetParent(player.transform,false);player.view.transform.localPosition=Vector3.up*1.65f;
                player.Teleport(feet,Quaternion.Euler(0,van.transform.eulerAngles.y+(marker==van.leftExit?-90:90),0));player.Body.enabled=true;player.controlled=true;return true;
            }
            Say("Saídas bloqueadas. Afasta a carrinha dos obstáculos.");return false;
        }
        public void Captured(){if(Current!=Page.Play||TestMode)return;Show(Page.Defeat);}
        public HeistItem Target()
        {
            foreach(var hit in Physics.RaycastAll(player.view.transform.position,player.view.transform.forward,2.7f,~0,QueryTriggerInteraction.Ignore).OrderBy(h=>h.distance))
            {
                if(hit.transform.IsChildOf(player.transform))continue;
                return hit.collider.GetComponentInParent<HeistItem>();
            }
            return null;
        }
        public bool Throw()
        {
            if(!HasThrowable||CarryingCase){Say(CarryingCase?"Pousa a mala primeiro.":"Procura uma garrafa para lançar.");return false;}
            var position=player.view.transform.position+player.view.transform.forward*.65f;
            if(Physics.CheckSphere(position,.12f,~0,QueryTriggerInteraction.Ignore)){Say("Não há espaço livre para lançar.");return false;}
            var bottle=GameObject.CreatePrimitive(PrimitiveType.Sphere);bottle.name="Distração lançada";bottle.transform.position=position;bottle.transform.localScale=Vector3.one*.16f;
            var rb=bottle.AddComponent<Rigidbody>();rb.mass=.3f;rb.collisionDetectionMode=CollisionDetectionMode.ContinuousDynamic;rb.linearVelocity=player.view.transform.forward*11+Vector3.up*2;
            bottle.AddComponent<ThrownNoise>().game=this;HasThrowable=false;return true;
        }
        void Update()
        {
            if(Input.GetKeyDown(KeyCode.Escape))Back();
            if(!Running)return;ActiveSeconds+=Time.deltaTime;
            if(!TestMode)
            {
                if(Input.GetKeyDown(KeyCode.J)){previous=Page.Pause;Show(Page.Notes);}
                if(Input.GetKeyDown(KeyCode.Q))DropCase();if(Input.GetKeyDown(KeyCode.G))Throw();
                if(Input.GetKeyDown(KeyCode.E)){if(InVan)ExitVan();else{var target=Target();if(target!=null)target.Use(this);else EnterVan();}}
                if(InVan){cabinYaw=Mathf.Clamp(cabinYaw+Input.GetAxis("Mouse X")*player.sensitivity,-100,100);cabinPitch=Mathf.Clamp(cabinPitch-Input.GetAxis("Mouse Y")*player.sensitivity*(player.invertY?-1:1),-55,65);player.view.transform.localRotation=Quaternion.Euler(cabinPitch,cabinYaw,0);}
            }
            if(!InVan)
            {
                var p=player.transform.position;
                if(Checkpoint<1&&p.x>12&&p.x<61&&p.z>0&&p.z<63)SaveCheckpoint(1);
                if(Checkpoint<2&&Has("officeKey")&&p.x>34&&p.z>25&&p.z<51)SaveCheckpoint(2);
                if(player.Body.velocity.magnitude>4&&Time.time>stepNoise){stepNoise=Time.time+.9f;Noise(p,8);}
                if(p.y<-8){Show(Page.Defeat);Say("Saíste da área segura.");}
            }
            if(CaseLoaded&&InVan&&Vector3.Distance(van.transform.position,hideout.position)<6&&van.SpeedKmh<1)Show(Page.Victory);
        }
    }
}
