using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace GolpeCalculado
{
    [Serializable]
    public class ToneSound
    {
        public string name;
        [Tooltip("Identificador usado no código (frequência do tom original).")] public float hz;
        public AudioClip clip;
    }

    // Estado de um objeto interativo dentro de uma gravação.
    [Serializable]
    public class ItemSnapshot
    {
        public string id;
        public bool used, open;
        public Vector3 position;
    }

    // Tudo o que um checkpoint guarda em disco (JSON).
    [Serializable]
    public class HeistSnapshot
    {
        public int version = 1, checkpoint;
        public Vector3 playerPosition, vanPosition, casePosition;
        public Quaternion playerRotation, vanRotation;
        public bool carrying, loaded, recovered, vaultPower, vaultOpen, alarm, throwable;
        public bool[] circuits;
        public string[] inventory, noteKeys, noteValues;
        public ItemSnapshot[] items;
        public float seconds;
    }

    // Regras da missão: objetivos, inventário, pistas, circuitos, mala, carrinha e vitória.
    // Outras partes da mesma classe: HeistGameUI (interface), HeistPersistence (gravação)
    // e HeistMenuActions (confirmações dos menus).
    public sealed partial class HeistGame : MonoBehaviour
    {
        public enum Page { Menu, Briefing, Play, Pause, Notes, Options, Controls, Credits, Defeat, Victory, Checkpoints, Confirm, Keypad, Document }

        public PlayerMotor player;
        public VanMotor van;
        public HeistGuard guard;
        public HeistItem caseItem, vaultDoor;
        public Transform hideout;
        public Light[] alarmLights;
        public Vector3 guardResetPosition;

        [Header("Cofre e checkpoints")]
        [Tooltip("Código do terminal do arquivo (TURNO + CAIS + DIA).")] public string vaultCode = "020713";
        [Tooltip("Caixa (Is Trigger) que guarda o CP1 quando o jogador entra no armazém.")] public BoxCollider checkpoint1Zone;
        [Tooltip("Caixa (Is Trigger) que guarda o CP2 quando o jogador entra no interior com a chave do escritório.")] public BoxCollider checkpoint2Zone;

        [Header("Distração")]
        [Tooltip("Prefab da garrafa lançada com G.")] public Rigidbody thrownBottlePrefab;
        public float throwSpeed = 11;
        public float throwLift = 2;

        [Header("Jogador")]
        [Tooltip("Multiplicador da velocidade enquanto carrega a mala.")] public float carrySpeedMultiplier = .62f;
        [Tooltip("Acima desta velocidade (m/s) os passos fazem ruído.")] public float footstepNoiseSpeed = 4;
        public float footstepNoiseInterval = .9f;
        public float footstepNoiseRange = 8;
        [Tooltip("Alcance para usar objetos e entrar na carrinha.")] public float interactDistance = 2.7f;

        [Header("Som")]
        [Tooltip("Fonte 2D que toca as respostas sonoras.")] public AudioSource effects;
        [Range(0, 1)] public float effectsVolume = .6f;
        [Tooltip("Som de cada resposta. O código pede-os pelo número (hz).")] public ToneSound[] sounds;

        [Header("Carrinha e esconderijo")]
        [Tooltip("Distância máxima à traseira da carrinha para carregar a mala.")] public float loadDistance = 3;
        [Tooltip("Raio do esconderijo onde a carrinha tem de parar para vencer.")] public float hideoutRadius = 6;

        // Estado da partida.
        public bool[] Circuits = { true, true };
        public bool VaultPower, VaultOpen, CarryingCase, CaseLoaded, CaseRecovered, HasThrowable, Alarm;
        public bool TestMode;
        public int Checkpoint { get; private set; }
        public float ActiveSeconds { get; private set; }
        public Page Current { get; private set; } = Page.Menu;
        public bool Running => Current == Page.Play;
        public bool InVan => van.Occupied;
        public readonly HashSet<string> Inventory = new HashSet<string>();
        public readonly Dictionary<string, string> Notes = new Dictionary<string, string>();
        public HeistItem[] Items { get; private set; }

        string message = "";
        string documentTitle = "";
        string documentText = "";
        string pin = "";
        string confirm = "";
        float messageUntil;
        float cabinYaw, cabinPitch;
        float nextStepNoise;
        Page previous = Page.Menu;
        float volume = .7f;

        // Posição da mala na mão, à frente da câmara.
        static readonly Vector3 CarriedCaseOffset = new Vector3(.42f, -.55f, .9f);

        public string SaveDirectory => Path.Combine(Application.persistentDataPath, TestMode ? "GolpeCalculado-Testes" : "GolpeCalculado");

        public string Objective
        {
            get
            {
                if (CaseLoaded) return "Conduz até ao esconderijo e imobiliza a carrinha";
                if (CarryingCase) return "Leva a mala à traseira da carrinha · E para carregar";
                if (CaseRecovered) return "Recupera a mala onde a pousaste";
                if (VaultOpen) return "Recupera a mala NC-17 no arquivo de valores";
                if (VaultPower) return "Descobre o código de seis algarismos nos documentos";
                if (Has("fuse")) return "Instala o fusível na alimentação auxiliar do arquivo";
                if (Has("officeKey")) return "Explora o arquivo e procura o fusível na manutenção";
                if (Checkpoint > 0) return "Procura acessos, ferramentas e documentos no armazém";
                return "Estaciona, observa a segurança e escolhe uma entrada";
            }
        }

        void Awake()
        {
            Items = FindObjectsByType<HeistItem>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var item in Items)
                item.Initialize();

            volume = PlayerPrefs.GetFloat("volume", .7f);
            AudioListener.volume = volume;
            player.sensitivity = PlayerPrefs.GetFloat("mouseSensitivity", 2);
            player.invertY = PlayerPrefs.GetInt("invertY", 0) == 1;
            player.view.fieldOfView = PlayerPrefs.GetFloat("fov", 75);
            Show(Page.Menu);
        }

        void Start()
        {
            // Guarda o mundo tal como está na cena, para «Novo jogo» e «Recomeçar missão».
            initialState = CaptureSnapshot(0);
            ApplyCircuits();
        }

        void OnDestroy()
        {
            Time.timeScale = 1;
            AudioListener.pause = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Muda de ecrã. Fora do jogo o tempo e o som ficam parados e o rato fica livre.
        public void Show(Page page)
        {
            Current = page;
            bool playing = page == Page.Play;
            Time.timeScale = playing ? 1 : 0;
            AudioListener.pause = !playing;
            Cursor.lockState = playing && !TestMode ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !playing || TestMode;
        }

        public void Begin()
        {
            if (initialState != null)
                ApplySnapshot(initialState);
            Show(Page.Play);
            Say("Contrato NC-17: observa o armazém. A carrinha será o teu ponto de extração.");
        }

        // ---------- Inventário, mensagens e pistas ----------

        public bool Has(string id)
        {
            return Inventory.Contains(id);
        }

        public void Take(string id)
        {
            Inventory.Add(id);
        }

        public string KeyName(string id)
        {
            switch (id)
            {
                case "officeKey": return "a chave do escritório";
                case "crowbar": return "o pé-de-cabra";
                case "staffKey": return "a chave de serviço";
                case "manifest": return "identificar a mala no manifesto";
                default: return id;
            }
        }

        public void Say(string text)
        {
            message = text;
            messageUntil = Time.unscaledTime + 5;
        }

        public void Discover(string id, string text)
        {
            if (Notes.ContainsKey(id))
                return;
            Notes.Add(id, text);
            Say("Nova pista no diário · J para consultar");
        }

        public void ReadDocument(string id, string label, string text)
        {
            Take(id);
            Notes[id] = text;
            documentTitle = label;
            documentText = text;
            previous = Page.Pause;
            Show(Page.Document);
            Tone(580);
        }

        // Toca a resposta sonora com este número (a lista está no Inspector, em Sounds).
        public void Tone(float hz)
        {
            var sound = sounds?.FirstOrDefault(s => Mathf.Approximately(s.hz, hz));
            if (sound == null || sound.clip == null || effects == null)
            {
                Debug.LogWarning("Sem som para " + hz + " Hz");
                return;
            }
            effects.PlayOneShot(sound.clip, effectsVolume);
        }

        // Um ruído atrai o vigilante se ele estiver dentro do alcance.
        public void Noise(Vector3 position, float range)
        {
            if (Vector3.Distance(guard.transform.position, position) < range)
                guard.Investigate(position);
        }

        // ---------- Circuitos e alarme ----------

        public void ToggleCircuit(int index)
        {
            Circuits[index] = !Circuits[index];
            ApplyCircuits();
            Tone(380);
            Say(Circuits[index] ? "Circuito ligado: luzes e câmara com energia." : "Circuito desligado: luzes e câmara sem energia.");
            string zone = index == 0 ? "Corredor B" : "Oficina C";
            Discover("circuit" + index, "O quadro " + zone + " alimenta as luzes e a câmara dessa zona. Uma falha pode levar o vigilante ao quadro.");

            // Uma falha de energia leva o vigilante ao quadro, dois segundos depois.
            if (!Circuits[index])
            {
                var board = Board(index);
                if (board != null)
                    StartCoroutine(InvestigatePower(board.transform.position));
            }
        }

        IEnumerator InvestigatePower(Vector3 point)
        {
            yield return new WaitForSeconds(2);
            guard.Investigate(point);
        }

        // O que cada quadro liga está no Inspector do quadro (evento On Switched).
        HeistItem Board(int circuit)
        {
            return Items.FirstOrDefault(i => i.kind == ItemKind.Power && i.circuit == circuit);
        }

        T[] OnCircuit<T>(int circuit) where T : UnityEngine.Object
        {
            var board = Board(circuit);
            if (board == null)
                return new T[0];
            var targets = new List<T>();
            for (int i = 0; i < board.onSwitched.GetPersistentEventCount(); i++)
            {
                if (board.onSwitched.GetPersistentTarget(i) is T target && !targets.Contains(target))
                    targets.Add(target);
            }
            return targets.ToArray();
        }

        public Light[] circuitBLights => OnCircuit<Light>(0);
        public Light[] circuitCLights => OnCircuit<Light>(1);

        public HeistCamera[] CamerasOnCircuit(int circuit)
        {
            return OnCircuit<HeistCamera>(circuit);
        }

        public void ApplyCircuits()
        {
            foreach (var board in Items.Where(i => i.kind == ItemKind.Power))
                board.onSwitched.Invoke(Circuits[board.circuit]);
            foreach (var light in alarmLights)
                light.enabled = Alarm;
        }

        public void SetAlarm(bool value)
        {
            Alarm = value;
            ApplyCircuits();
            if (value)
            {
                Tone(250);
                guard.Investigate(caseItem.transform.position);
                Say("Alarme de reserva ativo. Procura cobertura e prepara a saída.");
            }
        }

        // ---------- Terminal do arquivo ----------

        public void OpenKeypad(HeistItem item)
        {
            pin = "";
            previous = Page.Pause;
            Show(Page.Keypad);
        }

        public bool SubmitPin(string value)
        {
            if (value != vaultCode)
            {
                pin = "";
                Say("Código incorreto. Lê a ordem de expedição e os três registos.");
                Tone(130);
                return false;
            }
            if (!VaultPower)
                return false;

            VaultOpen = true;
            Take("vaultAccess");
            vaultDoor.SetOpen(true, true);
            Show(Page.Play);
            Say("Arquivo aberto. Identifica e recupera a mala NC-17.");
            Tone(800);
            return true;
        }

        // ---------- Mala ----------

        void HoldCase()
        {
            caseItem.transform.SetParent(player.view.transform, false);
            caseItem.transform.localPosition = CarriedCaseOffset;
            caseItem.transform.localRotation = Quaternion.identity;
            player.SpeedMultiplier = carrySpeedMultiplier;
        }

        public void PickCase(HeistItem item)
        {
            if (CarryingCase || CaseLoaded)
                return;
            if (!VaultOpen)
            {
                Say("O arquivo ainda está fechado.");
                return;
            }
            if (!Has("manifest"))
            {
                Say("Consulta o manifesto para confirmar a referência NC-17.");
                return;
            }

            CarryingCase = true;
            caseItem = item;
            var collider = item.GetComponent<Collider>();
            if (collider != null)
                collider.enabled = false;
            HoldCase();

            // A primeira vez que a mala sai da base dispara o alarme e guarda o CP3.
            if (!CaseRecovered)
            {
                CaseRecovered = true;
                SetAlarm(true);
                Discover("caseAlarm", "O sensor da mala usa alimentação de reserva. Desligar as luzes não impede o alarme de extração.");
                SaveCheckpoint(3);
            }
            Say("Mala recuperada. Q para pousar. Não podes lançar objetos enquanto a transportas.");
        }

        public bool DropCase()
        {
            if (!CarryingCase)
                return false;

            Vector3 from = player.transform.position + Vector3.up * .6f;
            Vector3 point = from + player.transform.forward * .9f;
            if (Physics.Linecast(from, point, ~0, QueryTriggerInteraction.Ignore))
            {
                Say("Sem espaço para pousar a mala.");
                return false;
            }
            if (!Physics.Raycast(point + Vector3.up, Vector3.down, out var hit, 3, ~0, QueryTriggerInteraction.Ignore))
            {
                Say("Procura chão livre.");
                return false;
            }

            caseItem.transform.SetParent(null);
            caseItem.transform.position = hit.point + Vector3.up * .23f;
            caseItem.transform.rotation = Quaternion.Euler(0, player.transform.eulerAngles.y, 0);
            caseItem.GetComponent<Collider>().enabled = true;
            CarryingCase = false;
            player.SpeedMultiplier = 1;
            Say("Mala pousada. Terás de voltar para a recuperar.");
            return true;
        }

        public bool LoadCase()
        {
            if (!CarryingCase)
            {
                Say(CaseLoaded ? "A mala já está carregada." : "Traz a mala até à traseira da carrinha.");
                return false;
            }
            Vector3 vanBack = van.transform.TransformPoint(new Vector3(0, 0, -2.6f));
            if (Vector3.Distance(player.transform.position, vanBack) > loadDistance)
            {
                Say("Aproxima-te da traseira da carrinha.");
                return false;
            }

            CarryingCase = false;
            CaseLoaded = true;
            player.SpeedMultiplier = 1;
            caseItem.transform.SetParent(null);
            caseItem.gameObject.SetActive(false);
            Say("Mala carregada. Entra e regressa ao esconderijo, onde começaste.");
            Tone(900);
            return true;
        }

        // ---------- Carrinha ----------

        // Só se entra parado, junto à porta, sem a mala e a olhar para a carrinha.
        public bool CanEnter()
        {
            if (InVan || CarryingCase || van.SpeedKmh >= 1)
                return false;
            if (Vector3.Distance(player.transform.position, van.entry.position) >= 2.3f)
                return false;
            var view = player.view.transform;
            return Physics.Raycast(view.position, view.forward, out var hit, interactDistance, ~0, QueryTriggerInteraction.Ignore)
                && hit.transform.IsChildOf(van.transform);
        }

        public bool EnterVan()
        {
            if (!CanEnter())
                return false;

            player.controlled = false;
            player.Body.enabled = false;
            player.view.transform.SetParent(van.seat, false);
            player.view.transform.localPosition = Vector3.zero;
            player.view.transform.localRotation = Quaternion.identity;
            cabinPitch = 0;
            cabinYaw = 0;
            van.SetOccupied(true);
            Say("Espaço: travão de mão. Shift: travar. E: sair quando parada.");
            return true;
        }

        public bool ExitVan()
        {
            if (!InVan)
                return false;
            if (van.SpeedKmh > 1)
            {
                Say("Para a carrinha antes de sair.");
                return false;
            }

            // Tenta a porta do condutor e depois a do outro lado.
            foreach (var marker in new[] { van.leftExit, van.rightExit })
            {
                if (!FindExitSpot(marker, out var feet))
                    continue;

                van.SetOccupied(false);
                player.view.transform.SetParent(player.transform, false);
                player.view.transform.localPosition = Vector3.up * 1.65f;
                float side = marker == van.leftExit ? -90 : 90;
                player.Teleport(feet, Quaternion.Euler(0, van.transform.eulerAngles.y + side, 0));
                player.Body.enabled = true;
                player.controlled = true;
                return true;
            }
            Say("Saídas bloqueadas. Afasta a carrinha dos obstáculos.");
            return false;
        }

        // Um lugar de saída serve se tiver chão plano, espaço para o jogador e caminho livre desde o banco.
        bool FindExitSpot(Transform marker, out Vector3 feet)
        {
            feet = Vector3.zero;
            if (!Physics.Raycast(marker.position + Vector3.up * 1.5f, Vector3.down, out var ground, 3, ~0, QueryTriggerInteraction.Ignore))
                return false;
            if (ground.normal.y < .8f || ground.point.y > van.transform.position.y + .4f)
                return false;

            feet = ground.point + Vector3.up * .06f;
            if (Physics.CheckCapsule(feet + Vector3.up * .35f, feet + Vector3.up * 1.48f, .32f, ~0, QueryTriggerInteraction.Ignore))
                return false;

            Vector3 origin = new Vector3(van.transform.position.x, feet.y, van.transform.position.z) + van.transform.forward * .9f;
            Vector3 direction = feet - origin;
            var hits = Physics.CapsuleCastAll(origin + Vector3.up * .35f, origin + Vector3.up * 1.48f, .3f,
                direction.normalized, direction.magnitude, ~0, QueryTriggerInteraction.Ignore);
            bool blocked = hits.Any(h => !h.transform.IsChildOf(van.transform) && !h.transform.IsChildOf(player.transform));
            return !blocked;
        }

        // ---------- Interação, lançamento e derrota ----------

        public void Captured()
        {
            if (Current != Page.Play || TestMode)
                return;
            Show(Page.Defeat);
        }

        // O objeto interativo para onde o jogador está a olhar (ou null).
        public HeistItem Target()
        {
            var view = player.view.transform;
            var hits = Physics.RaycastAll(view.position, view.forward, interactDistance, ~0, QueryTriggerInteraction.Ignore);
            foreach (var hit in hits.OrderBy(h => h.distance))
            {
                if (hit.transform.IsChildOf(player.transform))
                    continue;
                return hit.collider.GetComponentInParent<HeistItem>();
            }
            return null;
        }

        public bool Throw()
        {
            if (!HasThrowable || CarryingCase)
            {
                Say(CarryingCase ? "Pousa a mala primeiro." : "Procura uma garrafa para lançar.");
                return false;
            }
            var view = player.view.transform;
            Vector3 position = view.position + view.forward * .65f;
            if (Physics.CheckSphere(position, .12f, ~0, QueryTriggerInteraction.Ignore))
            {
                Say("Não há espaço livre para lançar.");
                return false;
            }

            var bottle = Instantiate(thrownBottlePrefab, position, Quaternion.identity);
            bottle.name = thrownBottlePrefab.name;
            bottle.linearVelocity = view.forward * throwSpeed + Vector3.up * throwLift;
            bottle.GetComponent<ThrownNoise>().game = this;
            HasThrowable = false;
            return true;
        }

        static bool Inside(BoxCollider zone, Vector3 point)
        {
            Vector3 local = zone.transform.InverseTransformPoint(point) - zone.center;
            Vector3 half = zone.size * .5f;
            return Mathf.Abs(local.x) < half.x && Mathf.Abs(local.y) < half.y && Mathf.Abs(local.z) < half.z;
        }

        // ---------- Ciclo do jogo ----------

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
                Back();
            if (!Running)
                return;

            ActiveSeconds += Time.deltaTime;
            if (!TestMode)
                ReadKeys();
            if (!InVan)
                UpdateOnFoot();

            bool parkedAtHideout = Vector3.Distance(van.transform.position, hideout.position) < hideoutRadius && van.SpeedKmh < 1;
            if (CaseLoaded && InVan && parkedAtHideout)
                Show(Page.Victory);
        }

        void ReadKeys()
        {
            if (Input.GetKeyDown(KeyCode.J))
            {
                previous = Page.Pause;
                Show(Page.Notes);
            }
            if (Input.GetKeyDown(KeyCode.Q))
                DropCase();
            if (Input.GetKeyDown(KeyCode.G))
                Throw();
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (InVan)
                {
                    ExitVan();
                }
                else
                {
                    var target = Target();
                    if (target != null)
                        target.Use(this);
                    else
                        EnterVan();
                }
            }
            if (InVan)
                LookAroundCabin();
        }

        void LookAroundCabin()
        {
            float invert = player.invertY ? -1 : 1;
            cabinYaw = Mathf.Clamp(cabinYaw + Input.GetAxis("Mouse X") * player.sensitivity, -100, 100);
            cabinPitch = Mathf.Clamp(cabinPitch - Input.GetAxis("Mouse Y") * player.sensitivity * invert, -55, 65);
            player.view.transform.localRotation = Quaternion.Euler(cabinPitch, cabinYaw, 0);
        }

        void UpdateOnFoot()
        {
            Vector3 p = player.transform.position;
            if (Checkpoint < 1 && Inside(checkpoint1Zone, p))
                SaveCheckpoint(1);
            if (Checkpoint < 2 && Has("officeKey") && Inside(checkpoint2Zone, p))
                SaveCheckpoint(2);

            // Correr faz barulho.
            if (player.Body.velocity.magnitude > footstepNoiseSpeed && Time.time > nextStepNoise)
            {
                nextStepNoise = Time.time + footstepNoiseInterval;
                Noise(p, footstepNoiseRange);
            }

            if (p.y < -8)
            {
                Show(Page.Defeat);
                Say("Saíste da área segura.");
            }
        }
    }
}
