using UnityEngine;

namespace GolpeCalculado
{
    public enum ItemKind { Door, Key, Tool, Fuse, Document, Power, FuseSocket, Keypad, Case, Load, Throwable, Crate, AlarmReset }
    public sealed class HeistItem : MonoBehaviour
    {
        public string id, label, text, requires;
        public ItemKind kind;
        public bool used, opened, noisy;
        public Vector3 closedPosition, openPosition;
        public Quaternion closedRotation;
        public bool canSlide;
        public int circuit;
        [Tooltip("Quadro elétrico: o que liga e desliga com este quadro (luzes, câmara). Recebe true quando o circuito tem energia.")]
        public UnityEngine.Events.UnityEvent<bool> onSwitched=new UnityEngine.Events.UnityEvent<bool>();
        [Tooltip("Alcance do ruído que este objeto faz ao ser usado (portas ruidosas, caixa de fusíveis, caixa empurrável).")]
        public float noiseRange;
        [Header("Animação da porta")]
        [Tooltip("Duração da abertura/fecho em segundos (0 = instantâneo).")] public float openDuration=.6f;
        public AnimationCurve openCurve=AnimationCurve.EaseInOut(0,0,1,1);
        float animStart=-1; Vector3 animFromPosition; Quaternion animFromRotation;
        public string Prompt => kind == ItemKind.Door ? (opened ? "Fechar " : "Abrir ") + label : label;
        public void Initialize() { closedPosition=transform.position; closedRotation=transform.rotation; }
        Vector3 TargetPosition=>opened?openPosition:closedPosition;
        Quaternion TargetRotation=>closedRotation*Quaternion.Euler(0,opened?100:0,0);
        // Sem animação: usado ao restaurar checkpoints e nos testes.
        public void SetOpen(bool value)=>SetOpen(value,false);
        public void SetOpen(bool value,bool animate)
        {
            opened=value;
            if(animate&&openDuration>0){animStart=Time.time;animFromPosition=transform.position;animFromRotation=transform.rotation;}
            else{animStart=-1;if(canSlide)transform.position=TargetPosition;else transform.rotation=TargetRotation;}
            var obstacle=GetComponent<UnityEngine.AI.NavMeshObstacle>();
            if(obstacle!=null) obstacle.enabled=!value;
        }
        void Update()
        {
            if(animStart<0)return;
            float t=Mathf.Clamp01((Time.time-animStart)/openDuration),k=openCurve.Evaluate(t);
            if(canSlide)transform.position=Vector3.LerpUnclamped(animFromPosition,TargetPosition,k);
            else transform.rotation=Quaternion.SlerpUnclamped(animFromRotation,TargetRotation,k);
            if(t>=1)animStart=-1;
        }
        public void Use(HeistGame game)
        {
            if(!string.IsNullOrEmpty(requires) && !game.Has(requires)) { game.Say("Precisas de " + game.KeyName(requires) + "."); game.Tone(150); return; }
            switch(kind)
            {
                case ItemKind.Door:
                    SetOpen(!opened,true); game.Tone(noisy?180:340);
                    if(noisy) { game.Noise(transform.position,noiseRange); game.Discover("ruido","As portas metálicas e os objetos lançados desviam a ronda para a origem do som."); }
                    break;
                case ItemKind.Key: case ItemKind.Tool: case ItemKind.Fuse:
                    if(used)return; used=true; game.Take(id); game.Say("Recolhido: "+label); game.Tone(680); gameObject.SetActive(false); break;
                case ItemKind.Document:
                    game.ReadDocument(id,label,text); break;
                case ItemKind.Power:
                    game.ToggleCircuit(circuit); break;
                case ItemKind.FuseSocket:
                    if(game.VaultPower) { game.Say("Alimentação auxiliar já estável."); break; }
                    if(!game.Has("fuse")) { game.Say("Falta um fusível de 16 A. Consulta a oficina de manutenção."); break; }
                    game.VaultPower=true; game.Take("fuse-installed"); game.Say("Fusível instalado. Terminal do arquivo de valores alimentado."); game.Tone(520); game.Noise(transform.position,noiseRange); break;
                case ItemKind.Keypad:
                    if(!game.VaultPower) { game.Say("Terminal sem energia. A caixa de alimentação está junto da entrada do arquivo."); break; }
                    game.OpenKeypad(this); break;
                case ItemKind.Case: game.PickCase(this); break;
                case ItemKind.Load: game.LoadCase(); break;
                case ItemKind.Throwable:
                    if(game.CarryingCase) { game.Say("Pousa a mala com Q antes de preparar uma distração."); break; }
                    if(game.HasThrowable) { game.Say("Já tens uma garrafa. G para lançar."); break; }
                    game.HasThrowable=true; used=true; gameObject.SetActive(false); game.Say("Garrafa recolhida. G para lançar uma distração."); break;
                case ItemKind.Crate:
                    var rb=GetComponent<Rigidbody>(); if(rb!=null) rb.AddForce(game.player.transform.forward*3+Vector3.up*.25f,ForceMode.VelocityChange);
                    game.Noise(transform.position,noiseRange); game.Tone(240); break;
                case ItemKind.AlarmReset:
                    game.SetAlarm(false); game.Say("Sirene silenciada. O vigilante continua a procurar na última zona relevante."); break;
            }
        }
    }
    public sealed class ThrownNoise : MonoBehaviour
    {
        public HeistGame game; bool sounded;
        [Tooltip("Alcance do ruído do impacto.")] public float noiseRange=18;
        void OnCollisionEnter(Collision collision)
        {
            if(sounded)return; sounded=true;
            game.Noise(transform.position,noiseRange); game.Tone(900);
            game.Discover("ruido","Os impactos atraem o vigilante ao local do som; a ronda fica temporariamente livre.");
            Destroy(gameObject,8);
        }
    }
}
