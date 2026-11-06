using UnityEngine;
using UnityEngine.AI;
namespace GolpeCalculado
{
    public sealed class HeistGuard : MonoBehaviour
    {
        public HeistGame game;
        public Transform[] patrol;
        public Renderer indicator;
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
            Vector3 origin=transform.position+Vector3.up*1.55f;
            Vector3 target=game.player.transform.position+Vector3.up*(game.player.IsCrouching?.7f:1.1f);
            var delta=target-origin;
            float range=game.player.IsCrouching && !game.Circuits[0]?6:15;
            if(delta.magnitude>range || Vector3.Angle(transform.forward,delta)>55)return false;
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
            LastKnown=position; State=Mode.Investigate; investigateUntil=Time.time+14; Go(position);
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
                if(Vector3.Distance(transform.position,LastKnown)<1.2f)game.Captured();
            }
            else if(State==Mode.Chase)
            {
                lostSight+=Time.deltaTime;
                if(lostSight>1.1f) { State=Mode.Search; investigateUntil=Time.time+18; Go(LastKnown); }
            }
            Agent.speed=State==Mode.Chase?4.3f:State==Mode.Patrol?1.8f:2.5f;
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
