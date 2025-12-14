using System.Collections;
using UnityEngine;
using Utils;

namespace FPS
{
    public class DashCible : MonoBehaviour
    {
        [Header("Ciblage")]
        [SerializeField] private Camera aimCamera;

        [Header("Définition du Dash")]
        [SerializeField] private DashDefinition dashDefinition;
        [SerializeField] private BounceDefinition groundBounce;
        [SerializeField] private BounceDefinition airBounce;

        [Header("Input")]
        [SerializeField] private KeyCode activationKey = KeyCode.Q;

        [Header("Target Outline")]
        [Tooltip("Couleur de l'outline affiché sur l'ennemi visé")]
        [SerializeField] private Color targetOutlineColor = Color.yellow;
        
        [Tooltip("Épaisseur de l'outline (0-10)")]
        [Range(0f, 10f)]
        [SerializeField] private float targetOutlineWidth = 2f;
        
        [Tooltip("Afficher l'outline pendant le slow-mo même si le cooldown n'est pas prêt")]
        [SerializeField] private bool showOutlineDuringSlowMo = true;

        [Header("Contournement d'obstacles")]
        [Tooltip("Angle maximum pour tenter de contourner un obstacle (degrés)")]
        [Range(15f, 90f)]
        [SerializeField] private float steerMaxAngle = 45f;
        
        [Tooltip("Nombre d'échantillons latéraux pour contourner (2-8)")]
        [Range(2, 8)]
        [SerializeField] private int steerSamples = 4;
        
        [Tooltip("Progrès minimum acceptable pour accepter une trajectoire détournée (0-1)")]
        [Range(0.1f, 0.9f)]
        [SerializeField] private float minAcceptableProgress = 0.25f;

        [Header("Références Joueur")]
        [SerializeField] private FPSPlayerController playerController;
        [SerializeField] private Rigidbody rb;
        [SerializeField] private FPSMovement fpsMovement;
        private PlayerHealth playerHealth;

        public bool isDashing;
        private bool chainActive;
        private int remainingChains;
        private float nextAvailableTime;
        private float slowMoEndUnscaled;
        public bool slowMoApplied;
        private float previousTimeScale = 1f;
        private bool pathElectricStunned;
        private float dashStartTime; // Pour détecter les dashs bloqués
        private Coroutine bounceCoroutine; // Coroutine de rebond active
        private bool hitStopActive; // Flag pour le hitstop en cours
        
        // Outline de la cible visée
        private TargetOutline currentAimedOutline;
        private EnemyHealth currentAimedTarget;

        private static readonly Collider[] OverlapBuffer = new Collider[16];

        private DashDefinition Config => dashDefinition;
        private LayerMask EnemyMask => Config?.enemyMask ?? ~0;
        private LayerMask ObstacleMask => Config?.obstacleMask ?? ~0;
        private float MaxAimAngle => Mathf.Max(0f, Config?.maxAimAngle ?? 30f);
        private int ConfigCountDash => Mathf.Max(1, Config?.countDash ?? 3);
        private float ConfigSlowMoTime => Mathf.Max(0.01f, Config?.slowMoTime ?? 0.75f);
        private float ConfigSlowMoScale => Mathf.Clamp(Config?.slowMoScale ?? 0.2f, 0.01f, 1f);
        private float ConfigDistanceDash => Mathf.Max(0.5f, Config?.distanceDash ?? 25f);
        private float ConfigCooldown => Mathf.Max(0f, Config?.cooldown ?? 1.5f);
        private float ConfigDashDamage => Mathf.Max(0f, Config?.dashDamage ?? 9999f);
        private float ConfigDashTravelTime => Mathf.Max(0.01f, Config?.dashTravelTime ?? 0.08f);
        private float ConfigCapsuleRadius => Mathf.Max(0f, Config?.capsuleRadius ?? 0.4f);
        private float ConfigStopOffset => Mathf.Max(0f, Config?.stopOffset ?? 1f);
        private float ConfigHitStopDuration => Mathf.Max(0f, Config?.hitStopUnscaledDuration ?? 0f);
        private bool ConfigHitStopFreezeTime => Config?.hitStopFreezeTime ?? true;
        private BounceDefinition CurrentBounce
        {
            get
            {
                bool grounded = fpsMovement == null || fpsMovement.IsGrounded;
                if (grounded)
                    return groundBounce ?? airBounce;
                return airBounce ?? groundBounce;
            }
        }

        private void Awake()
        {
            if (playerController == null)
                playerController = GetComponent<FPSPlayerController>();
            if (fpsMovement == null)
                fpsMovement = GetComponent<FPSMovement>();
            // Obtenir le Rigidbody directement pour éviter les problèmes d'ordre d'exécution
            if (rb == null)
                rb = GetComponent<Rigidbody>();
            if (aimCamera == null)
                aimCamera = Camera.main;
            playerHealth = GetComponent<PlayerHealth>();
        }

