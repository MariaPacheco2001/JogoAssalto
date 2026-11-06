using UnityEngine;
using UnityEngine.AI;
namespace GolpeCalculado
{
    public sealed class HeistGuard : MonoBehaviour
    {
        public HeistGame game;
        public Transform[] patrol;
        public Renderer indicator;
        [Header("Visão")]
        [Tooltip("Alcance da visão em metros.")] public float visionRange=15;
        [Tooltip("Alcance quando o jogador está agachado e o Corredor B está às escuras.")] public float crouchDarkRange=6;
        [Tooltip("Meio ângulo do cone de visão, em graus.")] public float visionAngle=55;
        public float eyeHeight=1.55f;
        [Tooltip("Distância a que o vigilante apanha o jogador.")] public float catchDistance=1.2f;
        [Header("Velocidades (m/s)")]
        public float patrolSpeed=1.8f; public float investigateSpeed=2.5f; public float chaseSpeed=4.3f;
        [Header("Tempos (s)")]
        public float investigateTime=14; public float searchTime=18;
        [Tooltip("Tempo sem ver o jogador até passar de perseguição a procura.")] public float lostSightDelay=1.1f;
        public enum Mode { Patrol, Investigate, Chase, Search }
        public Mode State {get;private set;}
        public Vector3 LastKnown {get;private set;}
        public NavMeshAgent Agent {get;private set;}
        int next; float investigateUntil, lostSight, updatePath, lastVoice;
        public string Description => State==Mode.Patrol?"RONDA":State==Mode.Investigate?"INVESTIGAÇÃO":State==Mode.Chase?"PERSEGUIÇÃO":"PROCURA";
        void Awake() { Agent=GetComponent<NavMeshAgent>(); Agent.enabled=true; }
        public bool SeesPlayer()
        {
            if(game.InVan)return false;
            Vector3 origin=transform.position+Vector3.up*eyeHeight;
            Vector3 target=game.player.transform.position+Vector3.up*(game.player.IsCrouching?.7f:1.1f);
            var delta=target-origin;
            float range=game.player.IsCrouching && !game.Circuits[0]?crouchDarkRange:visionRange;
            if(delta.magnitude>range || Vector3.Angle(transform.forward,delta)>visionAngle)return false;
            return !Physics.Linecast(origin,target,out var hit,~0,QueryTriggerInteraction.Ignore) || hit.transform.IsChildOf(game.player.transform);
        }
        void Go(Vector3 destination)
        {
            if(!Agent.isOnNavMesh)return;
            if(NavMesh.SamplePosition(destination,out var point,4,NavMesh.AllAreas))Agent.SetDestination(point.position);
        }
        public void Investigate(Vector3 position,bool camera=false)
        {
            if(State==Mode.Chase)return;
            LastKnown=position; State=Mode.Investigate; investigateUntil=Time.time+investigateTime; Go(position);
            if(Time.time-lastVoice>4) { game.Say(camera?"Rádio: movimento no corredor. Vou verificar.":"Vigilante: ouvi qualquer coisa..."); lastVoice=Time.time; }
        }
        public void ResetGuard(Vector3 position)
        {
            State=Mode.Patrol; next=0; lostSight=0; investigateUntil=0; updatePath=0; lastVoice=0; LastKnown=position;
            if(Agent.isOnNavMesh)Agent.Warp(position); else transform.position=position;
            if(patrol.Length>0)Go(patrol[0].position);
        }
        void Update()
        {
            if(game==null || !game.Running || Time.timeScale==0 || !Agent.isOnNavMesh)return;
            bool sees=SeesPlayer();
            if(sees)
            {
                if(State!=Mode.Chase) { game.Say("Foste visto. Quebra a linha de visão e esconde-te!"); game.Tone(240); }
                State=Mode.Chase; LastKnown=game.player.transform.position; lostSight=0;
                if(Vector3.Distance(transform.position,LastKnown)<catchDistance)game.Captured();
            }
            else if(State==Mode.Chase)
            {
                lostSight+=Time.deltaTime;
                if(lostSight>lostSightDelay) { State=Mode.Search; investigateUntil=Time.time+searchTime; Go(LastKnown); }
            }
            Agent.speed=State==Mode.Chase?chaseSpeed:State==Mode.Patrol?patrolSpeed:investigateSpeed;
            if(Time.time>updatePath)
            {
                updatePath=Time.time+.3f;
                if(State==Mode.Chase)Go(LastKnown);
                else if(State==Mode.Patrol && !Agent.pathPending && Agent.remainingDistance<.8f)
                { next=(next+1)%patrol.Length; Go(patrol[next].position); }
                else if((State==Mode.Search || State==Mode.Investigate) && Time.time>investigateUntil)
                { State=Mode.Patrol; Go(patrol[next].position); }
            }
            if((State==Mode.Search || State==Mode.Investigate) && Agent.remainingDistance<1)
                transform.Rotate(0,30*Time.deltaTime,0);
            if(indicator!=null)indicator.material.color=State==Mode.Chase?Color.red:State==Mode.Patrol?new Color(.2f,.4f,.5f):Color.yellow;
        }
    }
}
