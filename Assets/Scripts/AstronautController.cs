using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

[RequireComponent(typeof(CharacterController))]
public class AstronautController : MonoBehaviour
{
    [SerializeField] private Transform playerCamera;
    [SerializeField, Min(0f)] private float moveSpeed = 4f;
    [SerializeField, Min(0f)] private float mouseSensitivity = 0.1f;
    [SerializeField] private float gravity = -20f;

    private CharacterController controller;
    private float verticalSpeed;
    private float pitch;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (playerCamera == null)
        {
            Debug.LogError("Astronotun kamerası bağlanmamış.", this);
            enabled = false;
            return;
        }

        // Göz hizasındaki kameranın kapsülün içini görmesini önler.
        var body = GetComponent<MeshRenderer>();
        if (body != null)
            body.shadowCastingMode = ShadowCastingMode.ShadowsOnly;
    }

    private void Start()
    {
        SetCursorLocked(true);
    }

    private void Update()
    {
        if (!Application.isFocused)
            return;

        var keyboard = Keyboard.current;
        var mouse = Mouse.current;

        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            SetCursorLocked(false);
        else if (Cursor.lockState != CursorLockMode.Locked && mouse != null
                 && mouse.leftButton.wasPressedThisFrame)
        {
            SetCursorLocked(true);
            return;
        }

        bool controlsActive = Cursor.lockState == CursorLockMode.Locked;
        if (controlsActive && mouse != null)
        {
            // Fare delta değeri karedeki hareketi verir; deltaTime ile çarpılmaz.
            Vector2 look = mouse.delta.ReadValue() * mouseSensitivity;
            transform.Rotate(0f, look.x, 0f);
            pitch = Mathf.Clamp(pitch - look.y, -85f, 85f);
            playerCamera.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        Vector2 input = Vector2.zero;
        if (controlsActive && keyboard != null)
        {
            input.x = (keyboard.dKey.isPressed ? 1f : 0f)
                      - (keyboard.aKey.isPressed ? 1f : 0f);
            input.y = (keyboard.wKey.isPressed ? 1f : 0f)
                      - (keyboard.sKey.isPressed ? 1f : 0f);
            input = Vector2.ClampMagnitude(input, 1f);
        }

        if (controller.isGrounded && verticalSpeed < 0f)
            verticalSpeed = -2f;
        verticalSpeed += gravity * Time.deltaTime;

        Vector3 velocity = (transform.right * input.x + transform.forward * input.y) * moveSpeed;
        velocity.y = verticalSpeed;
        CollisionFlags collisions = controller.Move(velocity * Time.deltaTime);
        if ((collisions & CollisionFlags.Below) != 0 && verticalSpeed < 0f)
            verticalSpeed = -2f;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
            SetCursorLocked(false);
    }

    private void OnDisable()
    {
        SetCursorLocked(false);
    }

    private static void SetCursorLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
