using UnityEngine;
using System.Collections.Generic;

namespace Proto3GD.FPS
{

    // Système de dash directionnel.
    // Permet au joueur de dasher dans la direction de la caméra et de tuer les ennemis sur son passage.
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
            }

            characterController = playerController.Controller;
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
                        
                        // Désactiver immédiatement les collisions pour permettre le dash à travers
                        DisableEnemyCollisions(enemyRoot);
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
        
        private void HandleDash()
        {
            dashTimer += Time.deltaTime;
            
            if (dashTimer >= dashDuration)
            {
                EndDash();
            }
        }
        

        // Désactive toutes les collisions d'un ennemi pour permettre au dash de passer à travers.
        private void DisableEnemyCollisions(GameObject enemy)
        {

            Collider[] colliders = enemy.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                col.enabled = false;
            }
            
            // Désactiver également le rigidbody pour éviter les interactions physiques
            Rigidbody rb = enemy.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.detectCollisions = false;
            }
            
            Rigidbody[] childRbs = enemy.GetComponentsInChildren<Rigidbody>();
            foreach (Rigidbody childRb in childRbs)
            {
                childRb.isKinematic = true;
                childRb.detectCollisions = false;
            }
        }
        
        private System.Collections.IEnumerator CleanupEnemyTracking(GameObject enemy)
        {
            yield return new WaitForSeconds(2f);
            if (enemy == null)
            {
                // L'ennemi a bien été détruit, on peut le retirer du tracking
                enemiesKilledByDash.Remove(enemy);
            }
        }
        
        // Vérifie si un ennemi a été tué par le dash
        public static bool WasKilledByDash(GameObject enemy)
        {
            return enemiesKilledByDash.Contains(enemy);
        }
        
        private void EndDash()
        {
            isDashing = false;
            dashTimer = 0f;
            cooldownTimer = 0f;
            targetFOV = defaultFOV;
        }
        
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
        
        public bool CanDash => cooldownTimer >= dashCooldown && !isDashing;
        
        public float DashCooldownProgress => Mathf.Clamp01(cooldownTimer / dashCooldown);
    }
}
