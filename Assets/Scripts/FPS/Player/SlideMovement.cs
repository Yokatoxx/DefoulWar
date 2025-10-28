using Proto3GD.FPS;
using UnityEngine;

public class SlideMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform orientation;
    [Tooltip("Assigner un enfant visuel (mesh). Évite de scaler l'objet qui porte le CharacterController.")]
    [SerializeField] private Transform playerObj;

    private CharacterController controller;
    private FPSMovement movement;

    [Header("Input")]
    [SerializeField] private KeyCode slideKey = KeyCode.LeftControl;
    private float horizontalInput;
    private float verticalInput;

    [Header("Sliding")]
    [Tooltip("Durée max du slide (s)")]
    [SerializeField] private float maxSlideTime = 0.8f;
    [Tooltip("Accélération ajoutée par l'input (m/s²)")]
    [SerializeField] private float slideAccel = 20f;
    [Tooltip("Impulsion initiale au démarrage (m/s)")]
    [SerializeField] private float startImpulse = 8f;
    [Tooltip("Friction quand angle trop faible ou input nul (m/s²)")]
    [SerializeField] private float slideFriction = 10f;
    [Tooltip("Vitesse de base max (m/s)")]
    [SerializeField] private float maxSlideSpeed = 12f;
    [Tooltip("Scale Y visuelle pendant le slide (sur playerObj uniquement)")]
    [SerializeField] private float slideYScale = 0.5f;

    [Header("Slope")]
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float groundRayStart = 0.6f;
    [SerializeField] private float groundRayDistance = 1.5f;
    [Tooltip("Accélération due à la pente (m/s²), multipliée par sin(angle)")]
    [SerializeField] private float slopeAcceleration = 25f;
    [Tooltip("Angle minimal (°) pour commencer à accélérer avec la pente")]
    [SerializeField] private float minAccelSlopeAngle = 5f;
    [Tooltip("Bonus sur la Vmax selon sin(angle)")]
    [SerializeField] private float slopeVmaxBonus = 8f;

    [Header("CharacterController Resize (optionnel)")]
    [SerializeField] private bool resizeControllerWhileSliding = true;
    [SerializeField] private float controllerSlideHeight = 1.0f;

    private float slideTimer;
    private bool isSliding;
    private Vector3 slideVelocity; // horizontal only
    private Vector3 startLocalScale;

    private float initialControllerHeight;
    private Vector3 initialControllerCenter;

    public bool IsSliding => isSliding;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        movement = GetComponent<FPSMovement>();

        if (controller == null)
        {
            Debug.LogError("[SlideMovement] CharacterController manquant.");
            enabled = false;
            return;
        }

        if (playerObj == null)
        {
            var mr = GetComponentInChildren<MeshRenderer>();
            playerObj = mr != null ? mr.transform : transform;
        }
        startLocalScale = playerObj.localScale;

        initialControllerHeight = controller.height;
        initialControllerCenter = controller.center;

        isSliding = false;
        slideTimer = 0f;
        slideVelocity = Vector3.zero;
    }

    private void Update()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if (Input.GetKeyDown(slideKey) && !isSliding)
            StartSlide();

        if (Input.GetKeyUp(slideKey) && isSliding)
            StopSlide();

        if (isSliding)
            SlidingMovement(Time.deltaTime);
    }

    private void StartSlide()
    {
        isSliding = true;
        slideTimer = 0f;

        // Visuel
        if (playerObj != null)
            playerObj.localScale = new Vector3(startLocalScale.x, slideYScale, startLocalScale.z);

        // Resize CC en conservant les pieds au même niveau
        if (resizeControllerWhileSliding && controller != null)
        {
            float newH = Mathf.Max(0.5f, controllerSlideHeight);
            float bottomY = initialControllerCenter.y - initialControllerHeight * 0.5f;
            float newCenterY = bottomY + newH * 0.5f;
            controller.height = newH;
            controller.center = new Vector3(initialControllerCenter.x, newCenterY, initialControllerCenter.z);
        }

        // Impulsion initiale (horizontale)
        Vector3 fwd = (orientation != null ? orientation.forward : transform.forward);
        fwd.y = 0f;
        fwd.Normalize();
        slideVelocity = fwd * startImpulse;
    }

    private void StopSlide()
    {
        isSliding = false;

        if (playerObj != null)
            playerObj.localScale = startLocalScale;

        if (resizeControllerWhileSliding && controller != null)
        {
            float bottomY = initialControllerCenter.y - initialControllerHeight * 0.5f;
            float newCenterY = bottomY + initialControllerHeight * 0.5f;
            controller.height = initialControllerHeight;
            controller.center = new Vector3(initialControllerCenter.x, newCenterY, initialControllerCenter.z);
        }

        slideVelocity = Vector3.zero;
    }

    private void SlidingMovement(float dt)
    {
        // 1) Direction d'entrée
        Vector3 fwd = (orientation != null ? orientation.forward : transform.forward);
        Vector3 right = (orientation != null ? orientation.right : transform.right);
        fwd.y = 0f; right.y = 0f;
        fwd.Normalize(); right.Normalize();
        Vector3 inputDir = (fwd * verticalInput + right * horizontalInput);
        if (inputDir.sqrMagnitude > 1e-4f) inputDir.Normalize();

        // 2) Slope
        Vector3 slopeDir = Vector3.zero;
        float sinA = 0f;
        float slopeAngle = 0f;
        if (TryGetGround(out RaycastHit hit))
        {
            slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            slopeDir = Vector3.ProjectOnPlane(Vector3.down, hit.normal).normalized;
            sinA = Mathf.Sin(slopeAngle * Mathf.Deg2Rad);
        }

        // 3) Accélération
        if (inputDir.sqrMagnitude > 0f)
            slideVelocity += inputDir * slideAccel * dt;

        if (slopeDir.sqrMagnitude > 0f)
        {
            if (slopeAngle >= minAccelSlopeAngle)
                slideVelocity += slopeDir * (slopeAcceleration * sinA) * dt;
            else
            {
                // friction si pente trop faible
                slideVelocity = ApplyFriction(slideVelocity, slideFriction, dt);
            }
        }
        else
        {
            // En l'air: juste friction légère
            slideVelocity = ApplyFriction(slideVelocity, slideFriction * 0.5f, dt);
        }

        // 4) Clamp vitesse max selon pente
        float vmax = maxSlideSpeed + slopeVmaxBonus * sinA;
        if (slideVelocity.magnitude > vmax)
            slideVelocity = slideVelocity.normalized * vmax;

        // 5) Déplacement horizontal (la gravité reste gérée par FPSMovement)
        Vector3 horizontal = new Vector3(slideVelocity.x, 0f, slideVelocity.z);
        if (horizontal.sqrMagnitude > 0f)
            controller.Move(horizontal * dt);

        // 6) Timer
        slideTimer += dt;
        if (slideTimer >= maxSlideTime)
            StopSlide();
    }

    private Vector3 ApplyFriction(Vector3 v, float friction, float dt)
    {
        float speed = v.magnitude;
        if (speed <= 1e-4f) return Vector3.zero;
        speed = Mathf.Max(0f, speed - friction * dt);
        return (speed > 0f) ? v.normalized * speed : Vector3.zero;
    }

    private bool TryGetGround(out RaycastHit hit)
    {
        Vector3 origin = transform.position + Vector3.up * groundRayStart;
        return Physics.Raycast(origin, Vector3.down, out hit, groundRayDistance, groundMask, QueryTriggerInteraction.Ignore);
    }
}