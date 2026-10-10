using UnityEngine;
using UnityEngine.InputSystem;

/// CharacterController-based first-person movement + mouse look. Uses the new Input System
/// directly (this project's Active Input Handling is "Input System Package (New)" only, no
/// legacy Input.GetAxis available), matching the style already used by StudentTools/FlyCamera.
[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    public static FirstPersonController Instance { get; private set; }

    [Header("Move")]
    public float moveSpeed = 3.5f;
    public float gravity = -9.81f;

    [Header("Look")]
    public float eyeHeight = 1.6f;
    public float mouseSensitivity = 0.12f;
    public float minPitch = -85f;
    public float maxPitch = 85f;

    public Camera Camera { get; private set; }

    CharacterController _controller;
    float _yaw, _pitch;
    float _verticalVelocity;
    bool _inputEnabled = true;
    int _resumeFrame = -1;
    public bool InputEnabled => _inputEnabled && Time.frameCount > _resumeFrame;

    void Awake()
    {
        Instance = this;
        _controller = GetComponent<CharacterController>();

        Camera = GetComponentInChildren<Camera>();
        _yaw = transform.eulerAngles.y;
    }

    void Start()
    {
        SetInputEnabled(true);
    }

    public void SetInputEnabled(bool enabled)
    {
        _inputEnabled = enabled;
        if (enabled) _resumeFrame = Time.frameCount; // Do not reuse the key press that closed an overlay.
        Cursor.lockState = enabled ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !enabled;
    }

    void Update()
    {
        if (!InputEnabled || Keyboard.current == null) return;

        // Look
        if (Mouse.current != null)
        {
            Vector2 delta = Mouse.current.delta.ReadValue();
            _yaw += delta.x * mouseSensitivity;
            _pitch = Mathf.Clamp(_pitch - delta.y * mouseSensitivity, minPitch, maxPitch);
            transform.rotation = Quaternion.Euler(0, _yaw, 0);
            if (Camera != null)
                Camera.transform.localRotation = Quaternion.Euler(_pitch, 0, 0);
        }

        // Move
        Keyboard kb = Keyboard.current;
        float x = Axis(kb.dKey.isPressed, kb.aKey.isPressed);
        float z = Axis(kb.wKey.isPressed, kb.sKey.isPressed);
        Vector3 wish = transform.right * x + transform.forward * z;
        if (wish.sqrMagnitude > 1f) wish.Normalize();

        if (_controller.isGrounded && _verticalVelocity < 0f)
            _verticalVelocity = -1f; // small stick-to-ground value, not 0 (keeps isGrounded true)
        _verticalVelocity += gravity * Time.deltaTime;

        Vector3 motion = wish * moveSpeed + Vector3.up * _verticalVelocity;
        _controller.Move(motion * Time.deltaTime);
    }

    static float Axis(bool positive, bool negative) => (positive ? 1f : 0f) - (negative ? 1f : 0f);
}
