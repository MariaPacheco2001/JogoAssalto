using UnityEngine;
using UnityEngine.AI;
namespace GolpeCalculado
{
    public sealed class HeistCamera : MonoBehaviour
    {
        public HeistGame game;
        public Transform lens;
        public Renderer led;
        public int circuit;
        public float baseYaw;
        float exposure, nextReport;
        public void ResetObservation(){exposure=0;nextReport=0;}
        void Update()
        {
            if(game==null || !game.Running || Time.timeScale==0)return;
            bool powered=game.Circuits[circuit]; bool sees=false;
            if(powered)
            {
                lens.localRotation=Quaternion.Euler(12,baseYaw+Mathf.Sin(Time.time*.38f)*42,0);
                var target=game.player.transform.position+Vector3.up*1.15f;
                var delta=target-lens.position;
                sees=!game.InVan && delta.magnitude<17 && Vector3.Angle(lens.forward,delta)<35 &&
                    (!Physics.Linecast(lens.position+lens.forward*.25f,target,out var hit,~0,QueryTriggerInteraction.Ignore) || hit.transform.IsChildOf(game.player.transform));
            }
            exposure=sees?exposure+Time.deltaTime:0;
            led.material.color=!powered?Color.black:sees?Color.red:Color.green;
            if(exposure>.7f && Time.time>nextReport)
            {
                nextReport=Time.time+2; game.guard.Investigate(game.player.transform.position,true); game.Tone(1150);
                game.Say("Câmara: posição comunicada. Sai do campo de visão.");
                game.Discover("camara","As câmaras comunicam a última posição observada. Paredes e perda de energia interrompem a observação.");
            }
        }
    }
}
