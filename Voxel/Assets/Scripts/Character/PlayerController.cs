using After.Main;
using UnityEngine;

namespace Character
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : Controller
    {
        [Inject] private CoreGameInputsSystem _coreGameInputsSystem;

        [Header("References")]
        public Camera PlayerCamera;

        [Header("Movement")]
        public float MoveSpeed = 5f;
        public float JumpHeight = 1.5f;

        [Header("Mouse Look")]
        public float MouseSensitivity = 0.1f;
        public float MinLookAngle = -90f;
        public float MaxLookAngle = 90f;

        private CharacterController _controller;
        private float _verticalVelocity;
        private float _cameraPitch;

        private Vector2 _moveInput;
        private bool _jumpRequested;

        public override void Initialize()
        {
            base.Initialize();
            _controller = GetComponent<CharacterController>();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;

            _coreGameInputsSystem.OnMove += HandleMove;
            _coreGameInputsSystem.OnLook += HandleLook;
            _coreGameInputsSystem.OnJumpPerformed += HandleJump;
        }

        // Look is applied immediately, event-driven — no caching, no polling.
        private void HandleLook(Vector2 look)
        {
            float mouseX = look.x * MouseSensitivity;
            float mouseY = look.y * MouseSensitivity;

            transform.Rotate(Vector3.up * mouseX);

            _cameraPitch -= mouseY;
            _cameraPitch = Mathf.Clamp(_cameraPitch, MinLookAngle, MaxLookAngle);
            PlayerCamera.transform.localRotation = Quaternion.Euler(_cameraPitch, 0f, 0f);
        }

        // Move/Jump just update state; CharacterController still needs a per-tick call to apply gravity/movement.
        private void HandleMove(Vector2 move) => _moveInput = move;
        private void HandleJump() => _jumpRequested = true;

        private void FixedUpdate()
        {
            bool isGrounded = _controller.isGrounded;
            if (isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;

            Vector3 moveDirection = (transform.right * _moveInput.x + transform.forward * _moveInput.y).normalized;

            if (isGrounded && _jumpRequested)
                _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Physics.gravity.y);
            _jumpRequested = false;

            _verticalVelocity += Physics.gravity.y * Time.fixedDeltaTime;

            Vector3 velocity = moveDirection * MoveSpeed;
            velocity.y = _verticalVelocity;

            _controller.Move(velocity * Time.fixedDeltaTime);
        }

        protected override void OnControllerDestroy()
        {
            base.OnControllerDestroy();
            if (_coreGameInputsSystem == null) return;
            _coreGameInputsSystem.OnMove -= HandleMove;
            _coreGameInputsSystem.OnLook -= HandleLook;
            _coreGameInputsSystem.OnJumpPerformed -= HandleJump;
        }
    }
}