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
        [Tooltip("Alcance do ruído que este objeto faz ao ser usado (portas ruidosas, caixa de fusíveis, caixa empurrável).")]
        public float noiseRange;
        public string Prompt => kind == ItemKind.Door ? (opened ? "Fechar " : "Abrir ") + label : label;
        public void Initialize() { closedPosition=transform.position; closedRotation=transform.rotation; }
        public void SetOpen(bool value)
        {
            opened=value;
            if(canSlide) transform.position=value?openPosition:closedPosition;
            else transform.rotation=closedRotation*Quaternion.Euler(0,value?100:0,0);
            var obstacle=GetComponent<UnityEngine.AI.NavMeshObstacle>();
            if(obstacle!=null) obstacle.enabled=!value;
        }
        public void Use(HeistGame game)
        {
            if(!string.IsNullOrEmpty(requires) && !game.Has(requires)) { game.Say("Precisas de " + game.KeyName(requires) + "."); game.Tone(150); return; }
            switch(kind)
            {
                case ItemKind.Door:
                    SetOpen(!opened); game.Tone(noisy?180:340);
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
