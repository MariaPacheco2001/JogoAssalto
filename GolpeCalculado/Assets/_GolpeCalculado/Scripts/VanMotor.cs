using UnityEngine;

namespace GolpeCalculado
{
    // Condução da carrinha com WheelColliders: acelerar, travar, marcha-atrás, direção e travão de mão.
    [RequireComponent(typeof(Rigidbody))]
    public sealed class VanMotor : MonoBehaviour
    {
        public WheelCollider[] wheels;
        public Transform[] wheelVisuals;
        public Transform seat, entry, leftExit, rightExit;
        public AudioSource engine;
        public bool Occupied { get; private set; }
        public bool Handbrake { get; private set; } = true;
        public bool ExternalInput;
        public float ThrottleInput, SteerInput;
        public bool BrakeInput;
        public Rigidbody Body { get; private set; }
        public float SpeedKmh => Body == null ? 0 : Body.linearVelocity.magnitude * 3.6f;
        public float SignedSpeed => Vector3.Dot(Body.linearVelocity, transform.forward);
        float steering;
        void Awake()
        {
            Body = GetComponent<Rigidbody>();
            // Centro de massa baixo para a carrinha não tombar nas curvas.
            Body.centerOfMass = new Vector3(0, .55f, -.1f);
        }

        // Ao sair, o travão de mão fica sempre puxado.
        public void SetOccupied(bool value)
        {
            Occupied = value;
            if (!value)
                Handbrake = true;
        }

        public void SetHandbrake(bool value)
        {
            Handbrake = value;
        }

        void Update()
        {
            if (Time.timeScale == 0) return;
            if (Occupied && !ExternalInput && Input.GetKeyDown(KeyCode.Space)) Handbrake = !Handbrake;
            for (int i = 0; i < wheels.Length; i++)
            {
                wheels[i].GetWorldPose(out Vector3 p, out Quaternion q);
                wheelVisuals[i].SetPositionAndRotation(p, q * Quaternion.Euler(0, 0, 90));
            }
            if (engine != null)
            {
                engine.volume = Occupied ? .12f : .025f;
                engine.pitch = .65f + Mathf.Clamp(SpeedKmh / 38, 0, 1.3f);
            }
        }
        void FixedUpdate()
        {
            float throttle = Occupied ? (ExternalInput ? ThrottleInput : Input.GetAxisRaw("Vertical")) : 0;
            float turn = Occupied ? (ExternalInput ? SteerInput : Input.GetAxisRaw("Horizontal")) : 0;
            bool brake = Occupied && (ExternalInput ? BrakeInput : Input.GetKey(KeyCode.LeftShift));
            // Carregar no sentido contrário ao movimento trava antes de inverter a marcha.
            bool directionBrake = Mathf.Abs(SignedSpeed) > .65f && throttle * SignedSpeed < 0;
            float targetSteering = turn * Mathf.Lerp(30, 12, Mathf.Clamp01(SpeedKmh / 45));
            steering = Mathf.MoveTowards(steering, targetSteering, 75 * Time.fixedDeltaTime);
            for (int i = 0; i < wheels.Length; i++)
            {
                wheels[i].steerAngle = i < 2 ? steering : 0;
                wheels[i].brakeTorque = Handbrake || brake ? 6500 : directionBrake ? 4500 : Mathf.Abs(throttle) < .05f ? 160 : 0;
                bool limited = throttle > 0 ? SignedSpeed > 12.5f : SignedSpeed < -4;
                wheels[i].motorTorque = !Handbrake && !brake && !directionBrake && !limited ? throttle * 1050 : 0;
            }
            // Barra estabilizadora simples: evita que a carrinha incline demasiado nas curvas.
            for (int axle = 0; axle < 2; axle++)
            {
                var a = wheels[axle * 2]; var b = wheels[axle * 2 + 1];
                bool ga = a.GetGroundHit(out WheelHit ha), gb = b.GetGroundHit(out WheelHit hb);
                float ta = ga ? (-a.transform.InverseTransformPoint(ha.point).y - a.radius) / a.suspensionDistance : 1;
                float tb = gb ? (-b.transform.InverseTransformPoint(hb.point).y - b.radius) / b.suspensionDistance : 1;
                float force = (ta - tb) * 4500;
                if (ga) Body.AddForceAtPosition(-transform.up * force, a.transform.position);
                if (gb) Body.AddForceAtPosition(transform.up * force, b.transform.position);
            }
        }
    }
}
