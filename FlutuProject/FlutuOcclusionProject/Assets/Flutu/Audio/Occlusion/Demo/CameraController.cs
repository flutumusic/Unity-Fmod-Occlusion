using UnityEngine;
using UnityEngine.InputSystem;

namespace Flutu.Audio.Occlusion
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 10f;
        [SerializeField] private float fastSpeedMultiplier = 3f;
        [SerializeField] private float mouseSensitivity = 0.15f;

        private float yaw;
        private float pitch;

        private void Start()
        {
            InitializeCameraRotation();
            LockCursor();
        }

        private void Update()
        {
            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            if (keyboard == null || mouse == null)
                return;

            HandleMouseLook(mouse);
            HandleMovement(keyboard);
            HandleCursorToggle(keyboard);
        }

        private void InitializeCameraRotation()
        {
            Vector3 angles = transform.eulerAngles;
            yaw = angles.y;
            pitch = angles.x;
        }

        private void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void HandleMouseLook(Mouse mouse)
        {
            Vector2 mouseDelta = mouse.delta.ReadValue();
            yaw += mouseDelta.x * mouseSensitivity;
            pitch -= mouseDelta.y * mouseSensitivity;
            pitch = Mathf.Clamp(pitch, -90f, 90f);

            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        private void HandleMovement(Keyboard keyboard)
        {
            Vector3 direction = GetMovementInput(keyboard);
            float speed = GetCurrentSpeed(keyboard);

            transform.Translate(direction * speed * Time.deltaTime, Space.Self);
        }

        private Vector3 GetMovementInput(Keyboard keyboard)
        {
            float x = 0f;
            float y = 0f;
            float z = 0f;

            if (keyboard.aKey.isPressed) x -= 1f;
            if (keyboard.dKey.isPressed) x += 1f;
            if (keyboard.sKey.isPressed) z -= 1f;
            if (keyboard.wKey.isPressed) z += 1f;
            if (keyboard.leftCtrlKey.isPressed) y -= 1f;
            if (keyboard.spaceKey.isPressed) y += 1f;

            return new Vector3(x, y, z).normalized;
        }

        private float GetCurrentSpeed(Keyboard keyboard)
        {
            return keyboard.leftShiftKey.isPressed
                ? moveSpeed * fastSpeedMultiplier
                : moveSpeed;
        }

        private void HandleCursorToggle(Keyboard keyboard)
        {
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }
}