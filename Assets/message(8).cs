using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float lookSpeed = 2f;
    public float jumpForce = 8f;
    public float gravity = 20f;
    public float leanAngle = 15f;
    public float leanSpeed = 8f;
    public float airControlFactor = 0.4f;

    // Headbob settings
    public float bobFrequency = 1.8f;
    public float bobHorizontalAmplitude = 0.08f;
    public float bobVerticalAmplitude = 0.05f;
    public float bobSmoothing = 8f;

    public float defaultFOV = 60f;
    public float jumpFOV = 45f;
    public float fovTransitionSpeed = 8f;

    private float currentLean = 0f;
    private CharacterController controller;
    private Vector3 moveDirection = Vector3.zero;
    private Camera cam;
    private float cameraPitch = 0f;
    private float targetFOV;
    private bool isJumping = false;

    private float bobTimer = 0f;
    private Vector3 camDefaultLocalPos;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        cam = Camera.main;
        Cursor.lockState = CursorLockMode.Locked;
        targetFOV = defaultFOV;
        cam.fieldOfView = defaultFOV;
        camDefaultLocalPos = cam.transform.localPosition;
    }

    void Update()
    {
        // Rotation horizontale (corps)
        float mouseX = Input.GetAxis("Mouse X") * lookSpeed;
        transform.Rotate(Vector3.up * mouseX);

        // Rotation verticale (caméra)
        float mouseY = Input.GetAxis("Mouse Y") * lookSpeed;
        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, -89f, 89f);

        // Mouvement - VITESSE CONSTANTE
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector3 moveInput = new Vector3(h, 0f, v);
        Vector3 move = moveInput.magnitude > 1f ? moveInput.normalized : moveInput;
        move = transform.TransformDirection(move);

        if (controller.isGrounded)
        {
            moveDirection = move * moveSpeed;
            moveDirection.y = -1f;

            if (isJumping)
            {
                targetFOV = defaultFOV;
                isJumping = false;
            }

            if (Input.GetButtonDown("Jump"))
            {
                moveDirection.y = jumpForce;
                targetFOV = jumpFOV;
                isJumping = true;
            }
        }
        else
        {
            Vector3 airInput = new Vector3(h, 0f, v);
            Vector3 airMove = airInput.magnitude > 1f ? airInput.normalized : airInput;
            airMove = transform.TransformDirection(airMove) * moveSpeed * airControlFactor;
            moveDirection.x = Mathf.Lerp(moveDirection.x, airMove.x, airControlFactor);
            moveDirection.z = Mathf.Lerp(moveDirection.z, airMove.z, airControlFactor);
        }

        // Gravité
        moveDirection.y -= gravity * Time.deltaTime;
        controller.Move(moveDirection * Time.deltaTime);

        // --- LEAN DYNAMIQUE ---
        float targetLean = 0f;
        if (h != 0)
        {
            targetLean = Mathf.Clamp(h, -1f, 1f) * leanAngle;
        }
        if (Input.GetKey(KeyCode.Q)) targetLean = -leanAngle;
        if (Input.GetKey(KeyCode.E)) targetLean = leanAngle;
        currentLean = Mathf.Lerp(currentLean, targetLean, Time.deltaTime * leanSpeed);

        // Applique le lean sur la caméra
        cam.transform.localEulerAngles = new Vector3(cameraPitch, 0, currentLean);

        // --- FOV dynamique fluide ---
        cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * fovTransitionSpeed);

        // --- HEADBOB ---
        bool isMoving = controller.isGrounded && (Mathf.Abs(h) > 0.1f || Mathf.Abs(v) > 0.1f);

        if (isMoving)
        {
            bobTimer += Time.deltaTime * bobFrequency * moveSpeed;
            float bobX = Mathf.Sin(bobTimer) * bobHorizontalAmplitude;
            float bobY = Mathf.Cos(bobTimer * 2f) * bobVerticalAmplitude;
            Vector3 targetBobPos = camDefaultLocalPos + new Vector3(bobX, bobY, 0);
            cam.transform.localPosition = Vector3.Lerp(cam.transform.localPosition, targetBobPos, Time.deltaTime * bobSmoothing);
        }
        else
        {
            bobTimer = 0f;
            cam.transform.localPosition = Vector3.Lerp(cam.transform.localPosition, camDefaultLocalPos, Time.deltaTime * bobSmoothing);
        }
    }

    // ------- Ajout : téléportation safe -------
    public void TeleportTo(Vector3 newPosition)
    {
        // Pour CharacterController, il faut le désactiver avant de déplacer puis réactiver
        controller.enabled = false;
        transform.position = newPosition;
        controller.enabled = true;
    }
}