        private void Update()
        {
            // Sécurité: détecter si le dash est bloqué (plus de 2 secondes)
            if (isDashing && Time.unscaledTime - dashStartTime > 2f)
            {
                Debug.LogWarning("[DashCible] Dash bloqué depuis plus de 2s! Réinitialisation forcée.");
                FinalizeDash();
                if (chainActive) EndChain();
            }
            
            // Sécurité: si le slow-mo est actif mais le temps a expiré, le désactiver
            if (slowMoApplied && Time.unscaledTime >= slowMoEndUnscaled)
            {
                ClearSlowMo();
                if (!isDashing && chainActive)
                {
                    EndChain();
                }
            }
            
            // Sécurité supplémentaire: si Time.timeScale est anormalement bas sans slow-mo actif, restaurer
            if (!slowMoApplied && !hitStopActive && !isDashing && Time.timeScale < 0.5f)
            {
                Debug.LogWarning($"[DashCible] TimeScale anormal ({Time.timeScale}) sans slow-mo actif! Restauration à 1.");
                Time.timeScale = 1f;
                Time.fixedDeltaTime = 0.02f;
            }

            if (Input.GetKeyDown(activationKey))
            {
                TryTriggerOrChain();
            }
            
            // Mettre à jour l'outline de la cible visée
            UpdateAimHighlight();
        }
        
        /// <summary>
        /// Met à jour l'outline de la cible visée.
        /// L'outline est affiché uniquement si:
        /// - Le joueur vise un ennemi ET
        /// - (Le dash est prêt (pas en cooldown) OU le slow-mo est actif)
        /// </summary>
        private void UpdateAimHighlight()
        {
            // Chercher la cible actuellement visée
            EnemyHealth aimed = GetAimedEnemy();
            
            // Vérifier si on peut afficher l'outline
            bool canShowOutline = !IsCooldownActive || (slowMoApplied && showOutlineDuringSlowMo);
            
            // Si la cible a changé
            if (aimed != currentAimedTarget)
            {
                // Cacher l'outline de l'ancienne cible
                if (currentAimedOutline != null)
                {
                    currentAimedOutline.Hide();
                    currentAimedOutline = null;
                }
                
                currentAimedTarget = aimed;
                
                // Afficher l'outline sur la nouvelle cible si les conditions sont remplies
                if (aimed != null && canShowOutline)
                {
                    currentAimedOutline = aimed.GetComponent<TargetOutline>();
                    if (currentAimedOutline == null)
                        currentAimedOutline = aimed.GetComponentInChildren<TargetOutline>();
                    
                    if (currentAimedOutline != null)
                    {
                        currentAimedOutline.Show(targetOutlineColor, targetOutlineWidth);
                    }
                }
            }
            else if (aimed != null)
            {
                // Même cible, mais vérifier si l'état d'affichage doit changer
                if (canShowOutline)
                {
                    // On peut afficher, s'assurer que l'outline est visible
                    if (currentAimedOutline == null)
                    {
                        currentAimedOutline = aimed.GetComponent<TargetOutline>();
                        if (currentAimedOutline == null)
                            currentAimedOutline = aimed.GetComponentInChildren<TargetOutline>();
                    }
                    
                    if (currentAimedOutline != null && !currentAimedOutline.IsShowing)
                    {
                        currentAimedOutline.Show(targetOutlineColor, targetOutlineWidth);
                    }
                }
                else
                {
                    // On ne peut plus afficher, cacher l'outline
                    if (currentAimedOutline != null && currentAimedOutline.IsShowing)
                    {
                        currentAimedOutline.Hide();
                    }
                }
            }
            else
            {
                // Pas de cible visée, s'assurer que l'outline est caché
                if (currentAimedOutline != null)
                {
                    currentAimedOutline.Hide();
                    currentAimedOutline = null;
                }
            }
        }
        
