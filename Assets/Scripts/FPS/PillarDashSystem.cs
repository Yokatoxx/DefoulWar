using UnityEngine;
using System.Collections.Generic;

namespace Proto3GD.FPS
{
    /// <summary>
    /// Système de détection et d'interaction avec les piliers.
    /// Permet au joueur de cibler et dasher sur les piliers pour les détruire.
    /// </summary>
    public class PillarDashSystem : MonoBehaviour
    {
        [Header("Detection Settings")]
        [Tooltip("Distance maximale pour détecter un pilier")]
        [SerializeField] private float detectionRange = 5f;
        
        [Tooltip("Rayon du raycast pour la détection")]
        [SerializeField] private float detectionRadius = 0.5f;
        
        [Header("Dash Settings")]
        [Tooltip("Vitesse du dash vers le pilier")]
        [SerializeField] private float dashSpeed = 25f;
        
        [Tooltip("Durée du dash en secondes")]
        [SerializeField] private float dashDuration = 0.4f;
        
        [Tooltip("Cooldown entre chaque dash en secondes")]
        [SerializeField] private float dashCooldown = 1.5f;
        
        [Header("FOV Settings")]
        [Tooltip("FOV pendant le dash")]
        [SerializeField] private float dashFOV = 90f;
        
        [Tooltip("Vitesse de transition du FOV")]
        [SerializeField] private float fovTransitionSpeed = 15f;
        
        [Header("Visual Feedback")]
        [Tooltip("Couleur de l'outline quand un pilier est ciblé")]
        [SerializeField] private Color highlightColor = new Color(1f, 0.5f, 0f, 1f);
        
        [Tooltip("Épaisseur de l'outline")]
        [SerializeField] private float outlineWidth = 3f;
        
        [Header("References")]
        [SerializeField] private FPSPlayerController playerController;
        [SerializeField] private Transform cameraTransform;
        
        private Camera playerCamera;
        private float defaultFOV;
        private float targetFOV;
        
        private bool isDashing = false;
        private float dashTimer = 0f;
        private float cooldownTimer = 0f;
        private Vector3 dashDirection;
        private Vector3 dashTargetPosition;
        
        private GameObject currentTargetedPillar;
        private Outline currentOutline;
        
        private CharacterController characterController;
        
        // Tracking des ennemis tués par dash pour éviter le spawn de pilier
        private static System.Collections.Generic.HashSet<GameObject> enemiesKilledByDash = new System.Collections.Generic.HashSet<GameObject>();
        
        [Header("Directional Dash (nouveau)")]
        [SerializeField] private bool useDirectionalDash = true;
        [SerializeField] private float dashHitRadius = 1.0f;
        [SerializeField] private float dashDamage = 9999f; // dégâts par défaut létaux
        [SerializeField] private LayerMask enemyMask = ~0;

        // Runtime
        private Vector3 directionalDashDir;
        private Vector3 lastDashPosition;
        private readonly HashSet<GameObject> _hitThisDash = new HashSet<GameObject>();
        
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
                if (useDirectionalDash)
                {
                    // Dash directionnel: E déclenche un dash dans la direction de la caméra
                    if (!isStunned && Input.GetKeyDown(KeyCode.E) && cooldownTimer >= dashCooldown)
                    {
                        StartDirectionalDash();
                    }
                }
                else
                {
                    // Ancien mode: détection + dash vers une cible
                    DetectAndHighlightPillar();
                    if (!isStunned && Input.GetKeyDown(KeyCode.E) && cooldownTimer >= dashCooldown)
                    {
                        TryStartDash();
                    }
                }
            }
            
