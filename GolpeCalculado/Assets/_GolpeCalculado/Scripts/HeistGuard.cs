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
        [Tooltip("Salta os pontos da ronda a que não consegue chegar (por exemplo, atrás de uma porta fechada). Usado pelo segundo vigilante, que só entra no armazém com uma entrada aberta.")]
        public bool skipUnreachablePoints;

        [Header("Visão")]
        [Tooltip("Alcance da visão em metros.")] public float visionRange = 15;
        [Tooltip("Alcance quando o jogador está agachado dentro do Corredor B e o quadro B está desligado. Noutros sítios vale sempre o alcance normal.")] public float crouchDarkRange = 6;
        [Tooltip("Meio ângulo do cone de visão, em graus.")] public float visionAngle = 55;
        [Tooltip("A esta distância (na horizontal) sente o jogador em qualquer direção, mesmo fora do cone.")] public float proximityRange = 1;
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
        [Tooltip("Tempo entre a falha de energia e o vigilante partir para o quadro. Ao chegar, volta a ligá-lo.")] public float powerReactionDelay = 2;

        // Zona do mapa (MapZone "Zona Corredor B") onde agachar às escuras ajuda.
        public const string DarkCorridorZone = "corredorB";

        public Mode State { get; private set; }
        public Vector3 LastKnown { get; private set; }
        public NavMeshAgent Agent { get; private set; }

        int nextPoint;
        float investigateUntil;
        float lostSight;
        float nextPathUpdate;
        float lastVoice;

        // Prioridade dos avisos (GDD 3.4): visão direta > procura > câmara > alarme > ruído e falha elétrica.
        public const int NoisePriority = 1, PowerPriority = 1, AlarmPriority = 2, CameraPriority = 3, SearchPriority = 4;
        int currentPriority;

        // Circuito cujo quadro está a investigar por causa de uma falha elétrica (-1 se nenhum).
        public int PowerCircuit { get; private set; } = -1;

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

            // O cone mede-se na horizontal: um jogador agachado mesmo à frente
            // não pode ficar "por baixo" da visão por estar mais baixo que os olhos.
            Vector3 flat = Vector3.ProjectOnPlane(delta, Vector3.up);

            // Encostado a ele, sente o jogador em qualquer direção (se não houver parede no meio).
            if (flat.magnitude < proximityRange)
                return NothingBetween(origin, target);

            // Agachado dentro do Corredor B às escuras (quadro B desligado), o vigilante vê muito menos.
            // Fora do Corredor B vale sempre o alcance normal do nível, mesmo agachado e sem luz.
            // A mesma condição dá a pista "darkCrouch" no diário (HeistGame.CrouchedInDarkCorridor).
            bool hardToSee = game.CrouchedInDarkCorridor;
            float range = hardToSee ? crouchDarkRange : visionRange;
            if (delta.magnitude > range || Vector3.Angle(transform.forward, flat) > visionAngle)
                return false;

            return NothingBetween(origin, target);
        }

        // Verdadeiro se nenhuma parede ou objeto tapa a linha entre os olhos e o jogador.
        // O vigilante não tem colliders próprios, por isso a linha nunca bate nele mesmo.
        bool NothingBetween(Vector3 origin, Vector3 target)
        {
            bool blocked = Physics.Linecast(origin, target, out var hit, ~0, QueryTriggerInteraction.Ignore);
            return !blocked || hit.transform.IsChildOf(game.player.transform);
        }

        void Go(Vector3 destination)
        {
            if (!Agent.isOnNavMesh)
                return;
            if (NavMesh.SamplePosition(destination, out var point, 4, NavMesh.AllAreas))
                Agent.SetDestination(point.position);
        }

        // Falha elétrica: vai ao quadro e, se lá chegar sem passar a perseguição, volta a ligá-lo.
        public void InvestigatePower(int circuit, Vector3 boardPosition)
        {
            if (Investigate(boardPosition, PowerPriority))
                PowerCircuit = circuit;
        }

        // Devolve falso se o aviso foi ignorado (perseguição ou investigação mais importante).
        public bool Investigate(Vector3 position, int priority)
        {
            if (State == Mode.Chase)
                return false;
            // Um aviso menos importante não interrompe uma investigação ou procura mais importante.
            if (Searching && priority < currentPriority)
                return false;

            PowerCircuit = -1;
            currentPriority = priority;
            LastKnown = position;
            State = Mode.Investigate;
            investigateUntil = Time.time + investigateTime;
            Go(position);

            if (Time.time - lastVoice > 4)
            {
                game.Speak(priority == CameraPriority ? "radioCorredor" : "ouvi");
                lastVoice = Time.time;
            }
            return true;
        }

        public void ResetGuard(Vector3 position)
        {
            State = Mode.Patrol;
            currentPriority = 0;
            PowerCircuit = -1;
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

            // Chegou ao quadro da falha elétrica: volta a ligá-lo e continua a investigar ali.
            if (State == Mode.Investigate && PowerCircuit >= 0 && !Agent.pathPending && Agent.remainingDistance < 1)
            {
                game.RestoreCircuit(PowerCircuit);
                PowerCircuit = -1;
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
                PowerCircuit = -1;
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
                    currentPriority = SearchPriority;
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
                nextPoint = NextPatrolPoint(nextPoint);
                Go(patrol[nextPoint].position);
            }
            else if (Searching && Time.time > investigateUntil)
            {
                // Desiste e volta à ronda onde a deixou.
                State = Mode.Patrol;
                currentPriority = 0;
                Go(patrol[nextPoint].position);
            }
        }

        // Ponto seguinte da ronda. Com skipUnreachablePoints, passa à frente os que não têm caminho completo.
        int NextPatrolPoint(int from)
        {
            int next = (from + 1) % patrol.Length;
            if (!skipUnreachablePoints)
                return next;
            for (int i = 0; i < patrol.Length; i++, next = (next + 1) % patrol.Length)
                if (Reachable(patrol[next].position))
                    return next;
            return (from + 1) % patrol.Length;
        }

        bool Reachable(Vector3 destination)
        {
            var path = new NavMeshPath();
            return NavMesh.SamplePosition(destination, out var point, 4, NavMesh.AllAreas)
                && Agent.CalculatePath(point.position, path) && path.status == NavMeshPathStatus.PathComplete;
        }
    }
}