        /// <summary>
        /// Retourne l'ennemi actuellement visé par la caméra (sans déclencher de dash).
        /// Logique similaire à AcquireTarget() mais sans les effets de bord.
        /// </summary>
        private EnemyHealth GetAimedEnemy()
        {
            if (aimCamera == null) return null;

            Ray ray = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            
            // D'abord vérifier le raycast direct
            if (Physics.Raycast(ray, out RaycastHit hit, ConfigDistanceDash, EnemyMask, QueryTriggerInteraction.Ignore))
            {
                var eh = hit.collider.GetComponentInParent<EnemyHealth>() ?? hit.collider.GetComponent<EnemyHealth>();
                if (eh != null && !eh.IsDead)
                {
                    if (!IsObstructed(aimCamera.transform.position, eh.transform.position))
                        return eh;
                }
            }

            // Sinon chercher le meilleur ennemi dans le cône de visée
            var all = FindObjectsByType<EnemyHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            EnemyHealth best = null;
            float bestScore = float.MaxValue;
            Vector3 camPos = aimCamera.transform.position;
            Vector3 camFwd = aimCamera.transform.forward;

            foreach (var eh in all)
            {
                if (eh == null || eh.IsDead) continue;
                Vector3 to = eh.transform.position - camPos;
                float dist = to.magnitude;
                if (dist > ConfigDistanceDash) continue;
                Vector3 dir = to / (dist + 1e-5f);
                float angle = Mathf.Acos(Mathf.Clamp(Vector3.Dot(camFwd, dir), -1f, 1f)) * Mathf.Rad2Deg;
                if (angle > MaxAimAngle) continue;
                if (IsObstructed(camPos, eh.transform.position)) continue;
                float score = angle * 2f + dist * 0.2f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = eh;
                }
            }

            return best;
        }

        private void TryTriggerOrChain()
        {
            // Vérifier si le mouvement est bloqué (bug potentiel)
            if (fpsMovement != null && fpsMovement.IsMovementDisabled && !isDashing)
            {
                Debug.LogWarning("[DashCible] Mouvement était désactivé alors qu'on ne dashait pas! Réactivation forcée.");
                fpsMovement.EnableMovement();
            }
            
            if (isDashing)
            {
                Debug.Log("[DashCible] Dash bloqué: déjà en train de dasher");
                return;
            }

            // Si la chaîne est active mais le slow-mo a expiré et le cooldown est terminé,
            // on peut recommencer une nouvelle chaîne
            if (chainActive && !slowMoApplied && Time.time >= nextAvailableTime)
            {
                Debug.Log("[DashCible] Chaîne précédente expirée, reset pour nouvelle chaîne");
                chainActive = false;
            }

            bool isFirstDash = !chainActive;

            if (!chainActive)
            {
                if (Time.time < nextAvailableTime)
                {
                    Debug.Log($"[DashCible] Dash bloqué: cooldown actif ({nextAvailableTime - Time.time:F2}s restant)");
                    return;
                }
                remainingChains = ConfigCountDash;
                chainActive = true;
                Debug.Log($"[DashCible] Nouvelle chaîne démarrée, {remainingChains} dashs disponibles");
            }
            else
            {
                if (!slowMoApplied || remainingChains <= 0)
                {
                    Debug.Log($"[DashCible] Dash bloqué: slowMo={slowMoApplied}, remainingChains={remainingChains}");
                    return;
                }
            }

            var target = AcquireTarget();
            if (target == null)
            {
                Debug.Log("[DashCible] Pas de cible trouvée");
                // Pas de cible trouvée
                if (isFirstDash)
                {
                    // Premier dash sans cible - annuler la chaîne
                    chainActive = false;
                }
                // Sinon, ignorer le clic sans annuler la chaîne (le slow-mo continue)
                return;
            }

            remainingChains = Mathf.Max(0, remainingChains - 1);
            StartCoroutine(DoTargetDash(target));
        }

        private EnemyHealth AcquireTarget()
        {
            if (aimCamera == null) return null;

            Ray ray = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (Physics.Raycast(ray, out RaycastHit hit, ConfigDistanceDash, EnemyMask, QueryTriggerInteraction.Ignore))
            {
                var eh = hit.collider.GetComponentInParent<EnemyHealth>() ?? hit.collider.GetComponent<EnemyHealth>();
                if (eh != null && !eh.IsDead)
                {
                    if (!IsObstructed(aimCamera.transform.position, eh.transform.position))
                        return eh;
                }
            }

            var all = FindObjectsByType<EnemyHealth>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            EnemyHealth best = null;
            float bestScore = float.MaxValue;
            Vector3 camPos = aimCamera.transform.position;
            Vector3 camFwd = aimCamera.transform.forward;

            foreach (var eh in all)
            {
                if (eh == null || eh.IsDead) continue;
                Vector3 to = eh.transform.position - camPos;
                float dist = to.magnitude;
                if (dist > ConfigDistanceDash) continue;
                Vector3 dir = to / (dist + 1e-5f);
                float angle = Mathf.Acos(Mathf.Clamp(Vector3.Dot(camFwd, dir), -1f, 1f)) * Mathf.Rad2Deg;
                if (angle > MaxAimAngle) continue;
                if (IsObstructed(camPos, eh.transform.position)) continue;
                float score = angle * 2f + dist * 0.2f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = eh;
                }
            }

