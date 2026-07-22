using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace After.Main
{
    public class CoreGameInputsSystem : MonoBehaviour, IManager
    {
        public event Action<Vector2> OnMove;
        public event Action<Vector2> OnLook;
        public event Action OnJumpPerformed;

        public event Action OnMineStarted;
        public event Action OnMineCanceled;
        public event Action OnBuildPerformed;

        private InputSystem_Actions _gameInputs;

        public void Initialize()
        {
            if (_gameInputs != null)
                return;

            _gameInputs = new InputSystem_Actions();
        }

        private void OnEnable()
        {
            if (_gameInputs == null)
                Initialize(); // safety net if OnEnable fires before Initialize() is called externally

            _gameInputs.Player.Enable();

            _gameInputs.Player.Move.performed += HandleMovePerformed;
            _gameInputs.Player.Move.canceled += HandleMoveCanceled;

            _gameInputs.Player.Look.performed += HandleLookPerformed;
            _gameInputs.Player.Look.canceled += HandleLookCanceled;

            _gameInputs.Player.Jump.performed += HandleJumpPerformed;

            _gameInputs.Player.Attack.started += HandleMineStarted;
            _gameInputs.Player.Attack.canceled += HandleMineCanceled;

            _gameInputs.Player.Interact.performed += HandleBuildPerformed;
        }

        private void OnDisable()
        {
            _gameInputs.Player.Move.performed -= HandleMovePerformed;
            _gameInputs.Player.Move.canceled -= HandleMoveCanceled;

            _gameInputs.Player.Look.performed -= HandleLookPerformed;
            _gameInputs.Player.Look.canceled -= HandleLookCanceled;

            _gameInputs.Player.Jump.performed -= HandleJumpPerformed;

            _gameInputs.Player.Attack.started -= HandleMineStarted;
            _gameInputs.Player.Attack.canceled -= HandleMineCanceled;

            _gameInputs.Player.Interact.performed -= HandleBuildPerformed;

            _gameInputs.Player.Disable();
        }

        private void HandleMovePerformed(InputAction.CallbackContext ctx) => OnMove?.Invoke(ctx.ReadValue<Vector2>());
        private void HandleMoveCanceled(InputAction.CallbackContext ctx) => OnMove?.Invoke(Vector2.zero);

        private void HandleLookPerformed(InputAction.CallbackContext ctx) => OnLook?.Invoke(ctx.ReadValue<Vector2>());
        private void HandleLookCanceled(InputAction.CallbackContext ctx) => OnLook?.Invoke(Vector2.zero);

        private void HandleJumpPerformed(InputAction.CallbackContext ctx) => OnJumpPerformed?.Invoke();

        private void HandleMineStarted(InputAction.CallbackContext ctx) => OnMineStarted?.Invoke();
        private void HandleMineCanceled(InputAction.CallbackContext ctx) => OnMineCanceled?.Invoke();

        private void HandleBuildPerformed(InputAction.CallbackContext ctx) => OnBuildPerformed?.Invoke();
    }
}