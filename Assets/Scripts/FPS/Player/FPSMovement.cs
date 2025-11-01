using UnityEngine;
using UnityEngine;
using UnityEngine.Events;

namespace FPS
{
    /// Gère le mouvement du joueur
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class FPSMovement : MonoBehaviour
    {
        [Header("Events")]
        public UnityEvent<float> OnSpeedChanged = new UnityEvent<float>();

        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float sprintSpeed = 8f;
        [SerializeField] private float jumpHeight = 1.5f;
        [SerializeField] private float gravity = -9.81f;
        [SerializeField] private float gravityMultiplier = 2f;
        [SerializeField] private float increaseSpeedFactor = 25f;
        [SerializeField] private float speedLimit = 20f;
        
        [Header("Jump Settings")]
        [SerializeField, Tooltip("Temps après avoir quitté le sol pendant lequel on peut encore sauter")]
        private float coyoteTime = 0.15f;

        private float defaultMoveSpeed;

        [Header("Air Control")]
        [SerializeField, Tooltip("Contrôle en l'air (0 = aucun, 1 = identique au sol)")]
        private float airControlFactor = 0.4f;
        [SerializeField, Tooltip("Conserver la vitesse horizontale lors du saut")]
        private bool preserveJumpMomentum = false;
        [SerializeField, Tooltip("Multiplicateur de momentum lors du saut (1 = vitesse normale, >1 = boost)")]
        private float jumpMomentumMultiplier = 1f;
        [SerializeField, Tooltip("Vitesse maximale en l'air")]
        private float maxAirSpeed = 10f;

        [Header("Ground Check")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundDistance = 0.4f;
        [SerializeField] private LayerMask groundMask;

        private Rigidbody rb;
        private Vector3 moveDirection = Vector3.zero;
        private Vector3 jumpMomentum = Vector3.zero;
        private bool isGrounded;
        private float coyoteTimeCounter;

        public bool IsGrounded => isGrounded;
        public bool IsMoving { get; private set; }
        public float CurrentSpeed { get; private set; }
        
        // Méthode pour forcer la vitesse au max (utilisée par le dash)
        public void SetSpeedToMax()
        {
            moveSpeed = speedLimit;
        }
        
        // Méthode pour appliquer un momentum externe (utilisée par le dash)
        public void ApplyExternalMomentum(Vector3 momentum)
        {
            jumpMomentum = momentum;
            // S'assurer que le joueur garde ce momentum
            moveSpeed = Mathf.Max(moveSpeed, momentum.magnitude);
        }

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            rb.freezeRotation = true; // Empêche le Rigidbody de basculer

            defaultMoveSpeed = moveSpeed;
        }

        private void Update()
        {
            IncreaseSpeed();
            HandleGroundCheck();
        }

        private void FixedUpdate()
        {
            // Calculer la direction de mouvement
            Vector3 desiredDirection = (transform.right * currentInput.x + transform.forward * currentInput.y).normalized;

            if (isGrounded)
            {
                // Mouvement au sol
                Vector3 targetVelocity = desiredDirection * CurrentSpeed;
                targetVelocity.y = rb.velocity.y; // Conserver la vélocité verticale
                rb.velocity = Vector3.Lerp(rb.velocity, targetVelocity, 0.1f); // Lissage pour éviter les mouvements brusques
            }
            else
            {
                // Contrôle en l'air
                Vector3 airMove = desiredDirection * CurrentSpeed * airControlFactor;
                Vector3 newVelocity = rb.velocity;
                newVelocity.x = Mathf.Lerp(rb.velocity.x, airMove.x, airControlFactor);
                newVelocity.z = Mathf.Lerp(rb.velocity.z, airMove.z, airControlFactor);
                rb.velocity = newVelocity;
            }

            // Saut
            if (jumpQueued && (isGrounded || coyoteTimeCounter > 0f))
            {
                // Réinitialiser la vélocité verticale pour un saut constant
                rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);

                // Appliquer une force de saut
                rb.AddForce(Vector3.up * Mathf.Sqrt(jumpHeight * -2f * Mathf.Abs(gravity)), ForceMode.Impulse);

                // Consommer le saut
                jumpQueued = false;
                coyoteTimeCounter = 0f;
            }

            // Gérer la gravité manuellement
            rb.AddForce(Vector3.up * gravity * gravityMultiplier, ForceMode.Acceleration);
        }

        // Cette méthode sera appelée par le PlayerController

        private void IncreaseSpeed()
        {
            float previousSpeed = moveSpeed;

            if (IsMoving)
            {
                if (moveSpeed < speedLimit)
                {
                    moveSpeed += increaseSpeedFactor * Time.deltaTime;
                    if (CurrentSpeed > maxAirSpeed)
                    {
                        CurrentSpeed = maxAirSpeed;
                    }
                }
            }
            else
            {
                moveSpeed = defaultMoveSpeed;
            }

            if (!Mathf.Approximately(previousSpeed, moveSpeed))
            {
                OnSpeedChanged?.Invoke(moveSpeed);
            }
        }

        private Vector2 currentInput;
        private bool jumpQueued;

        public void Move(Vector2 input, bool sprint, bool jump)
        {
            // Calculer la direction de mouvement
            Vector3 desired = (transform.right * input.x + transform.forward * input.y);
            
            CurrentSpeed = sprint ? sprintSpeed : moveSpeed;
            IsMoving = desired.sqrMagnitude > 0.01f;

            currentInput = input;

            if (jump)
            {
                jumpQueued = true;
            }
        }

        private void HandleGroundCheck()
        {
            if (groundCheck != null && groundMask != 0)
            {
                isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
            }
            else
            {
                // Fallback si groundCheck n'est pas configuré
                isGrounded = Physics.Raycast(transform.position, Vector3.down, 1.1f, groundMask);
            }

            // Gérer le coyote time
            if (isGrounded)
            {
                coyoteTimeCounter = coyoteTime;
            }
            else
            {
                coyoteTimeCounter -= Time.deltaTime;
            }

            // Limiter la vélocité verticale à l'atterrissage pour éviter les rebonds
            if (isGrounded && rb.velocity.y < 0)
            {
                // Appliquer une petite force vers le bas pour "coller" au sol
                rb.velocity = new Vector3(rb.velocity.x, -2f, rb.velocity.z);
            }
        }

        public Rigidbody Rigidbody => rb;
    }
}
