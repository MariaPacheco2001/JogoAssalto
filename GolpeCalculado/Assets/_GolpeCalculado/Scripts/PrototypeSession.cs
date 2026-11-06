using UnityEngine;
using UnityEngine.SceneManagement;

namespace GolpeCalculado
{
    public sealed class PrototypeSession : MonoBehaviour
    {
        public PlayerMotor player;
        public VanMotor van;
        public Transform parking;
        public bool Paused { get; private set; }
        public bool Started { get; private set; }
        public bool InVan => van.Occupied;
        public int Stage { get; private set; }
        public string Feedback { get; private set; } = "";
        public bool TestMode;
        float feedbackUntil;
        float cabinYaw, cabinPitch;
        bool controls, confirmRestart;
        GUIStyle title, body, small, button, big;
        readonly Color amber = new Color(.95f, .69f, .28f);
        void Awake()
        {
            Time.timeScale = 0; AudioListener.pause = true;
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
            player.sensitivity = PlayerPrefs.GetFloat("mouseSensitivity", 2);
        }
        void OnDestroy() { Time.timeScale = 1; AudioListener.pause = false; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; }
        public void Begin()
        {
            Started = true; Stage = 0; SetPause(false);
            Say("Aproxima-te da porta do condutor. Olha para a carrinha e prime E.");
        }
        public void SetPause(bool value)
        {
            Paused = value; Time.timeScale = value ? 0 : 1; AudioListener.pause = value;
            Cursor.lockState = value || TestMode ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = value || TestMode;
        }
        public void Say(string text) { Feedback = text; feedbackUntil = Time.unscaledTime + 4; }
        public bool CanEnter()
        {
            if (InVan || van.SpeedKmh > 1 || Vector3.Distance(player.transform.position, van.entry.position) > 2.3f) return false;
            return Physics.Raycast(player.view.transform.position, player.view.transform.forward, out var hit, 2.7f,
                ~0, QueryTriggerInteraction.Ignore) && hit.transform.IsChildOf(van.transform);
        }
        public bool TryEnter()
        {
            if (!CanEnter()) return false;
            player.controlled = false; player.Body.enabled = false;
            player.view.transform.SetParent(van.seat, false);
            player.view.transform.localPosition = Vector3.zero;
            player.view.transform.localRotation = Quaternion.identity;
            cabinYaw = cabinPitch = 0;
            van.SetOccupied(true); Stage = Mathf.Max(Stage, 1);
            Say("Espaço: soltar travão de mão. W: acelerar. Shift: travar.");
            return true;
        }
        public bool TryExit()
        {
            if (!InVan) return false;
            if (van.SpeedKmh > 1) { Say("Imobiliza a carrinha antes de sair."); return false; }
            foreach (var marker in new[] { van.leftExit, van.rightExit })
            {
                if (!Physics.Raycast(marker.position + Vector3.up * 1.5f, Vector3.down, out var ground, 3, ~0, QueryTriggerInteraction.Ignore)) continue;
                if (ground.normal.y < .8f || ground.point.y > van.transform.position.y + .4f) continue;
                var feet = ground.point + Vector3.up * .06f;
                if (Physics.CheckCapsule(feet + Vector3.up * .35f, feet + Vector3.up * 1.48f, .32f, ~0, QueryTriggerInteraction.Ignore)) continue;
                var origin = new Vector3(van.transform.position.x, feet.y, van.transform.position.z) + van.transform.forward * .9f;
                var exitDirection = feet - origin; bool blocked = false;
                foreach (var hit in Physics.CapsuleCastAll(origin + Vector3.up * .35f, origin + Vector3.up * 1.48f, .3f,
                    exitDirection.normalized, exitDirection.magnitude, ~0, QueryTriggerInteraction.Ignore))
                    if (!hit.transform.IsChildOf(van.transform) && !hit.transform.IsChildOf(player.transform)) blocked = true;
                if (blocked) continue;
                van.SetOccupied(false);
                player.view.transform.SetParent(player.transform, false);
                player.view.transform.localPosition = Vector3.up * 1.65f;
                player.Teleport(feet, Quaternion.Euler(0, van.transform.eulerAngles.y + (marker == van.leftExit ? -90 : 90), 0));
                player.Body.enabled = true; player.controlled = true;
                if (Stage >= 3) Stage = 4;
                Say(Stage == 4 ? "Percurso concluído. Podes continuar a explorar e repetir o estacionamento." : "Carrinha estacionada. Travão de mão aplicado.");
                return true;
            }
            Say("Saídas bloqueadas. Afasta a carrinha do obstáculo."); return false;
        }
        public bool IsParked()
        {
            var local = parking.InverseTransformPoint(van.transform.position);
            float alignment = Mathf.Abs(Vector3.Dot(van.transform.forward, parking.forward));
            return Mathf.Abs(local.x) < .8f && Mathf.Abs(local.z) < 1.15f && alignment > .96f && van.SpeedKmh < .7f && van.Handbrake;
        }
        void Update()
        {
            if (!Started) return;
            if (Input.GetKeyDown(KeyCode.Escape)) { controls = false; confirmRestart = false; SetPause(!Paused); }
            if (Paused) return;
            if (!TestMode && Input.GetKeyDown(KeyCode.E)) { if (InVan) TryExit(); else TryEnter(); }
            if (InVan)
            {
                if (!TestMode)
                {
                    cabinYaw = Mathf.Clamp(cabinYaw + Input.GetAxis("Mouse X") * player.sensitivity, -100, 100);
                    cabinPitch = Mathf.Clamp(cabinPitch - Input.GetAxis("Mouse Y") * player.sensitivity, -55, 65);
                    player.view.transform.localRotation = Quaternion.Euler(cabinPitch, cabinYaw, 0);
                }
                if (Stage == 1 && van.SpeedKmh > 5) Stage = 2;
                if (Stage == 2 && IsParked()) { Stage = 3; Say("Estacionamento correto. Prime E para sair."); }
            }
            if (player.transform.position.y < -10 && !InVan) { player.Teleport(new Vector3(-3, .1f, -24), Quaternion.Euler(0, 85, 0)); Say("Regressaste ao ponto inicial."); }
        }
        void Styles()
        {
            if (title != null) return;
            title = new GUIStyle(GUI.skin.label) { fontSize = 44, fontStyle = FontStyle.Bold, wordWrap = true, normal = { textColor = Color.white } };
            big = new GUIStyle(title) { fontSize = 32 };
            body = new GUIStyle(GUI.skin.label) { fontSize = 19, wordWrap = true, normal = { textColor = new Color(.88f, .9f, .91f) } };
            small = new GUIStyle(body) { fontSize = 14 };
            button = new GUIStyle(GUI.skin.button) { fontSize = 18, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(18, 18, 8, 8) };
        }
        void Panel(Rect r, Color color) { var old = GUI.color; GUI.color = color; GUI.DrawTexture(r, Texture2D.whiteTexture); GUI.color = old; }
        bool Button(float y, string text) => GUI.Button(new Rect(70, y, 450, 44), text, button);
        void OnGUI()
        {
            Styles(); var oldMatrix = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(Screen.width / 1280f, Screen.height / 720f, 1));
            if (!Started || Paused)
            {
                Panel(new Rect(0, 0, 1280, 720), new Color(.025f, .04f, .05f, .93f));
                Panel(new Rect(40, 60, 4, 600), amber);
                GUI.color = amber; GUI.Label(new Rect(70, 65, 650, 25), "GOLPE CALCULADO  /  MARCO 1", small); GUI.color = Color.white;
                GUI.Label(new Rect(70, 105, 800, 80), controls ? "Controlos" : confirmRestart ? "Recomeçar o percurso?" : Paused ? "Pausa" : "Antes do golpe", title);
                GUI.Label(new Rect(70, 185, 470, 76), "Um pátio industrial. Uma carrinha. Aprende a chegar, estacionar e sair sem pressa.", body);
                if (confirmRestart)
                {
                    GUI.Label(new Rect(70, 285, 450, 60), "O progresso deste teste será perdido.", body);
                    if (Button(365, "Recomeçar")) { Time.timeScale = 1; SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
                    if (Button(420, "Cancelar")) confirmRestart = false;
                }
                else if (controls)
                {
                    GUI.Label(new Rect(70, 285, 1100, 220), "A PÉ\nWASD — andar   ·   Rato — olhar   ·   E — entrar\nShift — correr   ·   Ctrl — agachar   ·   Espaço — saltar\n\nNA CARRINHA\nW — acelerar   ·   S — travar / marcha-atrás   ·   A / D — direção\nShift — travão de serviço   ·   Espaço — travão de mão (alternar)\nE — sair quando parada   ·   Esc — pausa", body);
                    if (Button(570, "Voltar")) controls = false;
                }
                else
                {
                    if (Button(285, Started ? "Retomar percurso" : "Começar percurso")) { if (Started) SetPause(false); else Begin(); }
                    if (Button(340, "Controlos")) controls = true;
                    if (Started && Button(395, "Recomeçar percurso")) confirmRestart = true;
                    if (Button(Started ? 450 : 395, "Sair do jogo")) {
                        #if UNITY_EDITOR
                        UnityEditor.EditorApplication.isPlaying = false;
                        #else
                        Application.Quit();
                        #endif
                    }
                    GUI.Label(new Rect(700, 290, 480, 35), "01  MOVIMENTO E CONDUÇÃO", body);
                    GUI.Label(new Rect(700, 340, 460, 160), "1. Entra pela porta do condutor.\n2. Solta o travão de mão.\n3. Segue a rua até à zona amarela.\n4. Alinha a carrinha, trava e estaciona.\n5. Sai em segurança.", body);
                    GUI.Label(new Rect(700, 555, 460, 75), "Protótipo de mecânicas · PC e Mac\nA missão e a segurança serão acrescentadas nos marcos seguintes.", small);
                }
            }
            else
            {
                Panel(new Rect(28, 28, 520, 98), new Color(.035f, .05f, .06f, .88f));
                Panel(new Rect(28, 28, 4, 98), amber);
                GUI.color = amber; GUI.Label(new Rect(48, 40, 475, 22), "APROXIMAÇÃO / " + (Stage + 1).ToString("00"), small); GUI.color = Color.white;
                string[] goals = { "Entra na carrinha", "Solta o travão de mão e arranca", "Estaciona na zona amarela", "Sai da carrinha em segurança", "Percurso concluído" };
                GUI.Label(new Rect(48, 69, 475, 40), goals[Mathf.Clamp(Stage, 0, 4)], body);
                GUI.Label(new Rect(1150, 30, 100, 28), "ESC  Pausa", small);
                if (InVan)
                {
                    Panel(new Rect(975, 555, 278, 133), new Color(.035f, .05f, .06f, .9f));
                    GUI.Label(new Rect(995, 570, 230, 50), Mathf.RoundToInt(van.SpeedKmh) + " km/h", big);
                    GUI.color = van.Handbrake ? amber : Color.white;
                    GUI.Label(new Rect(995, 625, 250, 32), van.Handbrake ? "TRAVÃO DE MÃO ATIVO" : "W / S  ·  A / D  ·  SHIFT", small); GUI.color = Color.white;
                }
                else { GUI.Label(new Rect(635, 350, 20, 30), "·", body); }
                string prompt = InVan ? "E  Sair  ·  Espaço  Travão de mão" : CanEnter() ? "E  Entrar na carrinha" : "";
                if (prompt.Length > 0) { Panel(new Rect(430, 605, 420, 44), new Color(.035f, .05f, .06f, .9f)); GUI.Label(new Rect(447, 614, 398, 30), prompt, small); }
                if (Time.unscaledTime < feedbackUntil) { Panel(new Rect(28, 630, 640, 62), new Color(.035f, .05f, .06f, .9f)); GUI.Label(new Rect(43, 641, 610, 45), Feedback, small); }
            }
            GUI.matrix = oldMatrix;
        }
    }
}
