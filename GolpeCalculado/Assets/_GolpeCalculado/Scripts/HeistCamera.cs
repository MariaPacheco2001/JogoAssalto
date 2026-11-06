using UnityEngine;
using UnityEngine.AI;
namespace GolpeCalculado
{
    public sealed class HeistCamera : MonoBehaviour
    {
        public HeistGame game;
        public Transform lens;
        public Renderer led;
        public float baseYaw;
        [Header("Visão")]
        [Tooltip("Alcance em metros.")] public float range=17;
        [Tooltip("Meio ângulo do cone, em graus.")] public float viewAngle=35;
        [Header("Varrimento")]
        public float tilt=12; public float sweepAngle=42; public float sweepSpeed=.38f;
        [Header("Alerta")]
        [Tooltip("Segundos a ver o jogador até avisar o vigilante.")] public float exposureToReport=.7f;
        [Tooltip("Intervalo mínimo entre avisos.")] public float reportCooldown=2;
        float exposure, nextReport;
        // Ligado pelo quadro elétrico (evento On Switched no Inspector).
        public bool Powered {get;private set;}=true;
        public void SetPowered(bool value){Powered=value;}
        public void ResetObservation(){exposure=0;nextReport=0;}
        void Update()
        {
            if(game==null || !game.Running || Time.timeScale==0)return;
            bool powered=Powered; bool sees=false;
            if(powered)
            {
                lens.localRotation=Quaternion.Euler(tilt,baseYaw+Mathf.Sin(Time.time*sweepSpeed)*sweepAngle,0);
                var target=game.player.transform.position+Vector3.up*1.15f;
                var delta=target-lens.position;
                sees=!game.InVan && delta.magnitude<range && Vector3.Angle(lens.forward,delta)<viewAngle &&
                    (!Physics.Linecast(lens.position+lens.forward*.25f,target,out var hit,~0,QueryTriggerInteraction.Ignore) || hit.transform.IsChildOf(game.player.transform));
            }
            exposure=sees?exposure+Time.deltaTime:0;
            led.material.color=!powered?Color.black:sees?Color.red:Color.green;
            if(exposure>exposureToReport && Time.time>nextReport)
            {
                nextReport=Time.time+reportCooldown; game.guard.Investigate(game.player.transform.position,true); game.Tone(1150);
                game.Say("Câmara: posição comunicada. Sai do campo de visão.");
                game.Discover("camara","As câmaras comunicam a última posição observada. Paredes e perda de energia interrompem a observação.");
            }
        }
    }
}
