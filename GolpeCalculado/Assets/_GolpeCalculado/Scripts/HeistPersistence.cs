using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace GolpeCalculado
{
    public sealed partial class HeistGame
    {
        HeistSnapshot initialState;
        public string LastSaveError { get; private set; }
        public string Feedback => message;
        string FileFor(int cp)=>Path.Combine(SaveDirectory,"cp"+cp+".json");
        static bool Finite(float f)=>!float.IsNaN(f)&&!float.IsInfinity(f);
        static bool ValidPosition(Vector3 p)=>Finite(p.x)&&Finite(p.y)&&Finite(p.z)&&p.sqrMagnitude<100000000;
        static bool ValidRotation(Quaternion q)
        {
            float norm=q.x*q.x+q.y*q.y+q.z*q.z+q.w*q.w;
            return Finite(norm)&&norm>.5f&&norm<1.5f;
        }
        bool ValidateSnapshot(HeistSnapshot s,int cp)
        {
            if(s==null||s.version!=1||s.checkpoint!=cp||cp<1||cp>3)return false;
            if(s.circuits==null||s.circuits.Length!=2||s.inventory==null||s.noteKeys==null||s.noteValues==null||s.items==null)return false;
            if(s.noteKeys.Length!=s.noteValues.Length||s.items.Length!=Items.Length||!Finite(s.seconds)||s.seconds<0)return false;
            if(!ValidPosition(s.playerPosition)||!ValidPosition(s.vanPosition)||!ValidPosition(s.casePosition)||
                !ValidRotation(s.playerRotation)||!ValidRotation(s.vanRotation))return false;
            if(s.carrying&&s.loaded||(s.carrying||s.loaded)&&!s.recovered||s.vaultOpen&&!s.vaultPower)return false;
            if(s.inventory.Any(string.IsNullOrWhiteSpace)||s.inventory.Distinct().Count()!=s.inventory.Length||
                s.noteKeys.Any(string.IsNullOrWhiteSpace)||s.noteKeys.Distinct().Count()!=s.noteKeys.Length||
                s.noteValues.Any(v=>v==null))return false;
            var ids=new HashSet<string>();
            foreach(var item in s.items)
                if(item==null||string.IsNullOrWhiteSpace(item.id)||!ids.Add(item.id)||!ValidPosition(item.position))return false;
            if(!Items.All(i=>ids.Contains(i.id)))return false;
            foreach(var item in Items)
            {
                var state=s.items.First(i=>i.id==item.id);
                if((item.kind==ItemKind.Key||item.kind==ItemKind.Tool||item.kind==ItemKind.Fuse)&&
                    state.used!=s.inventory.Contains(item.id))return false;
                if(item.kind==ItemKind.Door&&state.open&&!string.IsNullOrEmpty(item.requires)&&
                    !s.inventory.Contains(item.requires))return false;
            }
            if(s.recovered&&(!s.vaultOpen||!s.inventory.Contains("manifest")))return false;
            return true;
        }
        bool TryReadCheckpoint(int cp,out HeistSnapshot snapshot,out string error)
        {
            snapshot=null;error="";
            if(cp<1||cp>3){error="Checkpoint desconhecido.";return false;}
            try
            {
                string path=FileFor(cp);
                if(!File.Exists(path)){error="Não existe uma gravação para este checkpoint.";return false;}
                if(new FileInfo(path).Length>1024*1024){error="A gravação excede o tamanho permitido.";return false;}
                var data=JsonUtility.FromJson<HeistSnapshot>(File.ReadAllText(path));
                if(!ValidateSnapshot(data,cp)){error="Gravação inválida ou de uma versão incompatível.";return false;}
                snapshot=data;return true;
            }
            catch(Exception ex) when(ex is IOException||ex is UnauthorizedAccessException||ex is ArgumentException)
            {error="Não foi possível ler a gravação. Podes regressar ao menu ou começar uma partida nova.";return false;}
        }
        public bool HasSave(int cp)=>TryReadCheckpoint(cp,out _,out _);
        public int LatestSave(){for(int i=3;i>0;i--)if(HasSave(i))return i;return 0;}
        HeistSnapshot CaptureSnapshot(int cp)=>new HeistSnapshot
        {
            checkpoint=cp,playerPosition=player.transform.position,playerRotation=player.transform.rotation,
            vanPosition=van.transform.position,vanRotation=van.transform.rotation,casePosition=caseItem.transform.position,
            carrying=CarryingCase,loaded=CaseLoaded,recovered=CaseRecovered,vaultPower=VaultPower,vaultOpen=VaultOpen,
            alarm=Alarm,throwable=HasThrowable,circuits=(bool[])Circuits.Clone(),inventory=Inventory.ToArray(),
            noteKeys=Notes.Keys.ToArray(),noteValues=Notes.Values.ToArray(),seconds=ActiveSeconds,
            items=Items.Select(i=>new ItemSnapshot{id=i.id,used=i.used,open=i.opened,position=i.transform.position}).ToArray()
        };
        public void SaveCheckpoint(int cp)
        {
            if(cp<1||cp>3)return;
            var snapshot=CaptureSnapshot(cp);
            if(!ValidateSnapshot(snapshot,cp)){Say("Não foi possível guardar um estado consistente.");return;}
            try
            {
                Directory.CreateDirectory(SaveDirectory);string target=FileFor(cp),tmp=target+".tmp";
                File.WriteAllText(tmp,JsonUtility.ToJson(snapshot,true));
                if(File.Exists(target))File.Replace(tmp,target,null);else File.Move(tmp,target);
                Checkpoint=Mathf.Max(Checkpoint,cp);LastSaveError="";Say("Checkpoint "+cp+" guardado.");
            }
            catch(Exception ex) when(ex is IOException||ex is UnauthorizedAccessException)
            {LastSaveError="Não foi possível guardar. Verifica as permissões de escrita.";Say(LastSaveError);}
        }
        public bool LoadCheckpoint(int cp)
        {
            if(!TryReadCheckpoint(cp,out var snapshot,out var error)){LastSaveError=error;Say(error);return false;}
            ApplySnapshot(snapshot);LastSaveError="";Show(Page.Play);Say("Checkpoint "+cp+" restaurado.");return true;
        }
        void ApplySnapshot(HeistSnapshot s)
        {
            StopAllCoroutines();
            foreach(var thrown in FindObjectsByType<ThrownNoise>(FindObjectsSortMode.None))
                if(thrown.game==this){thrown.gameObject.SetActive(false);Destroy(thrown.gameObject);}
            foreach(var camera in FindObjectsByType<HeistCamera>(FindObjectsSortMode.None))camera.ResetObservation();
            van.SetOccupied(false);player.view.transform.SetParent(player.transform,false);
            player.view.transform.localPosition=Vector3.up*1.65f;player.view.transform.localRotation=Quaternion.identity;
            player.Body.enabled=true;player.controlled=true;player.Teleport(s.playerPosition,s.playerRotation);
            player.MoveInput=player.LookInput=Vector2.zero;player.RunInput=player.CrouchInput=player.JumpInput=false;
            van.Body.position=s.vanPosition;van.Body.rotation=s.vanRotation;
            van.Body.linearVelocity=Vector3.zero;van.Body.angularVelocity=Vector3.zero;van.SetHandbrake(true);
            Inventory.Clear();foreach(string key in s.inventory)Inventory.Add(key);
            Notes.Clear();for(int i=0;i<s.noteKeys.Length;i++)Notes[s.noteKeys[i]]=s.noteValues[i];
            caseItem.transform.SetParent(null);
            foreach(var item in Items)
            {
                var state=s.items.First(i=>i.id==item.id);item.used=state.used;
                item.transform.position=state.position;item.transform.rotation=item.closedRotation;
                item.gameObject.SetActive(!state.used);
                if(item.kind==ItemKind.Door)item.SetOpen(state.open);
                var rb=item.GetComponent<Rigidbody>();
                if(rb!=null){rb.linearVelocity=Vector3.zero;rb.angularVelocity=Vector3.zero;}
            }
            Circuits=(bool[])s.circuits.Clone();VaultPower=s.vaultPower;VaultOpen=s.vaultOpen;
            vaultDoor.SetOpen(VaultOpen);Alarm=s.alarm;CaseLoaded=s.loaded;CaseRecovered=s.recovered;HasThrowable=s.throwable;
            caseItem.transform.position=s.casePosition;caseItem.gameObject.SetActive(!s.loaded);
            caseItem.GetComponent<Collider>().enabled=true;CarryingCase=false;player.SpeedMultiplier=1;
            if(s.carrying&&!s.loaded)
            {
                CarryingCase=true;caseItem.GetComponent<Collider>().enabled=false;
                caseItem.transform.SetParent(player.view.transform,false);caseItem.transform.localPosition=new Vector3(.42f,-.55f,.9f);
                caseItem.transform.localRotation=Quaternion.identity;player.SpeedMultiplier=.62f;
            }
            Checkpoint=s.checkpoint;ActiveSeconds=s.seconds;cabinYaw=cabinPitch=0;stepNoise=0;
            guard.ResetGuard(guardResetPosition);ApplyCircuits();Physics.SyncTransforms();
        }
        public bool ClearProgress()
        {
            var backups=new Dictionary<string,byte[]>();
            try
            {
                for(int cp=1;cp<=3;cp++)if(File.Exists(FileFor(cp)))backups[FileFor(cp)]=File.ReadAllBytes(FileFor(cp));
                foreach(var path in backups.Keys)File.Delete(path);
                return true;
            }
            catch(Exception ex) when(ex is IOException||ex is UnauthorizedAccessException)
            {
                foreach(var pair in backups)
                    try{if(!File.Exists(pair.Key))File.WriteAllBytes(pair.Key,pair.Value);}catch(IOException){}catch(UnauthorizedAccessException){}
                Say("Não foi possível substituir a gravação. A nova partida não foi iniciada.");return false;
            }
        }
    }
}