            return best;
        }

        private bool IsObstructed(Vector3 from, Vector3 to)
        {
            Vector3 dir = to - from;
            float dist = dir.magnitude;
            if (dist <= 0.01f) return false;
            // Utiliser uniquement ObstacleMask sans le layer des ennemis
            LayerMask obstructionMask = ObstacleMask & ~EnemyMask;
            return Physics.Raycast(from, dir.normalized, dist - 0.1f, obstructionMask, QueryTriggerInteraction.Ignore);
        }

        private IEnumerator DoTargetDash(EnemyHealth target)
        {
            Debug.Log($"[DashCible] DoTargetDash démarré vers {target.name}");
            isDashing = true;
            dashStartTime = Time.unscaledTime;
            pathElectricStunned = false;

            Vector3 start = transform.position;
            Vector3 targetPos = target.transform.position;
            Vector3 dirToTarget = (targetPos - start).normalized;
            float distToTarget = Vector3.Distance(start, targetPos);

            // Calculer la position d'arrêt devant l'ennemi
            float stopDist = Mathf.Clamp(ConfigStopOffset, 0f, Mathf.Max(0f, distToTarget - 0.1f));
            Vector3 end = targetPos - dirToTarget * stopDist;

            if (fpsMovement != null)
            {
                fpsMovement.SetSpeedToMax();
                fpsMovement.DisableMovement();
            }

            float t0 = Time.unscaledTime;
            float dur = Mathf.Max(0.01f, ConfigDashTravelTime);
            Vector3 prev = transform.position;

            while (Time.unscaledTime - t0 < dur)
            {
                // Vérifier si la cible existe encore
                if (target == null)
                {
                    Debug.Log("[DashCible] Cible détruite pendant le dash");
                    break;
                }
                
                // Recalculer la destination en temps réel (l'ennemi peut bouger)
                targetPos = target.transform.position;
                
                // Recalculer la direction depuis la position de DEPART (pas la position actuelle)
                // pour maintenir une trajectoire cohérente
                dirToTarget = (targetPos - start).normalized;
                
                // Recalculer la distance totale et la position d'arrêt
                float currentDistToTarget = Vector3.Distance(start, targetPos);
                stopDist = Mathf.Clamp(ConfigStopOffset, 0f, Mathf.Max(0f, currentDistToTarget - 0.1f));
                end = targetPos - dirToTarget * stopDist;

                float u = (Time.unscaledTime - t0) / dur;
                Vector3 desiredPos = Vector3.Lerp(start, end, u);
                
                // Vérifier si on a dépassé la position d'arrêt
                float distToEnd = Vector3.Distance(transform.position, end);
                float distFromStartToEnd = Vector3.Distance(start, end);
                float progressToEnd = 1f - (distToEnd / Mathf.Max(0.01f, distFromStartToEnd));
                
                // Si on est très proche ou on a dépassé, arrêter (mais vérifier les collisions d'abord)
                if (distToEnd < 0.5f || progressToEnd > 0.95f)
                {
                    // Vérifier s'il y a un obstacle entre nous et la position finale
                    Vector3 finalDelta = end - transform.position;
                    Vector3 safeFinalPos = GetSafePosition(transform.position, finalDelta);
                    
                    if (rb != null)
                        rb.MovePosition(safeFinalPos);
                    else
                        transform.position = safeFinalPos;
                    break;
                }
                
                Vector3 delta = desiredPos - prev;

                // Appliquer le mouvement avec vérification de collision
                if (delta.sqrMagnitude > 0.0001f)
                {
                    Vector3 safePos = GetSafePosition(prev, delta);
                    
                    if (rb != null)
                        rb.MovePosition(safePos);
                    else
                        transform.position = safePos;
                }

                prev = transform.position;

                TryStunElectricOnPath(prev);

                yield return null;
            }

            // Mouvement final vers la destination (seulement si on n'est pas déjà arrivé)
            float finalDistToEnd = Vector3.Distance(transform.position, end);
            if (finalDistToEnd > 0.1f && finalDistToEnd < 3f)
            {
                Vector3 finalDelta = end - transform.position;
                if (finalDelta.sqrMagnitude > 0.0001f)
                {
                    Vector3 safeEndPos = GetSafePosition(transform.position, finalDelta);
                    
                    if (rb != null)
                        rb.MovePosition(safeEndPos);
                    else
                        transform.position = safeEndPos;
                }
            }

            // Vérifier si la cible existe encore avant de continuer
            if (target == null)
            {
                Debug.Log("[DashCible] Cible détruite, fin du dash sans dégâts");
                FinalizeDash();
                yield break;
            }

            // Vérifier si on est assez proche de l'ennemi pour appliquer les dégâts
            float finalDistance = Vector3.Distance(transform.position, target.transform.position);
            // Augmenter la tolérance pour s'assurer que le dash compte comme réussi
            bool reachedTarget = finalDistance <= ConfigStopOffset + 3f;
            
            Debug.Log($"[DashCible] Distance finale: {finalDistance:F2}, StopOffset: {ConfigStopOffset:F2}, reachedTarget: {reachedTarget}");

            var electric = target.GetComponent<Ennemies.Effect.ElectricEnnemis>();
            if (electric != null)
            {
                var playerStun = GetComponent<PlayerStunAutoFire>();
                if (playerStun == null) playerStun = gameObject.AddComponent<PlayerStunAutoFire>();
                if (electric.OverrideAutoFireInterval)
                    playerStun.ApplyStun(electric.StunDuration, electric.StunAutoFireInterval);
                else
                    playerStun.ApplyStun(electric.StunDuration);

                if (electric.ResistToDash)
                {
                    FinalizeDash();
                    ClearSlowMo();
                    EndChain();
                    yield break;
                }
            }

            // Appliquer les dégâts seulement si on a atteint la cible
            if (reachedTarget && target != null)
            {
                var hitCol = target.GetComponentInChildren<Collider>();
                var dmg = new DamageInfo(amount: ConfigDashDamage, zoneName: "Dash", type: DamageType.Dash, hitPoint: target.transform.position, hitNormal: -dirToTarget, attacker: transform, hitCollider: hitCol);
                bool applied = target.TryApplyDamage(dmg);

                if (applied)
                {
                    // Hitstop en temps réel AVANT le slow-mo
                    if (ConfigHitStopDuration > 0f)
                    {
                        yield return StartCoroutine(HitStopRealtime(ConfigHitStopDuration, ConfigHitStopFreezeTime));
                    }
                    
                    ApplyOrRefreshSlowMo();
                    StartBounceImpulse(dirToTarget);
                }
            }

            FinalizeDash();

            if (remainingChains <= 0)
            {
                if (!slowMoApplied)
                {
                    EndChain();
                }
            }
        }

        /// <summary>
        /// Retourne une position sûre en vérifiant les collisions entre la position actuelle et la destination.
        /// Tente de contourner les petits obstacles via sliding puis échantillonnage latéral.
        /// Exclut les ennemis pour permettre de dasher à travers eux.
        /// </summary>
        private Vector3 GetSafePosition(Vector3 fromPos, Vector3 delta)
        {
            if (delta.sqrMagnitude < 0.0001f)
                return fromPos;
            
            float moveLen = delta.magnitude;
            Vector3 moveDir = delta.normalized;
            
            // Utiliser un SphereCast pour détecter les obstacles (SANS les ennemis)
            float radius = ConfigCapsuleRadius > 0 ? ConfigCapsuleRadius : 0.4f;
            
            // Exclure les ennemis du masque de collision pour le dash
            LayerMask collisionMask = ObstacleMask & ~EnemyMask;
            
            // Test initial : pas d'obstacle ?
            if (!Physics.SphereCast(fromPos + Vector3.up * 0.5f, radius, moveDir, out RaycastHit hit, moveLen, collisionMask, QueryTriggerInteraction.Ignore))
            {
                // Pas d'obstacle, on peut aller à la destination
                return fromPos + delta;
            }
            
            // === PHASE 1 : Tenter le sliding ===
            Vector3 slideResult = SlideMoveWithMask(fromPos, delta, collisionMask);
            float slideProgress = slideResult.magnitude / moveLen;
            
            // Si le sliding permet un progrès suffisant, l'utiliser
            if (slideProgress >= minAcceptableProgress)
            {
                return fromPos + slideResult;
            }
            
            // === PHASE 2 : Échantillonnage latéral ===
            Vector3 bestMove = slideResult;
            float bestScore = slideProgress;
            
            // Générer des directions alternatives (rotations autour de l'axe Y)
            for (int i = 0; i < steerSamples; i++)
            {
                // Calculer l'angle pour cet échantillon (-maxAngle à +maxAngle)
                float t = (float)(i + 1) / (steerSamples + 1);
                float angle = Mathf.Lerp(-steerMaxAngle, steerMaxAngle, t);
                
                // Rotation autour de l'axe Y
                Quaternion rotation = Quaternion.Euler(0f, angle, 0f);
                Vector3 altDir = rotation * moveDir;
                Vector3 altDelta = altDir * moveLen;
                
                // Tester cette direction alternative
                if (!Physics.SphereCast(fromPos + Vector3.up * 0.5f, radius, altDir, out RaycastHit altHit, moveLen, collisionMask, QueryTriggerInteraction.Ignore))
                {
                    // Pas d'obstacle dans cette direction !
                    // Calculer un score basé sur la déviation angulaire (moins de déviation = meilleur)
                    float deviation = Mathf.Abs(angle) / steerMaxAngle;
                    float score = 1f - (deviation * 0.3f); // Légère pénalité pour déviation
                    
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestMove = altDelta;
                    }
                }
                else
                {
                    // Il y a un obstacle mais peut-être plus loin
                    float altProgress = altHit.distance / moveLen;
                    if (altProgress > bestScore)
                    {
                        bestScore = altProgress;
                        bestMove = altDir * Mathf.Max(0f, altHit.distance - 0.1f);
                    }
                }
            }
            
            // Si on a trouvé une meilleure trajectoire, l'utiliser
            if (bestScore >= minAcceptableProgress)
            {
                return fromPos + bestMove;
            }
            
            // === FALLBACK : Arrêt classique devant l'obstacle ===
            float safeDistance = Mathf.Max(0f, hit.distance - 0.1f);
            return fromPos + moveDir * safeDistance;
        }

        /// <summary>
        /// Calcule un vecteur de déplacement qui glisse le long des obstacles.
        /// Utilise le masque d'obstacles par défaut (incluant potentiellement les ennemis).
        /// </summary>
        private Vector3 SlideMove(Vector3 currentPos, Vector3 desiredDelta)
        {
            return SlideMoveWithMask(currentPos, desiredDelta, ObstacleMask);
        }

        /// <summary>
        /// Calcule un vecteur de déplacement qui glisse le long des obstacles.
        /// Utilise un masque de collision personnalisé.
        /// </summary>
        private Vector3 SlideMoveWithMask(Vector3 currentPos, Vector3 desiredDelta, LayerMask collisionMask)
        {
            Vector3 totalMove = Vector3.zero;
            Vector3 remainingMove = desiredDelta;
            const int maxIterations = 3;

            for (int i = 0; i < maxIterations; i++)
            {
                if (remainingMove.sqrMagnitude < 0.0001f)
                    break;

                float moveLen = remainingMove.magnitude;
                Vector3 moveDir = remainingMove / moveLen;

                Vector3 top = currentPos + totalMove + Vector3.up * 1.5f;
                Vector3 bottom = currentPos + totalMove + Vector3.up * 0.2f;

                if (Physics.CapsuleCast(top, bottom, ConfigCapsuleRadius, moveDir, out RaycastHit hit, moveLen, collisionMask, QueryTriggerInteraction.Ignore))
                {
                    // Avancer jusqu'au point de contact (avec une petite marge)
                    float safeDistance = Mathf.Max(0f, hit.distance - 0.05f);
                    Vector3 safeMove = moveDir * safeDistance;
                    totalMove += safeMove;

                    // Calculer le mouvement restant projeté sur le plan de l'obstacle (sliding)
                    float leftoverDist = moveLen - hit.distance;
                    if (leftoverDist > 0.01f)
                    {
                        Vector3 leftoverDir = moveDir * leftoverDist;
                        remainingMove = Vector3.ProjectOnPlane(leftoverDir, hit.normal);
                    }
                    else
                    {
                        break;
                    }
                }
                else
                {
                    // Pas d'obstacle, on peut avancer complètement
                    totalMove += remainingMove;
                    break;
                }
            }

            return totalMove;
        }

        /// <summary>
        /// Finalise le dash en réactivant le mouvement et en mettant à jour les flags
        /// </summary>
        private void FinalizeDash()
        {
            isDashing = false;
            Debug.Log($"[DashCible] Dash finalisé, remainingChains={remainingChains}, slowMoApplied={slowMoApplied}");
            
            // Réactiver le mouvement normal
            if (fpsMovement != null)
            {
                fpsMovement.EnableMovement();
            }

            // Bloquer temporairement le tir via le WeaponSystem si configuré
            try
            {
                if (Config != null && Config.postDashNoFireDuration > 0f)
                {
                    var ws = GetComponentInChildren<WeaponSystem>();
                    if (ws != null)
                    {
                        ws.DisableShootingFor(Config.postDashNoFireDuration);
                        Debug.Log($"[DashCible] WeaponSystem shooting disabled for {Config.postDashNoFireDuration} seconds (unscaled)");
                    }
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[DashCible] Erreur lors de la tentative de désactivation du tir: {ex}");
            }
        }

        private void ApplyOrRefreshSlowMo()
        {
            slowMoEndUnscaled = Time.unscaledTime + ConfigSlowMoTime;
            if (!slowMoApplied)
            {
                // S'assurer qu'on sauvegarde la bonne valeur (pas le hitstop)
                previousTimeScale = (Time.timeScale <= 0.01f) ? 1f : Time.timeScale;
                Time.timeScale = ConfigSlowMoScale;
                Time.fixedDeltaTime = 0.02f * Time.timeScale;
                slowMoApplied = true;
                Debug.Log($"[DashCible] SlowMo appliqué: timeScale={ConfigSlowMoScale}, previousTimeScale={previousTimeScale}, durée={ConfigSlowMoTime}s");
            }
            else
            {
                Debug.Log($"[DashCible] SlowMo rafraîchi: nouvelle fin dans {ConfigSlowMoTime}s");
            }
        }

        private void ClearSlowMo()
        {
            if (!slowMoApplied) return;
            
            // S'assurer qu'on restaure une valeur valide
            float targetTimeScale = (previousTimeScale <= 0.01f) ? 1f : previousTimeScale;
            Time.timeScale = targetTimeScale;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            slowMoApplied = false;
            
            Debug.Log($"[DashCible] SlowMo terminé: timeScale restauré à {targetTimeScale}");
            
            // S'assurer que le mouvement est réactivé si on n'est plus en train de dasher
            if (!isDashing && fpsMovement != null)
            {
                fpsMovement.EnableMovement();
            }
        }

        private void EndChain()
        {
            chainActive = false;
            remainingChains = 0;
            nextAvailableTime = Time.time + ConfigCooldown;
            
            // S'assurer que le mouvement est réactivé
            if (fpsMovement != null)
            {
                fpsMovement.EnableMovement();
            }
        }

        private void OnDisable()
        {
            // Arrêter les coroutines en cours
            if (bounceCoroutine != null)
            {
                StopCoroutine(bounceCoroutine);
                bounceCoroutine = null;
            }
            
            // Restaurer le temps si hitstop ou slow-mo actif
            if (hitStopActive || slowMoApplied)
            {
                Time.timeScale = 1f;
                Time.fixedDeltaTime = 0.02f;
                hitStopActive = false;
            }
            if (slowMoApplied) ClearSlowMo();
            if (fpsMovement != null) fpsMovement.EnableMovement();
            
            // Cacher l'outline de la cible visée
            if (currentAimedOutline != null)
            {
                currentAimedOutline.Hide();
                currentAimedOutline = null;
            }
            currentAimedTarget = null;
        }

        private void OnDestroy()
        {
            // Arrêter les coroutines en cours
            if (bounceCoroutine != null)
            {
                StopCoroutine(bounceCoroutine);
                bounceCoroutine = null;
            }
            
            // Restaurer le temps si hitstop ou slow-mo actif
            if (hitStopActive || slowMoApplied)
            {
                Time.timeScale = 1f;
                Time.fixedDeltaTime = 0.02f;
                hitStopActive = false;
            }
            if (slowMoApplied) ClearSlowMo();
            if (fpsMovement != null) fpsMovement.EnableMovement();
            
            // Cacher l'outline de la cible visée
            if (currentAimedOutline != null)
            {
                currentAimedOutline.Hide();
                currentAimedOutline = null;
            }
            currentAimedTarget = null;
        }

        public int CountDash => ConfigCountDash;
        public float SlowMoTime => ConfigSlowMoTime;
        public float DistanceDash => ConfigDistanceDash;
        public float Cooldown => ConfigCooldown;
        
        public bool IsChainActive => chainActive;
        public int RemainingChains => chainActive ? Mathf.Clamp(remainingChains, 0, ConfigCountDash) : ConfigCountDash;
        public bool IsSlowMoActive => slowMoApplied;
        
        /// <summary>True si le cooldown global est en cours (après la fin du slow-mo)</summary>
        public bool IsCooldownActive => !chainActive && Time.time < nextAvailableTime;
        
        /// <summary>Progression du cooldown de 0 (début) à 1 (terminé)</summary>
        public float CooldownProgress => IsCooldownActive 
            ? 1f - ((nextAvailableTime - Time.time) / ConfigCooldown) 
            : 1f;
        
        private void TryStunElectricOnPath(Vector3 currentPos)
        {
            if (pathElectricStunned) return;
            Vector3 top = currentPos + Vector3.up * 1.5f;
            Vector3 bottom = currentPos + Vector3.up * 0.2f;
            int count = Physics.OverlapCapsuleNonAlloc(top, bottom, ConfigCapsuleRadius, OverlapBuffer, EnemyMask, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < count; i++)
            {
                var col = OverlapBuffer[i];
                if (col == null) continue;
                var electric = col.GetComponentInParent<Ennemies.Effect.ElectricEnnemis>() ?? col.GetComponent<Ennemies.Effect.ElectricEnnemis>();
                if (electric == null) continue;

                var playerStun = GetComponent<PlayerStunAutoFire>() ?? gameObject.AddComponent<PlayerStunAutoFire>();
                if (electric.OverrideAutoFireInterval)
                    playerStun.ApplyStun(electric.StunDuration, electric.StunAutoFireInterval);
                else
                    playerStun.ApplyStun(electric.StunDuration);

                pathElectricStunned = true;
                break;
            }
        }

        /// <summary>
        /// Applique un hitstop en temps réel (pause du jeu) avant de continuer.
        /// </summary>
        private IEnumerator HitStopRealtime(float unscaledDuration, bool freezeTime)
        {
            if (unscaledDuration <= 0f)
                yield break;

            hitStopActive = true;
            
            // Sauvegarder l'état actuel du temps (s'assurer que c'est une valeur valide)
            float savedTimeScale = (Time.timeScale <= 0.01f) ? 1f : Time.timeScale;
            float savedFixedDeltaTime = Time.fixedDeltaTime;

            // Appliquer le freeze
            Time.timeScale = freezeTime ? 0f : 0.01f;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;

            // Attendre en temps réel
            yield return new WaitForSecondsRealtime(unscaledDuration);

            // Restaurer l'état du temps (sauf si slow-mo est déjà appliqué, ce qui sera géré après)
            if (!slowMoApplied)
            {
                Time.timeScale = savedTimeScale;
                Time.fixedDeltaTime = 0.02f * savedTimeScale;
            }
            else
            {
                // Le slow-mo est déjà appliqué, restaurer sa valeur
                float targetTimeScale = (previousTimeScale <= 0.01f) ? 1f : previousTimeScale;
                Time.timeScale = ConfigSlowMoScale;
                Time.fixedDeltaTime = 0.02f * ConfigSlowMoScale;
            }

            hitStopActive = false;
        }

        /// <summary>
        /// Démarre la coroutine de rebond avec courbe.
        /// </summary>
        private void StartBounceImpulse(Vector3 dashDirection)
        {
            if (bounceCoroutine != null)
            {
                StopCoroutine(bounceCoroutine);
            }
            bounceCoroutine = StartCoroutine(ApplyBounceImpulseOverTime(dashDirection, CurrentBounce));
        }

        /// <summary>
        /// Applique l'impulsion de rebond sur une durée, pilotée par une courbe.
        /// La courbe contrôle la vitesse du rebond à chaque instant (Y=1 = vitesse max, Y=0 = pas de mouvement).
        /// </summary>
        private IEnumerator ApplyBounceImpulseOverTime(Vector3 dashDirection, BounceDefinition config)
        {
            if (config == null || config.force <= 0f)
            {
                bounceCoroutine = null;
                yield break;
            }

            Vector3 dir = ResolveBounceDirection(dashDirection, config);
            if (dir.sqrMagnitude <= 1e-4f)
            {
                bounceCoroutine = null;
                yield break;
            }

            // Si pas de durée ou pas de courbe, appliquer instantanément (comportement legacy)
            if (config.duration <= 0f || config.forceOverTime == null)
            {
                Vector3 instantImpulse = dir.normalized * config.force;
                ApplyMomentum(instantImpulse);
                bounceCoroutine = null;
                yield break;
            }

            // Application sur la durée avec la courbe
            // La courbe définit le multiplicateur de vitesse à chaque instant
            float elapsed = 0f;
            float duration = config.duration;
            Vector3 direction = dir.normalized;

            while (elapsed < duration)
            {
                // Calculer le facteur de la courbe (temps normalisé 0-1)
                float t = elapsed / duration;
                float curveValue = config.forceOverTime.Evaluate(t);

                // La courbe contrôle directement la vitesse à cet instant
                // force = vitesse de base, curveValue = multiplicateur de cette vitesse
                Vector3 velocity = direction * (config.force * curveValue);
                
                // Appliquer comme momentum (le système de momentum gère le déplacement)
                ApplyMomentum(velocity);

                elapsed += Time.deltaTime;
                yield return null;
            }

            bounceCoroutine = null;
        }

        /// <summary>
        /// Applique un momentum au joueur via fpsMovement ou fallback rb.
        /// </summary>
        private void ApplyMomentum(Vector3 momentum)
        {
            if (fpsMovement != null)
            {
                fpsMovement.ApplyExternalMomentum(momentum);
            }
            else if (rb != null)
            {
                rb.AddForce(momentum, ForceMode.VelocityChange);
            }
            else
            {
                transform.position += momentum * Time.deltaTime;
            }
        }

        private Vector3 ResolveBounceDirection(Vector3 fallbackDashDirection, BounceDefinition config)
        {
            Vector3 dir = config.directionIsLocal ? transform.TransformDirection(config.direction) : config.direction;
            if (dir.sqrMagnitude <= 1e-4f)
                dir = -fallbackDashDirection;
            if (dir.sqrMagnitude <= 1e-4f)
                return Vector3.up;
            return dir.normalized;
        }
    }
}