            // Gestion du FOV
            UpdateFOV();
        }

        private void FixedUpdate()
        {
            if (!isDashing || characterController == null) return;

            if (useDirectionalDash)
            {
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
            else
            {
                // Ancien comportement: suivre la cible actuelle
                if (currentTargetedPillar != null)
                {
                    Vector3 pillarPosition = currentTargetedPillar.transform.position;
                    dashDirection = (pillarPosition - transform.position).normalized;
                }
                Vector3 dashMovement = dashDirection * (dashSpeed * Time.fixedDeltaTime);
                characterController.Move(dashMovement);
                Debug.DrawRay(transform.position, dashDirection * 3f, Color.red, 0.1f);
            }
        }

        private static readonly RaycastHit[] _hitBuffer = new RaycastHit[32];

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
        /// Détecte un pilier devant le joueur et l'highlight s'il est à portée.
        /// </summary>
        private void DetectAndHighlightPillar()
        {
            if (cameraTransform == null) return;
            
            RaycastHit hit;
            bool foundTarget = false;
            GameObject targetedObject = null;
            
            // Essayer d'abord avec un Raycast simple (plus fiable)
            if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out hit, detectionRange))
            {
                // Vérifier si c'est un pilier
                PillarController pillarController = hit.collider.GetComponentInParent<PillarController>();
                if (pillarController == null)
                {
                    pillarController = hit.collider.GetComponent<PillarController>();
                }
                
                if (pillarController != null)
                {
                    targetedObject = pillarController.gameObject;
                    foundTarget = true;
                    Debug.DrawRay(cameraTransform.position, cameraTransform.forward * hit.distance, Color.green, 0.1f);
                }
                else
                {
                    // Vérifier si c'est un ennemi
                    EnemyHealth enemyHealth = hit.collider.GetComponentInParent<EnemyHealth>();
                    if (enemyHealth == null)
                    {
                        enemyHealth = hit.collider.GetComponent<EnemyHealth>();
                    }
                    
                    if (enemyHealth != null)
                    {
                        targetedObject = enemyHealth.gameObject;
                        foundTarget = true;
                        Debug.DrawRay(cameraTransform.position, cameraTransform.forward * hit.distance, Color.cyan, 0.1f);
                    }
                }
            }
            
            // Si pas trouvé, essayer avec SphereCast pour une détection plus large
            if (!foundTarget && Physics.SphereCast(
                cameraTransform.position,
                detectionRadius,
                cameraTransform.forward,
                out hit,
                detectionRange
            ))
            {
                // Vérifier pilier
                PillarController pillarController = hit.collider.GetComponentInParent<PillarController>();
                if (pillarController == null)
                {
                    pillarController = hit.collider.GetComponent<PillarController>();
                }
                
                if (pillarController != null)
                {
                    targetedObject = pillarController.gameObject;
                    foundTarget = true;
                }
                else
                {
                    // Vérifier ennemi
                    EnemyHealth enemyHealth = hit.collider.GetComponentInParent<EnemyHealth>();
                    if (enemyHealth == null)
                    {
                        enemyHealth = hit.collider.GetComponent<EnemyHealth>();
                    }
                    
                    if (enemyHealth != null)
                    {
                        targetedObject = enemyHealth.gameObject;
                        foundTarget = true;
                    }
                }
            }
            
            // Si on cible un nouveau objet différent
            if (targetedObject != currentTargetedPillar)
            {
                // Retirer le highlight du pilier précédent
                RemoveHighlight();
                
                // Appliquer le highlight au nouvel objet
                if (targetedObject != null)
                {
                    ApplyHighlight(targetedObject);
                    
                    // Afficher le type d'objet ciblé
                    if (targetedObject.GetComponent<PillarController>() != null)
                    {
                        Debug.Log($"Pilier ciblé : {targetedObject.name}");
                    }
                    else if (targetedObject.GetComponent<EnemyHealth>() != null)
                    {
                        Debug.Log($"Ennemi ciblé : {targetedObject.name}");
                    }
                }
                
                currentTargetedPillar = targetedObject;
            }
        }
        
        /// <summary>
        /// Applique un effet de highlight visuel au pilier ciblé.
        /// </summary>
        private void ApplyHighlight(GameObject pillar)
        {
            // Essayer de récupérer le composant Outline existant
            currentOutline = pillar.GetComponent<Outline>();
            if (currentOutline == null)
            {
                Debug.LogWarning($"Aucun composant Outline trouvé sur {pillar.name} - Le highlight ne peut pas fonctionner !");
                return;
            }
            
            // Configurer la couleur et l'épaisseur de l'outline
            currentOutline.OutlineColor = highlightColor;
            currentOutline.OutlineWidth = outlineWidth;
            currentOutline.enabled = true;
            
            Debug.Log($"✓ Outline appliqué sur {pillar.name} avec couleur {highlightColor} et épaisseur {outlineWidth}");
        }
        
        /// <summary>
        /// Retire l'effet de highlight du pilier précédemment ciblé.
        /// </summary>
        private void RemoveHighlight()
        {
            if (currentTargetedPillar != null)
            {
                Outline outline = currentTargetedPillar.GetComponent<Outline>();
                if (outline != null)
                {
                    outline.enabled = false;
                }
            }
        }
        
        private void OnDestroy()
        {
            // Nettoyer l'outline quand le composant est détruit
            RemoveHighlight();
        }
        
        /// <summary>
        /// Tente de commencer un dash vers le pilier ciblé.
        /// </summary>
        private void TryStartDash()
        {
            if (currentTargetedPillar == null) return;
            
            // Calculer la direction du dash
            Vector3 pillarPosition = currentTargetedPillar.transform.position;
            dashDirection = (pillarPosition - transform.position).normalized;
            
            // Commencer le dash
            isDashing = true;
            dashTimer = 0f;
            targetFOV = dashFOV;
            
            Debug.Log($"Dash commencé vers {currentTargetedPillar.name}!");
        }
        
        /// <summary>
        /// Gère le mouvement pendant le dash.
        /// </summary>
        private void HandleDash()
        {
            dashTimer += Time.deltaTime;
            
            if (dashTimer < dashDuration)
            {
                if (!useDirectionalDash)
                {
                    // Ancien: vérifier collision avec la cible
                    CheckPillarCollision();
                }
            }
            else
            {
                EndDash();
            }
        }
        
        /// <summary>
        /// Vérifie si on a touché le pilier ciblé pendant le dash.
        /// </summary>
        private void CheckPillarCollision()
        {
            if (currentTargetedPillar == null) return;
            
            float distanceToTarget = Vector3.Distance(transform.position, currentTargetedPillar.transform.position);
            
            if (distanceToTarget < 1.5f)
            {
                // Vérifier si c'est un pilier ou un ennemi
                PillarController pillarController = currentTargetedPillar.GetComponent<PillarController>();
                EnemyHealth enemyHealth = currentTargetedPillar.GetComponent<EnemyHealth>();
                
                if (pillarController != null)
                {
                    // Désactiver les collisions du pilier
                    DisableCollisions(currentTargetedPillar);
                    
                    // Détruire le pilier
                    pillarController.DestroyPillar(0f);
                    Debug.Log($"Pilier {currentTargetedPillar.name} détruit par dash!");
                }
                else if (enemyHealth != null)
                {
                    // Désactiver les collisions de l'ennemi
                    DisableCollisions(currentTargetedPillar);
                    
                    // Marquer l'ennemi comme tué par dash AVANT de le tuer
                    GameObject enemyRoot = enemyHealth.transform.root.gameObject;
                    enemiesKilledByDash.Add(enemyRoot);
                    
                    // Tuer l'ennemi avec un dégât massif
                    enemyHealth.TakeDamage(9999f, "Dash");
                    Debug.Log($"Ennemi {currentTargetedPillar.name} tué par dash!");
                    
                    // Nettoyer le HashSet après un délai pour éviter les fuites mémoire
                    StartCoroutine(CleanupEnemyTracking(enemyRoot));
                }
                
                // Retirer le highlight
                RemoveHighlight();
                currentTargetedPillar = null;
                
                EndDash();
            }
        }
        
        /// <summary>
        /// Désactive toutes les collisions d'un GameObject
        /// </summary>
        private void DisableCollisions(GameObject target)
        {
            // Désactiver tous les colliders
            Collider[] colliders = target.GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                col.enabled = false;
            }
            
            // Désactiver également le rigidbody si présent
            Rigidbody rb = target.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.detectCollisions = false;
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
        
        private void OnDisable()
        {
            // Nettoyer le highlight quand le composant est désactivé
            RemoveHighlight();
        }
        
        /// <summary>
        /// Vérifie si le joueur peut dasher actuellement.
        /// </summary>
        public bool CanDash => cooldownTimer >= dashCooldown && !isDashing;
        
        /// <summary>
        /// Retourne le pourcentage de cooldown du dash (0 = en cooldown, 1 = disponible)
        /// </summary>
        public float DashCooldownProgress => Mathf.Clamp01(cooldownTimer / dashCooldown);
        
        /// <summary>
        /// Récupère le pilier actuellement ciblé.
        /// </summary>
        public GameObject CurrentTargetedPillar => currentTargetedPillar;
    }
}
