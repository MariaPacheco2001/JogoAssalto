using UnityEngine;

namespace GolpeCalculado
{
    // Câmara de vigilância: varre a zona, vê o jogador e comunica a posição ao vigilante.
    // Só funciona com energia; o quadro elétrico liga-a e desliga-a (evento On Switched).
    public sealed class HeistCamera : MonoBehaviour
    {
        public HeistGame game;
        public Transform lens;
        public Renderer led;
        public float baseYaw;

        [Header("Visão")]
        [Tooltip("Alcance em metros.")] public float range = 17;
        [Tooltip("Meio ângulo do cone, em graus.")] public float viewAngle = 35;

        [Header("Varrimento")]
        public float tilt = 12;
        public float sweepAngle = 42;
        public float sweepSpeed = .38f;

        [Header("Alerta")]
        [Tooltip("Segundos a ver o jogador até avisar o vigilante.")] public float exposureToReport = .7f;
        [Tooltip("Intervalo mínimo entre avisos.")] public float reportCooldown = 2;

        public bool Powered { get; private set; } = true;

        float exposure;
        float nextReport;

        public void SetPowered(bool value)
        {
            Powered = value;
        }

        public void ResetObservation()
        {
            exposure = 0;
            nextReport = 0;
        }

        void Update()
        {
            if (game == null || !game.Running || Time.timeScale == 0)
                return;

            bool sees = false;
            if (Powered)
            {
                Sweep();
                sees = SeesPlayer();
            }

            exposure = sees ? exposure + Time.deltaTime : 0;
            led.material.color = !Powered ? Color.black : sees ? Color.red : Color.green;

            if (exposure > exposureToReport && Time.time > nextReport)
                Report();
        }

        void Sweep()
        {
            float yaw = baseYaw + Mathf.Sin(Time.time * sweepSpeed) * sweepAngle;
            lens.localRotation = Quaternion.Euler(tilt, yaw, 0);
        }

        bool SeesPlayer()
        {
            if (game.InVan)
                return false;

            Vector3 target = game.player.transform.position + Vector3.up * 1.15f;
            Vector3 delta = target - lens.position;
            if (delta.magnitude >= range || Vector3.Angle(lens.forward, delta) >= viewAngle)
                return false;

            // Paredes e objetos à frente cortam a visão.
            Vector3 origin = lens.position + lens.forward * .25f;
            bool blocked = Physics.Linecast(origin, target, out var hit, ~0, QueryTriggerInteraction.Ignore);
            return !blocked || hit.transform.IsChildOf(game.player.transform);
        }

        void Report()
        {
            nextReport = Time.time + reportCooldown;
            game.guard.Investigate(game.player.transform.position, true);
            game.Tone(1150);
            game.Say("Câmara: posição comunicada. Sai do campo de visão.");
            game.Discover("camara", "As câmaras comunicam a última posição observada. Paredes e perda de energia interrompem a observação.");
        }
    }
}
