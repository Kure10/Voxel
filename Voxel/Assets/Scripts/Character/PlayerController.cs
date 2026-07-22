using UnityEngine;

namespace Character
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        [Header("References")]
        public Camera PlayerCamera;

        [Header("Movement")]
        public float MoveSpeed = 5f;
        public float Gravity = -20f;
        public float JumpHeight = 1.5f;

        [Header("Mouse Look")]
        public float MouseSensitivity = 2f;
        public float MinLookAngle = -90f;
        public float MaxLookAngle = 90f;

        private CharacterController _controller;
        private float _verticalVelocity;
        private float _cameraPitch;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Update()
        {
            HandleMouseLook();
            HandleMovement();
        }

        private void HandleMouseLook()
        {
            float mouseX = Input.GetAxis("Mouse X") * MouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * MouseSensitivity;

            transform.Rotate(Vector3.up * mouseX);

            _cameraPitch -= mouseY;
            _cameraPitch = Mathf.Clamp(_cameraPitch, MinLookAngle, MaxLookAngle);
            PlayerCamera.transform.localRotation = Quaternion.Euler(_cameraPitch, 0f, 0f);
        }

        private void HandleMovement()
        {
            bool isGrounded = _controller.isGrounded;
            if (isGrounded && _verticalVelocity < 0f)
                _verticalVelocity = -2f;

            float inputX = Input.GetAxisRaw("Horizontal");
            float inputZ = Input.GetAxisRaw("Vertical");
            Vector3 moveDirection = (transform.right * inputX + transform.forward * inputZ).normalized;

            if (isGrounded && Input.GetButtonDown("Jump"))
                _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

            _verticalVelocity += Gravity * Time.deltaTime;

            Vector3 velocity = moveDirection * MoveSpeed;
            velocity.y = _verticalVelocity;

            _controller.Move(velocity * Time.deltaTime);
        }
    }
}