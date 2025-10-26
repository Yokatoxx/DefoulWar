using UnityEngine;
using System.Collections.Generic;

namespace Proto3GD.FPS
{
    /// <summary>
    /// Système de dash directionnel.
    /// Permet au joueur de dasher dans la direction de la caméra et de tuer les ennemis sur son passage.
    /// </summary>
    public class PillarDashSystem : MonoBehaviour
    {
        [Header("Dash Settings")]
        [Tooltip("Vitesse du dash")]
        [SerializeField] private float dashSpeed = 25f;
        
        [Tooltip("Durée du dash en secondes")]
        [SerializeField] private float dashDuration = 0.4f;
        
        [Tooltip("Cooldown entre chaque dash en secondes")]
        [SerializeField] private float dashCooldown = 1.5f;
        
        [Tooltip("Rayon de détection des ennemis pendant le dash")]
        [SerializeField] private float dashHitRadius = 1.0f;
        
        [Tooltip("Dégâts infligés aux ennemis touchés")]
        [SerializeField] private float dashDamage = 9999f;
        
        [Tooltip("LayerMask pour détecter les ennemis")]
        [SerializeField] private LayerMask enemyMask = ~0;
        
        [Header("FOV Settings")]
        [Tooltip("FOV pendant le dash")]
        [SerializeField] private float dashFOV = 90f;
        
        [Tooltip("Vitesse de transition du FOV")]
        [SerializeField] private float fovTransitionSpeed = 15f;
        
        [Header("References")]
        [SerializeField] private FPSPlayerController playerController;
        [SerializeField] private Transform cameraTransform;
        
        private Camera playerCamera;
        private float defaultFOV;
        private float targetFOV;
        
        private bool isDashing = false;
        private float dashTimer = 0f;
        private float cooldownTimer = 0f;
        
        private CharacterController characterController;
        
        // Tracking des ennemis tués par dash pour éviter le spawn de pilier
        private static System.Collections.Generic.HashSet<GameObject> enemiesKilledByDash = new System.Collections.Generic.HashSet<GameObject>();
        
        // Runtime
        private Vector3 directionalDashDir;
        private Vector3 lastDashPosition;
        private readonly HashSet<GameObject> _hitThisDash = new HashSet<GameObject>();
        private static readonly RaycastHit[] _hitBuffer = new RaycastHit[32];
        
        private void Start()
        {
            if (playerController == null)
            {
                playerController = GetComponent<FPSPlayerController>();
                if (playerController == null)
                {
                    return;
                }
            }

            if (cameraTransform == null)
            {
                cameraTransform = playerController.CameraTransform;
                if (cameraTransform == null)
                {
                    return;
                }
            }

            if (cameraTransform != null)
            {
                playerCamera = cameraTransform.GetComponent<Camera>();
                if (playerCamera != null)
                {
                    defaultFOV = playerCamera.fieldOfView;
                    targetFOV = defaultFOV;
                }
                else
                {
                    Debug.LogError("[PillarDashSystem] Aucun composant Camera trouvé sur CameraTransform. Veuillez vérifier la hiérarchie.");
                    return;
                }
            }

            characterController = playerController.Controller;
            if (characterController == null)
            {
                Debug.LogError("[PillarDashSystem] Le champ Controller de FPSPlayerController n'est pas assigné. Veuillez l'assigner dans l'Inspector.");
                return;
            }
            
            Debug.Log("[PillarDashSystem] Initialisation réussie !");
        }
        
        private void Update()
        {
            // Mise à jour du cooldown
            if (cooldownTimer < dashCooldown)
            {
                cooldownTimer += Time.deltaTime;
            }
            
            // Empêcher le dash pendant un stun
            var stunComp = GetComponent<PlayerStunAutoFire>();
            bool isStunned = stunComp != null && stunComp.IsStunned;
            
            // Gestion du dash
            if (isDashing)
            {
                HandleDash();
            }
            else
            {
                // Dash directionnel: E déclenche un dash dans la direction de la caméra
                if (!isStunned && Input.GetKeyDown(KeyCode.E) && cooldownTimer >= dashCooldown)
                {
                    StartDirectionalDash();
                }
            }
            
            // Gestion du FOV
            UpdateFOV();
        }

