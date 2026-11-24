using UnityEngine;

namespace GolpeCalculado
{
    // Só visual: escolhe a animação do vigilante a partir do estado do HeistGuard
    // e acerta o ritmo das passadas com a velocidade real do NavMeshAgent.
    // Não mexe no agente, na visão nem na lógica.
    [RequireComponent(typeof(HeistGuard))]
    public sealed class GuardAnimator : MonoBehaviour
    {
        // Valores do parâmetro "Estado" do Animator Controller "Vigilante".
        public const int Stopped = 0, Patrol = 1, Investigate = 2, SearchWalk = 3, SearchLook = 4, Chase = 5, Captured = 6;

        public Animator animator;

        [Header("Velocidade natural das animações (m/s)")]
        [Tooltip("Velocidade a que os pés não deslizam com a animação de andar a ritmo normal.")] public float walkClipSpeed = 1.4f;
        [Tooltip("Velocidade a que os pés não deslizam com a animação de correr a ritmo normal.")] public float runClipSpeed = 3.5f;
        [Tooltip("Abaixo desta velocidade o vigilante está parado.")] public float stopSpeed = .15f;

        [Header("Olhar à volta (procura)")]
        [Tooltip("Ângulo máximo para cada lado, em graus.")] public float lookAngle = 60;
        [Tooltip("Voltas por segundo do olhar à volta.")] public float lookFrequency = .35f;

        HeistGuard guard;
        Transform head, neck;
        float speed;
        float lookWeight;

        void Awake()
        {
            guard = GetComponent<HeistGuard>();
            if (animator != null && animator.isHuman)
            {
                head = animator.GetBoneTransform(HumanBodyBones.Head);
                neck = animator.GetBoneTransform(HumanBodyBones.Neck);
            }
        }

        void Update()
        {
            if (animator == null || guard.Agent == null)
                return;

            var game = guard.game;
            bool captured = game != null && game.Current == HeistGame.Page.Defeat;

            // Velocidade real (suavizada) do agente, na horizontal.
            float real = guard.Agent.enabled ? Vector3.ProjectOnPlane(guard.Agent.velocity, Vector3.up).magnitude : 0;
            speed = Mathf.Lerp(speed, real, 1 - Mathf.Exp(-12 * Time.deltaTime));
            bool moving = speed > stopSpeed && !captured;

            int state;
            if (captured)
                state = Captured;
            else if (guard.State == HeistGuard.Mode.Search)
                state = moving ? SearchWalk : SearchLook;
            else if (!moving)
                state = Stopped;
            else if (guard.State == HeistGuard.Mode.Chase)
                state = Chase;
            else if (guard.State == HeistGuard.Mode.Investigate)
                state = Investigate;
            else
                state = Patrol;

            // Ritmo das passadas = velocidade real / velocidade natural da animação.
            float clipSpeed = state == Chase ? runClipSpeed : walkClipSpeed;
            float pace = moving ? Mathf.Clamp(speed / clipSpeed, .5f, 2.2f) : 1;

            animator.SetInteger("Estado", state);
            animator.SetFloat("Ritmo", pace);

            bool looking = state == SearchLook || state == SearchWalk;
            lookWeight = Mathf.MoveTowards(lookWeight, looking ? 1 : 0, 2 * Time.deltaTime);
        }

        // Depois do Animator: roda o pescoço e a cabeça para os lados durante a procura.
        void LateUpdate()
        {
            if (lookWeight <= 0 || head == null)
                return;
            float angle = Mathf.Sin(Time.time * lookFrequency * 2 * Mathf.PI) * lookAngle * lookWeight;
            if (neck != null)
            {
                neck.rotation = Quaternion.AngleAxis(angle * .4f, transform.up) * neck.rotation;
                angle *= .6f;
            }
            head.rotation = Quaternion.AngleAxis(angle, transform.up) * head.rotation;
        }
    }
}
