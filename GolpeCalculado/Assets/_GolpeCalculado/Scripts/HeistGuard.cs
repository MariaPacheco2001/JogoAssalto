using UnityEngine;
using UnityEngine.AI;

namespace GolpeCalculado
{
    // Vigilante com quatro estados: ronda, investigação (ruído ou câmara), perseguição e procura.
    public sealed class HeistGuard : MonoBehaviour
    {
        public enum Mode { Patrol, Investigate, Chase, Search }

        public HeistGame game;
        public Transform[] patrol;
        public Renderer indicator;

        [Header("Visão")]
        [Tooltip("Alcance da visão em metros.")] public float visionRange = 15;
        [Tooltip("Alcance quando o jogador está agachado e o Corredor B está às escuras.")] public float crouchDarkRange = 6;
        [Tooltip("Meio ângulo do cone de visão, em graus.")] public float visionAngle = 55;
        public float eyeHeight = 1.55f;
        [Tooltip("Distância a que o vigilante apanha o jogador.")] public float catchDistance = 1.2f;

        [Header("Velocidades (m/s)")]
        public float patrolSpeed = 1.8f;
        public float investigateSpeed = 2.5f;
        public float chaseSpeed = 4.3f;

        [Header("Tempos (s)")]
        public float investigateTime = 14;
        public float searchTime = 18;
        [Tooltip("Tempo sem ver o jogador até passar de perseguição a procura.")] public float lostSightDelay = 1.1f;

        public Mode State { get; private set; }
        public Vector3 LastKnown { get; private set; }
        public NavMeshAgent Agent { get; private set; }

        int nextPoint;
        float investigateUntil;
        float lostSight;
        float nextPathUpdate;
        float lastVoice;

        bool Searching => State == Mode.Search || State == Mode.Investigate;

        public string Description
        {
            get
            {
                switch (State)
                {
                    case Mode.Patrol: return "RONDA";
                    case Mode.Investigate: return "INVESTIGAÇÃO";
                    case Mode.Chase: return "PERSEGUIÇÃO";
                    default: return "PROCURA";
                }
            }
        }

        void Awake()
        {
            // O agente fica desligado na cena e só liga quando o NavMesh já está carregado.
            Agent = GetComponent<NavMeshAgent>();
            Agent.enabled = true;
        }

        public bool SeesPlayer()
        {
            if (game.InVan)
                return false;

            var player = game.player;
            Vector3 origin = transform.position + Vector3.up * eyeHeight;
            Vector3 target = player.transform.position + Vector3.up * (player.IsCrouching ? .7f : 1.1f);
            Vector3 delta = target - origin;

            // Agachado e com o Corredor B às escuras, o vigilante vê muito menos.
            bool hardToSee = player.IsCrouching && !game.Circuits[0];
            float range = hardToSee ? crouchDarkRange : visionRange;
            if (delta.magnitude > range || Vector3.Angle(transform.forward, delta) > visionAngle)
                return false;

            bool blocked = Physics.Linecast(origin, target, out var hit, ~0, QueryTriggerInteraction.Ignore);
            return !blocked || hit.transform.IsChildOf(player.transform);
        }

        void Go(Vector3 destination)
        {
            if (!Agent.isOnNavMesh)
                return;
            if (NavMesh.SamplePosition(destination, out var point, 4, NavMesh.AllAreas))
                Agent.SetDestination(point.position);
        }

        public void Investigate(Vector3 position, bool camera = false)
        {
            if (State == Mode.Chase)
                return;

            LastKnown = position;
            State = Mode.Investigate;
            investigateUntil = Time.time + investigateTime;
            Go(position);

            if (Time.time - lastVoice > 4)
            {
                game.Say(camera ? "Rádio: movimento no corredor. Vou verificar." : "Vigilante: ouvi qualquer coisa...");
                lastVoice = Time.time;
            }
        }

        public void ResetGuard(Vector3 position)
        {
            State = Mode.Patrol;
            nextPoint = 0;
            lostSight = 0;
            investigateUntil = 0;
            nextPathUpdate = 0;
            lastVoice = 0;
            LastKnown = position;

            if (Agent.isOnNavMesh)
                Agent.Warp(position);
            else
                transform.position = position;

            if (patrol.Length > 0)
                Go(patrol[0].position);
        }

        void Update()
        {
            if (game == null || !game.Running || Time.timeScale == 0 || !Agent.isOnNavMesh)
                return;

            UpdateSight();
            Agent.speed = State == Mode.Chase ? chaseSpeed : State == Mode.Patrol ? patrolSpeed : investigateSpeed;

            if (Time.time > nextPathUpdate)
            {
                nextPathUpdate = Time.time + .3f;
                UpdatePath();
            }

            // Ao chegar ao sítio suspeito, olha à volta.
            if (Searching && Agent.remainingDistance < 1)
                transform.Rotate(0, 30 * Time.deltaTime, 0);

            if (indicator != null)
                indicator.material.color = State == Mode.Chase ? Color.red : State == Mode.Patrol ? new Color(.2f, .4f, .5f) : Color.yellow;
        }

        void UpdateSight()
        {
            if (SeesPlayer())
            {
                if (State != Mode.Chase)
                {
                    game.Say("Foste visto. Quebra a linha de visão e esconde-te!");
                    game.Tone(240);
                }
                State = Mode.Chase;
                LastKnown = game.player.transform.position;
                lostSight = 0;
                if (Vector3.Distance(transform.position, LastKnown) < catchDistance)
                    game.Captured();
            }
            else if (State == Mode.Chase)
            {
                lostSight += Time.deltaTime;
                if (lostSight > lostSightDelay)
                {
                    State = Mode.Search;
                    investigateUntil = Time.time + searchTime;
                    Go(LastKnown);
                }
            }
        }

        void UpdatePath()
        {
            if (State == Mode.Chase)
            {
                Go(LastKnown);
            }
            else if (State == Mode.Patrol && !Agent.pathPending && Agent.remainingDistance < .8f)
            {
                nextPoint = (nextPoint + 1) % patrol.Length;
                Go(patrol[nextPoint].position);
            }
            else if (Searching && Time.time > investigateUntil)
            {
                // Desiste e volta à ronda onde a deixou.
                State = Mode.Patrol;
                Go(patrol[nextPoint].position);
            }
        }
    }
}