        private void FixedUpdate()
        {
            if (!isDashing || characterController == null) return;

            // Mouvement en ligne droite selon la direction de dash
            Vector3 dashMovement = directionalDashDir * (dashSpeed * Time.fixedDeltaTime);

            // Balayage des ennemis entre lastDashPosition et la nouvelle position
            Vector3 currentPos = transform.position;
            Vector3 nextPos = currentPos + dashMovement;
            Vector3 seg = nextPos - lastDashPosition;
            float segLen = seg.magnitude;
            if (segLen > 0.0001f)
            {
                int hits = Physics.SphereCastNonAlloc(
                    lastDashPosition,
                    dashHitRadius,
                    seg.normalized,
                    _hitBuffer,
                    segLen,
                    enemyMask,
                    QueryTriggerInteraction.Ignore
                );
                for (int i = 0; i < hits; i++)
                {
                    var h = _hitBuffer[i];
                    if (h.collider == null) continue;
                    var enemyHealth = h.collider.GetComponentInParent<EnemyHealth>() ?? h.collider.GetComponent<EnemyHealth>();
                    if (enemyHealth == null) continue;

                    var enemyRoot = enemyHealth.transform.root.gameObject;
                    if (_hitThisDash.Contains(enemyRoot)) continue;
                    _hitThisDash.Add(enemyRoot);

                    // Si c'est un ennemi électrique -> stun le joueur (auto-fire)
                    var electric = h.collider.GetComponentInParent<Ennemies.Effect.ElectricEnnemis>() ?? h.collider.GetComponent<Ennemies.Effect.ElectricEnnemis>();
                    if (electric != null)
                    {
                        var playerStun = GetComponent<PlayerStunAutoFire>();
                        if (playerStun == null) playerStun = gameObject.AddComponent<PlayerStunAutoFire>();
                        if (electric.OverrideAutoFireInterval)
                            playerStun.ApplyStun(electric.StunDuration, electric.StunAutoFireInterval);
                        else
                            playerStun.ApplyStun(electric.StunDuration);
                    }

                    // Marquer comme kill par dash si le coup sera létal
                    bool willDie = enemyHealth.CurrentHealth <= dashDamage;
                    if (willDie)
                    {
                        enemiesKilledByDash.Add(enemyRoot);
                        StartCoroutine(CleanupEnemyTracking(enemyRoot));
                    }

                    // Appliquer dégâts
                    enemyHealth.TakeDamage(dashDamage, "Dash");
                }
            }

            lastDashPosition = nextPos;
            characterController.Move(dashMovement);
            Debug.DrawRay(transform.position, directionalDashDir * 3f, Color.cyan, 0.05f);
        }

        private void StartDirectionalDash()
        {
            // Direction = caméra si dispo sinon forward du joueur
            Vector3 fwd = cameraTransform != null ? cameraTransform.forward : transform.forward;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.0001f) fwd = transform.forward;
            directionalDashDir = fwd.normalized;

            // Init timers/état
            isDashing = true;
            dashTimer = 0f;
            targetFOV = dashFOV;
            lastDashPosition = transform.position;
            _hitThisDash.Clear();
        }
        
        /// <summary>
        /// Gère le mouvement pendant le dash.
        /// </summary>
        private void HandleDash()
        {
            dashTimer += Time.deltaTime;
            
            if (dashTimer >= dashDuration)
            {
                EndDash();
            }
        }
        
        /// <summary>
        /// Nettoie le tracking des ennemis tués par dash après un délai.
        /// </summary>
        private System.Collections.IEnumerator CleanupEnemyTracking(GameObject enemy)
        {
            yield return new WaitForSeconds(2f);
            if (enemy == null)
            {
                // L'ennemi a bien été détruit, on peut le retirer du tracking
                enemiesKilledByDash.Remove(enemy);
            }
        }
        
        /// <summary>
        /// Vérifie si un ennemi a été tué par le dash (pour empêcher le spawn de pilier).
        /// </summary>
        public static bool WasKilledByDash(GameObject enemy)
        {
            return enemiesKilledByDash.Contains(enemy);
        }
        
        /// <summary>
        /// Termine le dash et réinitialise les paramètres.
        /// </summary>
        private void EndDash()
        {
            isDashing = false;
            dashTimer = 0f;
            cooldownTimer = 0f;
            targetFOV = defaultFOV;
        }
        
        /// <summary>
        /// Met à jour le FOV de la caméra avec interpolation.
        /// </summary>
        private void UpdateFOV()
        {
            if (playerCamera != null)
            {
                playerCamera.fieldOfView = Mathf.Lerp(
                    playerCamera.fieldOfView,
                    targetFOV,
                    Time.deltaTime * fovTransitionSpeed
                );
            }
        }
        
        /// <summary>
        /// Vérifie si le joueur peut dasher actuellement.
        /// </summary>
        public bool CanDash => cooldownTimer >= dashCooldown && !isDashing;
        
        /// <summary>
        /// Retourne le pourcentage de cooldown du dash (0 = en cooldown, 1 = disponible)
        /// </summary>
        public float DashCooldownProgress => Mathf.Clamp01(cooldownTimer / dashCooldown);
    }
}
