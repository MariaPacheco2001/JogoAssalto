using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

namespace GolpeCalculado
{
    public enum ItemKind { Door, Key, Tool, Fuse, Document, Power, FuseSocket, Keypad, Case, Load, Throwable, Crate, AlarmReset }

    // Qualquer objeto com que o jogador interage com E. O tipo (kind) decide o que acontece.
    // O id tem de ser único: a gravação dos checkpoints usa-o.
    public sealed class HeistItem : MonoBehaviour
    {
        public string id, label, text, requires;
        public ItemKind kind;
        public bool used, opened, noisy;
        public Vector3 closedPosition, openPosition;
        public Quaternion closedRotation;
        public bool canSlide;
        public int circuit;

        [Tooltip("Quadro elétrico: o que liga e desliga com este quadro (luzes, câmara). Recebe true quando o circuito tem energia.")]
        public UnityEvent<bool> onSwitched = new UnityEvent<bool>();

        [Tooltip("Alcance do ruído que este objeto faz ao ser usado (portas ruidosas, caixa de fusíveis, caixa empurrável).")]
        public float noiseRange;

        [Header("Animação da porta")]
        [Tooltip("Duração da abertura/fecho em segundos (0 = instantâneo).")] public float openDuration = .6f;
        public AnimationCurve openCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

        float animationStart = -1;
        Vector3 animationFromPosition;
        Quaternion animationFromRotation;

        public string Prompt
        {
            get
            {
                if (kind != ItemKind.Door)
                    return label;
                return (opened ? "Fechar " : "Abrir ") + label;
            }
        }

        Vector3 TargetPosition => opened ? openPosition : closedPosition;
        Quaternion TargetRotation => closedRotation * Quaternion.Euler(0, opened ? 100 : 0, 0);

        public void Initialize()
        {
            closedPosition = transform.position;
            closedRotation = transform.rotation;
        }

        // Sem animação: usado ao restaurar checkpoints e nos testes.
        public void SetOpen(bool value)
        {
            SetOpen(value, false);
        }

        public void SetOpen(bool value, bool animate)
        {
            opened = value;
            if (animate && openDuration > 0)
            {
                animationStart = Time.time;
                animationFromPosition = transform.position;
                animationFromRotation = transform.rotation;
            }
            else
            {
                animationStart = -1;
                if (canSlide)
                    transform.position = TargetPosition;
                else
                    transform.rotation = TargetRotation;
            }

            // Porta aberta deixa de bloquear o caminho do vigilante.
            var obstacle = GetComponent<NavMeshObstacle>();
            if (obstacle != null)
                obstacle.enabled = !value;
        }

        void Update()
        {
            if (animationStart < 0)
                return;

            float t = Mathf.Clamp01((Time.time - animationStart) / openDuration);
            float k = openCurve.Evaluate(t);
            if (canSlide)
                transform.position = Vector3.LerpUnclamped(animationFromPosition, TargetPosition, k);
            else
                transform.rotation = Quaternion.SlerpUnclamped(animationFromRotation, TargetRotation, k);

            if (t >= 1)
                animationStart = -1;
        }

        public void Use(HeistGame game)
        {
            if (!string.IsNullOrEmpty(requires) && !game.Has(requires))
            {
                game.Say("Precisas de " + game.KeyName(requires) + ".");
                game.Tone(150);
                return;
            }

            switch (kind)
            {
                case ItemKind.Door: UseDoor(game); break;
                case ItemKind.Key:
                case ItemKind.Tool:
                case ItemKind.Fuse: PickUp(game); break;
                case ItemKind.Document: game.ReadDocument(id, label, text); break;
                case ItemKind.Power: game.ToggleCircuit(circuit); break;
                case ItemKind.FuseSocket: InstallFuse(game); break;
                case ItemKind.Keypad: UseKeypad(game); break;
                case ItemKind.Case: game.PickCase(this); break;
                case ItemKind.Load: game.LoadCase(); break;
                case ItemKind.Throwable: PickUpBottle(game); break;
                case ItemKind.Crate: PushCrate(game); break;
                case ItemKind.AlarmReset:
                    game.SetAlarm(false);
                    game.Say("Sirene silenciada. O vigilante continua a procurar na última zona relevante.");
                    break;
            }
        }

        void UseDoor(HeistGame game)
        {
            SetOpen(!opened, true);
            game.Tone(noisy ? 180 : 340);
            if (noisy)
            {
                game.Noise(transform.position, noiseRange);
                game.Discover("ruido", "As portas metálicas e os objetos lançados desviam a ronda para a origem do som.");
            }
        }

        void PickUp(HeistGame game)
        {
            if (used)
                return;
            used = true;
            game.Take(id);
            game.Say("Recolhido: " + label);
            game.Tone(680);
            gameObject.SetActive(false);
        }

        void InstallFuse(HeistGame game)
        {
            if (game.VaultPower)
            {
                game.Say("Alimentação auxiliar já estável.");
                return;
            }
            if (!game.Has("fuse"))
            {
                game.Say("Falta um fusível de 16 A. Consulta a oficina de manutenção.");
                return;
            }
            game.VaultPower = true;
            game.Take("fuse-installed");
            game.Say("Fusível instalado. Terminal do arquivo de valores alimentado.");
            game.Tone(520);
            game.Noise(transform.position, noiseRange);
        }

        void UseKeypad(HeistGame game)
        {
            if (!game.VaultPower)
            {
                game.Say("Terminal sem energia. A caixa de alimentação está junto da entrada do arquivo.");
                return;
            }
            game.OpenKeypad(this);
        }

        void PickUpBottle(HeistGame game)
        {
            if (game.CarryingCase)
            {
                game.Say("Pousa a mala com Q antes de preparar uma distração.");
                return;
            }
            if (game.HasThrowable)
            {
                game.Say("Já tens uma garrafa. G para lançar.");
                return;
            }
            game.HasThrowable = true;
            used = true;
            gameObject.SetActive(false);
            game.Say("Garrafa recolhida. G para lançar uma distração.");
        }

        void PushCrate(HeistGame game)
        {
            var body = GetComponent<Rigidbody>();
            if (body != null)
                body.AddForce(game.player.transform.forward * 3 + Vector3.up * .25f, ForceMode.VelocityChange);
            game.Noise(transform.position, noiseRange);
            game.Tone(240);
        }
    }
}
