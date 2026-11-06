using UnityEngine;

namespace GolpeCalculado
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerMotor : MonoBehaviour
    {
        public Camera view;
        public bool controlled = true;
        public float walkSpeed = 3.4f, runSpeed = 5.5f, crouchSpeed = 1.7f;
        public float sensitivity = 2f;
        public bool invertY;
        public float SpeedMultiplier = 1f;
        public bool ExternalInput;
        public Vector2 MoveInput, LookInput;
        public bool RunInput, CrouchInput, JumpInput;
        public bool IsCrouching { get; private set; }
        public CharacterController Body { get; private set; }
        float pitch, vertical;
        void Awake() { Body = GetComponent<CharacterController>(); }
        void Update()
        {
            if (!controlled || Time.timeScale == 0) return;
            Vector2 move = ExternalInput ? MoveInput : new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            Vector2 look = ExternalInput ? LookInput : new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            bool run = ExternalInput ? RunInput : Input.GetKey(KeyCode.LeftShift);
            bool crouch = ExternalInput ? CrouchInput : Input.GetKey(KeyCode.LeftControl);
            bool jump = ExternalInput ? JumpInput : Input.GetKeyDown(KeyCode.Space);
            transform.Rotate(0, look.x * sensitivity, 0);
            pitch = Mathf.Clamp(pitch - look.y * sensitivity * (invertY ? -1 : 1), -80, 80);
            view.transform.localRotation = Quaternion.Euler(pitch, 0, 0);
            // Keep the feet fixed. Standing up is blocked by ceilings and other colliders.
            if (!crouch && IsCrouching)
            {
                foreach (var hit in Physics.OverlapCapsule(transform.position + Vector3.up * .4f,
                    transform.position + Vector3.up * 1.5f, .29f, ~0, QueryTriggerInteraction.Ignore))
                    if (hit != Body && !hit.transform.IsChildOf(transform)) crouch = true;
            }
            bool grounded = Body.isGrounded;
            if (IsCrouching != crouch)
            {
                Body.height = crouch ? 1.05f : 1.8f;
                Body.center = Vector3.up * Body.height * .5f;
            }
            IsCrouching = crouch;
            view.transform.localPosition = Vector3.Lerp(view.transform.localPosition,
                Vector3.up * (crouch ? .9f : 1.65f), 15 * Time.deltaTime);
            if (grounded && vertical < 0) vertical = -2;
            if (jump && grounded && !crouch) vertical = Mathf.Sqrt(2 * 18f * .8f);
            vertical -= 18f * Time.deltaTime;
            var desired = transform.TransformDirection(new Vector3(move.x, 0, move.y).normalized);
            Body.Move((desired * (crouch ? crouchSpeed : run ? runSpeed : walkSpeed) * SpeedMultiplier + Vector3.up * vertical) * Time.deltaTime);
            JumpInput = false;
        }
        public void Teleport(Vector3 feet, Quaternion rotation)
        {
            bool wasEnabled = Body.enabled;
            Body.enabled = false;
            transform.SetPositionAndRotation(feet, rotation);
            Body.enabled = wasEnabled;
            vertical = 0; pitch = 0;
        }
    }
}
