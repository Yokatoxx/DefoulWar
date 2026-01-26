using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float jumpHeight = 1.6f;
    public float gravity = -9.81f;

    [Header("Look")]
    public Transform playerCamera;
    public float mouseSensitivity = 1.5f;
    public float maxLookAngle = 80f;

    CharacterController cc;
    Vector3 velocity;
    float cameraPitch = 0f;
    bool controlsEnabled = true;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (playerCamera == null && Camera.main != null)
            playerCamera = Camera.main.transform;

        // Lock cursor by default for FPS feel
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        if (!controlsEnabled)
            return;

        HandleLook();
        HandleMove();
    }

    void HandleLook()
    {
        if (playerCamera == null) return;
        Vector2 mouse = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * mouseSensitivity;
        // yaw
        transform.Rotate(Vector3.up * mouse.x);
        // pitch
        cameraPitch -= mouse.y;
        cameraPitch = Mathf.Clamp(cameraPitch, -maxLookAngle, maxLookAngle);
        playerCamera.localEulerAngles = Vector3.right * cameraPitch;
    }

    void HandleMove()
    {
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        Vector3 move = transform.right * input.x + transform.forward * input.y;
        move.Normalize();

        float speed = Input.GetKey(KeyCode.LeftShift) ? runSpeed : walkSpeed;
        Vector3 moveVel = move * speed;

        if (cc.isGrounded && velocity.y < 0f)
            velocity.y = -2f; // small grounding force

        if (Input.GetButtonDown("Jump") && cc.isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        // apply gravity
        velocity.y += gravity * Time.deltaTime;

        Vector3 final = moveVel + new Vector3(0f, velocity.y, 0f);
        cc.Move(final * Time.deltaTime);
    }

    // Public control toggles used by DialogueManager
    public void EnableControls()
    {
        controlsEnabled = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void DisableControls()
    {
        controlsEnabled = false;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
