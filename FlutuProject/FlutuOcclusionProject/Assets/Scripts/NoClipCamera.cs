using UnityEngine;
using UnityEngine.InputSystem;

public class NoclipCamera : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float fastSpeedMultiplier = 3f;
    public float mouseSensitivity = 0.15f;

    private float yaw;
    private float pitch;

    void Start()
    {
        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;

        if (keyboard == null || mouse == null)
            return;

        // Mouse look
        Vector2 mouseDelta = mouse.delta.ReadValue();
        yaw += mouseDelta.x * mouseSensitivity;
        pitch -= mouseDelta.y * mouseSensitivity;
        pitch = Mathf.Clamp(pitch, -90f, 90f);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

        // Movement input
        float x = 0f;
        float y = 0f;
        float z = 0f;

        if (keyboard.aKey.isPressed) x -= 1f;
        if (keyboard.dKey.isPressed) x += 1f;
        if (keyboard.sKey.isPressed) z -= 1f;
        if (keyboard.wKey.isPressed) z += 1f;

        if (keyboard.leftCtrlKey.isPressed) y -= 1f;
        if (keyboard.spaceKey.isPressed) y += 1f;

        float speed = keyboard.leftShiftKey.isPressed
            ? moveSpeed * fastSpeedMultiplier
            : moveSpeed;

        Vector3 direction = new Vector3(x, y, z).normalized;
        transform.Translate(direction * speed * Time.deltaTime, Space.Self);

        // Unlock cursor
        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}