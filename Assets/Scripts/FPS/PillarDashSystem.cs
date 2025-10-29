using UnityEngine;
using System.Collections.Generic;

namespace Proto3GD.FPS
{

    // Système de dash directionnel
    // Permet au joueur de dasher dans la direction de la caméra et de tuer les ennemis sur son passage
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
        
        [Header("Collision Settings")]
        [Tooltip("Angle maximum (en degrés) entre la direction du dash et la surface pour continuer le dash. Au-delà, le dash s'arrête.")]
        [SerializeField] private float maxCollisionAngle = 45f;
        
        [Tooltip("LayerMask pour les obstacles qui peuvent arrêter le dash")]
        [SerializeField] private LayerMask obstacleMask = ~0;
        
        [Tooltip("Distance de détection des obstacles devant le joueur")]
        [SerializeField] private float obstacleCheckDistance = 0.5f;
        
        [Header("FOV Settings")]
        [Tooltip("FOV pendant le dash")]
        [SerializeField] private float dashFOV = 90f;
        
        [Tooltip("Vitesse de transition du FOV")]
        [SerializeField] private float fovTransitionSpeed = 15f;
        
        [Header("Momentum Settings")]
        [Tooltip("Conserver l'énergie cinétique à la sortie du dash")]
        [SerializeField] private bool conserveMomentum = true;
        
        [Tooltip("Pourcentage du momentum du dash à conserver (0 à 1)")]
        [SerializeField] private float momentumRetention = 0.8f;
        
        [Header("References")]
        [SerializeField] private FPSPlayerController playerController;
        [SerializeField] private FPSMovement fpsMovement;
        [SerializeField] private Transform cameraTransform;
        
        private Camera playerCamera;
        private float defaultFOV;
        private float targetFOV;
        
        public bool isDashing = false; 
        private float dashTimer = 0f;
        private float cooldownTimer = 0f;
        
        private CharacterController characterController;
        
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

            if (fpsMovement == null)
            {
                fpsMovement = GetComponent<FPSMovement>();
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

            // Vérifier les obstacles devant le joueur
            if (CheckObstacleCollision())
            {
                EndDash();
                return;
            }
            
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

                    // Appliquer dégâts - EnemyHealth gère maintenant toute la logique
                    enemyHealth.TakeDamage(dashDamage, "Dash");
                    
                    // Si l'ennemi est mort et qu'il était électrique, arrêter le dash
                    if (enemyHealth.IsDead && enemyHealth.KilledByDash)
                    {
                        var electric = h.collider.GetComponentInParent<Ennemies.Effect.ElectricEnnemis>() ?? h.collider.GetComponent<Ennemies.Effect.ElectricEnnemis>();
                        if (electric != null)
                        {
                            // Arrêter le dash immédiatement à cause du stun électrique
                            EndDash();
                            return;
                        }
                    }
                }
            }

            lastDashPosition = nextPos;
            characterController.Move(dashMovement);
            Debug.DrawRay(transform.position, directionalDashDir * 3f, Color.cyan, 0.05f);
        }

        private void StartDirectionalDash()
        {
            Vector3 fwd;
            if (playerCamera != null)
            {
                Ray aimRay = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                fwd = aimRay.direction;
            }
            else if (cameraTransform != null)
            {
                fwd = cameraTransform.forward;
            }
            else
            {
                fwd = transform.forward;
            }
            
            directionalDashDir = fwd.normalized;
            
            currentDashCharge = 0f;

            // Mettre la vitesse de mouvement au maximum
            if (fpsMovement != null)
            {
                fpsMovement.SetSpeedToMax();
            }

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
        
        // Vérifie si le dash doit être arrêté par une collision avec un obstacle à mauvais angle
        private bool CheckObstacleCollision()
        {
            // Raycast dans la direction du dash pour détecter les obstacles
            RaycastHit hit;
            float checkDistance = obstacleCheckDistance;
            
            // SphereCast pour détecter les obstacles devant le joueur avec un rayon similaire au dashHitRadius
            if (Physics.SphereCast(
                transform.position,
                dashHitRadius * 0.8f, // Légèrement plus petit pour éviter les faux positifs
                directionalDashDir,
                out hit,
                checkDistance,
                obstacleMask,
                QueryTriggerInteraction.Ignore))
            {
                // Ignorer si c'est un ennemi (ils sont gérés séparément)
                var enemyHealth = hit.collider.GetComponentInParent<EnemyHealth>() ?? hit.collider.GetComponent<EnemyHealth>();
                if (enemyHealth != null)
                {
                    return false; // Ne pas arrêter le dash pour les ennemis
                }
                
                // Calculer l'angle entre la direction du dash et la normale de la surface
                float angle = Vector3.Angle(directionalDashDir, -hit.normal);
                
                // Debug visuel
                Debug.DrawRay(hit.point, hit.normal * 2f, Color.red, 0.1f);
                Debug.DrawRay(hit.point, directionalDashDir * 2f, Color.yellow, 0.1f);
                
                // Si l'angle est trop abrupt (surface trop perpendiculaire à la direction du dash)
                if (angle > maxCollisionAngle)
                {
                    Debug.Log($"[PillarDashSystem] Dash arrêté par collision ! Angle: {angle:F1}° (max: {maxCollisionAngle}°)");
                    return true; // Arrêter le dash
                }
            }
            
            return false; // Continuer le dash
        }

        

        private void EndDash()
        {
            // Appliquer le momentum de sortie si activé
            if (conserveMomentum && fpsMovement != null)
            {
                // Calculer le momentum basé sur la vitesse du dash
                Vector3 dashMomentum = directionalDashDir * dashSpeed * momentumRetention;
                fpsMovement.ApplyExternalMomentum(dashMomentum);
                
                Debug.Log($"[PillarDashSystem] Momentum conservé: {dashMomentum.magnitude:F1} m/s dans la direction {directionalDashDir}");
            }
            
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
