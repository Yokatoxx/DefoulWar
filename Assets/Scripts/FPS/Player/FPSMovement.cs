using UnityEngine;
using UnityEngine.Events;

namespace FPS
{
    /// Gère le mouvement du joueur avec Rigidbody
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
        [SerializeField] private float maxFallSpeed = -20f;
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

        [Header("External Momentum")]
        [SerializeField, Tooltip("Facteur de dissipation appliqué au momentum externe (0 = sans dissipation).")]
        private float externalMomentumDamping = 12f;
        [SerializeField, Tooltip("Durée pendant laquelle la composante verticale du momentum est conservée même si le joueur est au sol.")]
        private float externalMomentumGroundGrace = 0.08f;

        private Rigidbody rb;
        private CapsuleCollider capsuleCollider;
        private Vector3 velocity;
        private Vector3 moveDirection = Vector3.zero;
        private Vector3 jumpMomentum = Vector3.zero;
        private Vector3 externalMomentum = Vector3.zero;
        private float externalMomentumGroundTimer;
        private bool isGrounded;
        private float coyoteTimeCounter;

        // Variables pour stocker les inputs entre Update et FixedUpdate
        private Vector2 inputMove;
        private bool inputSprint;
        private bool jumpRequested;
        
        // Flag pour désactiver le mouvement normal (pendant le dash)
        private bool movementDisabled;

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
            externalMomentum = momentum;
            externalMomentumGroundTimer = externalMomentumGroundGrace;
            moveSpeed = Mathf.Max(moveSpeed, momentum.magnitude);
        }
        
        // Désactiver le mouvement normal (pendant le dash)
        public void DisableMovement()
        {
            movementDisabled = true;
            velocity.y = 0f; // Reset la gravité
            Debug.Log("[FPSMovement] Mouvement désactivé");
        }
        
        // Réactiver le mouvement normal (après le dash)
        public void EnableMovement()
        {
            if (movementDisabled)
            {
                Debug.Log("[FPSMovement] Mouvement réactivé");
            }
            movementDisabled = false;
        }
        
        // Propriété pour vérifier si le mouvement est désactivé
        public bool IsMovementDisabled => movementDisabled;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            capsuleCollider = GetComponent<CapsuleCollider>();

            // Configuration du Rigidbody pour un contrôle FPS
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.constraints = RigidbodyConstraints.FreezeRotation; // Freeze toutes les rotations
            rb.useGravity = false; // On gère la gravité manuellement
            rb.linearDamping = 0f; // Pas de résistance au mouvement
            rb.angularDamping = 0f;
            
            // Créer un PhysicMaterial sans friction pour éviter le ralentissement sur les surfaces
            PhysicsMaterial frictionlessMat = new PhysicsMaterial("PlayerNoFriction")
            {
                dynamicFriction = 0f,
                staticFriction = 0f,
                frictionCombine = PhysicsMaterialCombine.Minimum,
                bounciness = 0f,
                bounceCombine = PhysicsMaterialCombine.Minimum
            };
            capsuleCollider.material = frictionlessMat;

            defaultMoveSpeed = moveSpeed;
        }

        private void Update()
        {
            IncreaseSpeed();
            HandleGroundCheck();
        }

        private void FixedUpdate()
        {
            ApplyMovement();
        }

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

        // Cette méthode sera appelée par le PlayerController (dans Update)
        public void Move(Vector2 input, bool sprint, bool jump)
        {
            inputMove = input;
            inputSprint = sprint;
            if (jump) jumpRequested = true;
        }

        private void ApplyMovement()
        {
            // Ne pas appliquer le mouvement normal si désactivé (pendant le dash)
            if (movementDisabled) return;
            
            // Calculer la direction de mouvement
            Vector3 desired = (transform.right * inputMove.x + transform.forward * inputMove.y);
            float desiredMag = desired.magnitude;
            if (desiredMag > 1f) desired /= desiredMag;
            
            CurrentSpeed = inputSprint ? sprintSpeed : moveSpeed;
            IsMoving = desired.sqrMagnitude > 0.01f;

            Vector3 horizontalVelocity = Vector3.zero;

            if (isGrounded)
            {
                // Réinitialiser le momentum au sol
                jumpMomentum = Vector3.zero;
                
                // Mouvement au sol
                if (desired.sqrMagnitude > 0f)
                {
                    horizontalVelocity = desired * CurrentSpeed;
                }

                // Saut
                if (jumpRequested)
                {
                    velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                    
                    // Capturer le momentum actuel pour le préserver en l'air
                    if (preserveJumpMomentum && desired.sqrMagnitude > 0f)
                    {
                        jumpMomentum = desired * CurrentSpeed * jumpMomentumMultiplier;
                    }
                    
                    // Consommer le coyote time
                    coyoteTimeCounter = 0f;
                    jumpRequested = false;
                }
            }
            else
            {
                // Permettre le saut pendant le coyote time
                if (jumpRequested && coyoteTimeCounter > 0f)
                {
                    velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                    
                    // Capturer le momentum actuel pour le préserver en l'air
                    if (preserveJumpMomentum && desired.sqrMagnitude > 0f)
                    {
                        jumpMomentum = desired * CurrentSpeed * jumpMomentumMultiplier;
                    }
                    
                    // Consommer le coyote time
                    coyoteTimeCounter = 0f;
                    jumpRequested = false;
                }
                
                // En l'air
                if (preserveJumpMomentum && jumpMomentum.sqrMagnitude > 0f)
                {
                    // Appliquer le momentum capturé au moment du saut
                    horizontalVelocity = jumpMomentum;
                    
                    // Permettre un contrôle limité en l'air en PLUS du momentum
                    if (desired.sqrMagnitude > 0f)
                    {
                        horizontalVelocity += desired * CurrentSpeed * airControlFactor;
                    }
                }
                else
                {
                    // Mouvement aérien sans momentum préservé
                    horizontalVelocity = desired * CurrentSpeed * airControlFactor;
                }
            }

            // Reset jump request si pas utilisé
            jumpRequested = false;

            // Traiter le momentum externe
            ProcessExternalMomentum();
            horizontalVelocity += new Vector3(externalMomentum.x, 0, externalMomentum.z);

            // Appliquer la gravité seulement en l'air
            if (!isGrounded)
            {
                velocity.y += gravity * gravityMultiplier * Time.fixedDeltaTime;
                
                // Limiter la vitesse de chute
                if (velocity.y < maxFallSpeed)
                {
                    velocity.y = maxFallSpeed;
                }
            }
            
            // Ajouter le momentum vertical externe
            float verticalVelocity = velocity.y + externalMomentum.y;

            // Appliquer la vélocité au Rigidbody (les collisions seront gérées automatiquement)
            rb.linearVelocity = new Vector3(horizontalVelocity.x, verticalVelocity, horizontalVelocity.z);
        }

        private void HandleGroundCheck()
        {
            bool wasGrounded = isGrounded;
            
            // Créer un masque qui exclut le layer du joueur
            int playerLayer = gameObject.layer;
            int excludePlayerMask = ~(1 << playerLayer);
            
            if (groundCheck != null)
            {
                // Utiliser groundMask si défini, sinon détecter tout sauf le joueur
                if (groundMask != 0)
                {
                    isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, groundMask);
                }
                else
                {
                    // Détecter tous les colliders sauf le joueur et les triggers
                    isGrounded = Physics.CheckSphere(groundCheck.position, groundDistance, excludePlayerMask, QueryTriggerInteraction.Ignore);
                }
            }
            else
            {
                // Fallback: raycast vers le bas depuis le bas du collider
                float rayLength = groundDistance + 0.1f;
                Vector3 rayOrigin = transform.position + Vector3.up * 0.1f;
                
                if (groundMask != 0)
                {
                    isGrounded = Physics.Raycast(rayOrigin, Vector3.down, rayLength, groundMask);
                }
                else
                {
                    // Détecter tous les colliders sauf le joueur et les triggers
                    isGrounded = Physics.Raycast(rayOrigin, Vector3.down, rayLength, excludePlayerMask, QueryTriggerInteraction.Ignore);
                }
            }

            // Gérer le coyote time
            if (isGrounded)
            {
                coyoteTimeCounter = coyoteTime;
                
                // Réinitialiser la vélocité verticale au sol
                if (velocity.y < 0)
                {
                    velocity.y = 0f;
                }
            }
            else
            {
                coyoteTimeCounter -= Time.deltaTime;
                
                // Si on vient de quitter le sol naturellement (sans sauter), 
                // commencer avec une vélocité verticale nulle pour une chute progressive
                if (wasGrounded && velocity.y <= 0)
                {
                    velocity.y = 0f;
                }
            }
        }

        private void ProcessExternalMomentum()
        {
            if (externalMomentum.sqrMagnitude <= 1e-4f)
                return;

            if (externalMomentumGroundTimer > 0f)
                externalMomentumGroundTimer -= Time.fixedDeltaTime;

            if (isGrounded && externalMomentumGroundTimer <= 0f && externalMomentum.y > 0f)
                externalMomentum.y = 0f;

            float damping = externalMomentumDamping;
            if (isGrounded && externalMomentumGroundTimer <= 0f)
                damping *= 1.5f;

            if (damping > 0f)
            {
                externalMomentum = Vector3.MoveTowards(externalMomentum, Vector3.zero, damping * Time.fixedDeltaTime);
            }

            if (isGrounded && externalMomentumGroundTimer <= 0f && Mathf.Abs(externalMomentum.y) < 0.01f)
                externalMomentum.y = 0f;
        }

        /// <summary>
        /// Accès au Rigidbody pour les systèmes externes (Dash, etc.)
        /// </summary>
        public Rigidbody Rb => rb;
    }
}
