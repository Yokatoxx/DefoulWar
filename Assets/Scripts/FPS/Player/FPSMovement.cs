using UnityEngine;
using UnityEngine.Rendering;
using TMPro;

namespace Proto3GD.FPS
{
    /// <summary>
    /// Gère le mouvement du joueur (déplacement, saut, contrôle en l'air)
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class FPSMovement : MonoBehaviour
    {
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
        [SerializeField] private TextMeshProUGUI speedDisplay;

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

        private CharacterController controller;
        private Vector3 velocity;
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
            controller = GetComponent<CharacterController>();

            defaultMoveSpeed = moveSpeed;
        }

        private void Update()
        {
            IncreaseSpeed();
            HandleGroundCheck();
        }

        private void IncreaseSpeed()
        {
            if (IsMoving)
            {
                if (moveSpeed < speedLimit)
                {
                    moveSpeed += increaseSpeedFactor * Time.deltaTime;
                    speedDisplay.text = "Speed: " + Mathf.RoundToInt(moveSpeed).ToString();
                    if (CurrentSpeed > maxAirSpeed)
                    {
                        CurrentSpeed = maxAirSpeed;
                    }
                }
            }
            else
            {
                speedDisplay.text = "Speed: " + Mathf.RoundToInt(moveSpeed).ToString();
                moveSpeed = defaultMoveSpeed;
            }
        }

        public void Move(Vector2 input, bool sprint, bool jump)
        {
            // Calculer la direction de mouvement
            Vector3 desired = (transform.right * input.x + transform.forward * input.y);
            float desiredMag = desired.magnitude;
            if (desiredMag > 1f) desired /= desiredMag;
            
            CurrentSpeed = sprint ? sprintSpeed : moveSpeed;
            IsMoving = desired.sqrMagnitude > 0.01f;

            if (isGrounded)
            {
                // Réinitialiser le momentum au sol
                jumpMomentum = Vector3.zero;
                
                // Mouvement au sol
                if (desired.sqrMagnitude > 0f)
                {
                    controller.Move(desired * (CurrentSpeed * Time.deltaTime));
                }

                // Saut
                if (jump)
                {
                    velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity); //
                    
                    // Capturer le momentum actuel pour le préserver en l'air
                    if (preserveJumpMomentum && desired.sqrMagnitude > 0f)
                    {
                        jumpMomentum = desired * CurrentSpeed * jumpMomentumMultiplier;
                    }
                    
                    // Consommer le coyote time
                    coyoteTimeCounter = 0f;
                }
            }
            else
            {
                // Permettre le saut pendant le coyote time
                if (jump && coyoteTimeCounter > 0f)
                {
                    velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                    
                    // Capturer le momentum actuel pour le préserver en l'air
                    if (preserveJumpMomentum && desired.sqrMagnitude > 0f)
                    {
                        jumpMomentum = desired * CurrentSpeed * jumpMomentumMultiplier;
                    }
                    
                    // Consommer le coyote time
                    coyoteTimeCounter = 0f;
                }
                
                // En l'air
                if (preserveJumpMomentum && jumpMomentum.sqrMagnitude > 0f)
                {
                    // Appliquer le momentum capturé au moment du saut
                    controller.Move(jumpMomentum * Time.deltaTime);
                    
                    // Permettre un contrôle limité en l'air en PLUS du momentum
                    if (desired.sqrMagnitude > 0f)
                    {
                        controller.Move(desired * (CurrentSpeed * airControlFactor * Time.deltaTime));
                    }
                }
                else
                {
                    // Contrôle normal en l'air avec Lerp progressif (comme message(8).cs)
                    Vector3 airMove = desired * CurrentSpeed * airControlFactor;
                    moveDirection.x = Mathf.Lerp(moveDirection.x, airMove.x, airControlFactor);
                    moveDirection.z = Mathf.Lerp(moveDirection.z, airMove.z, airControlFactor);
                    
                    controller.Move(new Vector3(moveDirection.x, 0, moveDirection.z) * Time.deltaTime);
                }
            }
            
            // Appliquer la gravité
            velocity.y += gravity * gravityMultiplier * Time.deltaTime;
            controller.Move(new Vector3(0, velocity.y, 0) * Time.deltaTime);
        }

        private void HandleGroundCheck()
        {
            if (groundCheck != null && groundMask != 0)
            {
                isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
            }
            else
            {
                isGrounded = controller.isGrounded;
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

            // Réinitialiser la vélocité verticale au sol
            if (isGrounded && velocity.y < 0)
            {
                velocity.y = -2f;
            }
        }

        public CharacterController Controller => controller;
    }
}
