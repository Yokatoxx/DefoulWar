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
        
        [Tooltip("Courbe de vitesse du dash (X = temps normalisé 0-1, Y = multiplicateur de vitesse)")]
        [SerializeField] private AnimationCurve dashSpeedCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.3f);
        
        [Header("Dash Charge Settings")]
        [Tooltip("Activer la régénération automatique du dash (désactiver pour forcer le kill d'ennemis)")]
        [SerializeField] private bool autoRegenerate = false;
        
        [Tooltip("Cooldown entre chaque dash en secondes (uniquement si autoRegenerate est activé)")]
        [SerializeField] private float dashCooldown = 1.5f;
        
        [Tooltip("Nombre d'ennemis spéciaux à tuer pour remplir complètement la barre de dash")]
        [SerializeField] private int enemiesRequiredForFullCharge = 3;
        
        [Tooltip("Charge de dash actuelle (0 à 1)")]
        [SerializeField] private float currentDashCharge = 1f;
        
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
        
        public bool isDashing = false;
        private float dashTimer = 0f;
        private float cooldownTimer = 0f;
        
        private CharacterController characterController;
        
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
            // Mise à jour du cooldown (uniquement si autoRegenerate est activé)
            if (autoRegenerate && cooldownTimer < dashCooldown)
            {
                cooldownTimer += Time.deltaTime;
                
                // Régénération automatique de la charge
                if (cooldownTimer >= dashCooldown && currentDashCharge < 1f)
                {
                    currentDashCharge = 1f;
                }
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
                bool hasCharge = currentDashCharge >= 1f;
                bool cooldownReady = autoRegenerate ? (cooldownTimer >= dashCooldown) : true;
                
                if (!isStunned && Input.GetMouseButtonDown(1) && hasCharge && cooldownReady)
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

            // Calculer le progrès du dash (0 à 1)
            float dashProgress = Mathf.Clamp01(dashTimer / dashDuration);
            
            // Évaluer la courbe pour obtenir le multiplicateur de vitesse
            float speedMultiplier = dashSpeedCurve.Evaluate(dashProgress);
            
            // Mouvement en ligne droite selon la direction de dash avec la courbe appliquée
            Vector3 dashMovement = directionalDashDir * (dashSpeed * speedMultiplier * Time.fixedDeltaTime);
            
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

            // Consommer la charge
            currentDashCharge = 0f;

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
        
        public bool CanDash => currentDashCharge >= 1f && !isDashing && (autoRegenerate ? cooldownTimer >= dashCooldown : true);
        
        public float DashCooldownProgress => Mathf.Clamp01(cooldownTimer / dashCooldown);
        
        public float CurrentDashCharge => currentDashCharge;
        
        // Appelé quand un ennemi spécial (DashEnergyEnemy) est tué
        public void OnDashEnemyKilled(float energyAmount)
        {
            float oldCharge = currentDashCharge;
            
            // Calculer l'énergie par ennemi (1 / nombre d'ennemis requis)
            float energyPerEnemy = 1f / Mathf.Max(1, enemiesRequiredForFullCharge);
            
            // Ajouter l'énergie (multipliée par le montant configuré sur l'ennemi)
            currentDashCharge = Mathf.Clamp01(currentDashCharge + (energyPerEnemy * energyAmount));
            
            Debug.Log($"[PillarDashSystem] Dash rechargé! {oldCharge:P0} → {currentDashCharge:P0} (+{energyPerEnemy * energyAmount:P0})");
        }
    }
}
