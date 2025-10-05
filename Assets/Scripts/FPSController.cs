using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FPSController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 4.5f;
    public float sprintSpeed = 6.5f;
    public float jumpHeight = 1.1f;
    public float gravity = -20f;

    [Header("Mouse Look")]
    public Transform cam;              // assign your Camera
    public float mouseSensitivity = 1.6f; // adjust to taste
    public float pitchMin = -80f;
    public float pitchMax = 80f;
    public bool lockCursor = true;

    [Header("Smoothing")]
    public float moveSharpness = 12f;  // higher = snappier
    public float lookSharpness = 20f;

    CharacterController _cc;
    Vector3 _velocity;     // current world velocity
    float _targetSpeed;    // desired horizontal speed
    Vector3 _moveInput;    // desired horizontal input (xz)
    float _pitch;          // camera pitch

    void Awake()
    {
        _cc = GetComponent<CharacterController>();
        if (cam == null) cam = Camera.main != null ? Camera.main.transform : null;
        if (lockCursor) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
    }

    void Update()
    {
        // ----- Look -----
        float mx = Input.GetAxis("Mouse X");
        float my = Input.GetAxis("Mouse Y");
        float yawDelta = mx * mouseSensitivity * 10f * Time.deltaTime;
        float pitchDelta = -my * mouseSensitivity * 10f * Time.deltaTime;

        transform.rotation = Quaternion.Slerp(transform.rotation,
            Quaternion.Euler(0f, transform.eulerAngles.y + yawDelta, 0f), lookSharpness * Time.deltaTime);

        _pitch = Mathf.Clamp(_pitch + pitchDelta, pitchMin, pitchMax);
        if (cam) cam.localRotation = Quaternion.Slerp(cam.localRotation,
            Quaternion.Euler(_pitch, 0f, 0f), lookSharpness * Time.deltaTime);

        // ----- Move input -----
        float h = Input.GetAxisRaw("Horizontal"); // A/D or Left/Right
        float v = Input.GetAxisRaw("Vertical");   // W/S or Up/Down
        Vector3 input = new Vector3(h, 0f, v);
        input = Vector3.ClampMagnitude(input, 1f);

        // convert to world space relative to facing
        _moveInput = transform.TransformDirection(input);

        bool sprint = Input.GetKey(KeyCode.LeftShift);
        _targetSpeed = (sprint ? sprintSpeed : walkSpeed) * _moveInput.magnitude;

        // smooth horizontal velocity (keep vertical separate)
        Vector3 horizVel = Vector3.ProjectOnPlane(_velocity, Vector3.up);
        Vector3 desired = _moveInput * _targetSpeed;
        horizVel = Vector3.Lerp(horizVel, desired, 1f - Mathf.Exp(-moveSharpness * Time.deltaTime));

        // ----- Grounding & jump -----
        if (_cc.isGrounded)
        {
            _velocity.y = -0.5f; // keep grounded
            if (Input.GetButtonDown("Jump")) // Space by default
            {
                _velocity.y = Mathf.Sqrt(-2f * gravity * jumpHeight);
            }
        }
        else
        {
            _velocity.y += gravity * Time.deltaTime;
        }

        _velocity = horizVel + Vector3.up * _velocity.y;

        // ----- Move -----
        _cc.Move(_velocity * Time.deltaTime);

        // Escape toggles cursor
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            lockCursor = !lockCursor;
            Cursor.lockState = lockCursor ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !lockCursor;
        }
    }
}
