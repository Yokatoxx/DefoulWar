using System;
using System.Collections;
using UnityEngine;

namespace FPS
{
    /// <summary>
    /// Système de dash ciblé vers les ennemis.
    /// Utilise DashSettings pour centraliser toute la configuration.
    /// </summary>
    [RequireComponent(typeof(DashSlowMo))]
    [RequireComponent(typeof(DashBounce))]
    [RequireComponent(typeof(DashHighlight))]
    [RequireComponent(typeof(DashHitStop))]
    public class DashCible : MonoBehaviour
    {
        // Événement déclenché quand le dash touche un ennemi (position de l'impact)
        public static event Action<Vector3> OnDashImpact;
        
        #region Serialized Fields
        
        [Header("Configuration")]
        [SerializeField] private DashSettings settings;

        [Header("Ciblage")]
        [SerializeField] private Camera aimCamera;

        [Header("Input")]
        [SerializeField] private KeyCode activationKey = KeyCode.Q;

        [Header("Contournement d'obstacles")]
        [Range(15f, 90f)]
        [SerializeField] private float steerMaxAngle = 45f;
        [Range(2, 8)]
        [SerializeField] private int steerSamples = 4;
        [Range(0.1f, 0.9f)]
        [SerializeField] private float minAcceptableProgress = 0.25f;

        [Header("Références Joueur")]
        [SerializeField] private FPSPlayerController playerController;
        [SerializeField] private Rigidbody rb;
        [SerializeField] private FPSMovement fpsMovement;
        
        #endregion

        #region Private Fields
        
        // Modules
        private DashSlowMo slowMo;
        private DashBounce bounce;
        private DashHighlight highlight;
        private DashHitStop hitStop;
        private PlayerHealth playerHealth;
        
        // État du dash
        public bool isDashing;
        private bool chainActive;
        private int remainingChains;
        private float nextAvailableTime;
        private bool pathElectricStunned;
        private float dashStartTime;
        private Vector3 lastDashDirection;
        private bool waitingForLandingCooldown;

        private static readonly Collider[] OverlapBuffer = new Collider[16];
        
        #endregion

        #region Properties - Configuration
        
        private DashSettings Config => settings;
        private LayerMask EnemyMask => Config?.enemyMask ?? ~0;
        private LayerMask ObstacleMask => Config?.obstacleMask ?? ~0;
        private float MaxAimAngle => Mathf.Max(0f, Config?.maxAimAngle ?? 30f);
        private int ConfigCountDash => Mathf.Max(1, Config?.countDash ?? 3);
        private float ConfigDistanceDash => Mathf.Max(0.5f, Config?.distanceDash ?? 25f);
        private float ConfigCooldown => Mathf.Max(0f, Config?.cooldown ?? 1.5f);
        private float ConfigDashDamage => Mathf.Max(0f, Config?.dashDamage ?? 9999f);
        private float ConfigDashTravelTime => Mathf.Max(0.01f, Config?.dashTravelTime ?? 0.08f);
        private float ConfigCapsuleRadius => Mathf.Max(0f, Config?.capsuleRadius ?? 0.4f);
        private float ConfigStopOffset => Mathf.Max(0f, Config?.stopOffset ?? 1f);
        
        #endregion

        #region Properties - Public State
        
        public int CountDash => ConfigCountDash;
        public float SlowMoTime => slowMo?.SlowMoDuration ?? 0.75f;
        public float DistanceDash => ConfigDistanceDash;
        public float Cooldown => ConfigCooldown;
        public bool IsChainActive => chainActive;
        public int RemainingChains => chainActive ? Mathf.Clamp(remainingChains, 0, ConfigCountDash) : ConfigCountDash;
        public bool IsSlowMoActive => slowMo?.IsActive ?? false;
        public bool slowMoApplied => IsSlowMoActive;
        public bool IsWaitingForLanding => waitingForLandingCooldown;
        public bool IsCooldownActive => !chainActive && (waitingForLandingCooldown || Time.time < nextAvailableTime);
        public float CooldownProgress => waitingForLandingCooldown ? 0f : (IsCooldownActive ? 1f - ((nextAvailableTime - Time.time) / ConfigCooldown) : 1f);
        public bool IsDashReady => !chainActive && !waitingForLandingCooldown && Time.time >= nextAvailableTime;
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            // Récupérer les références
            if (playerController == null) playerController = GetComponent<FPSPlayerController>();
            if (fpsMovement == null) fpsMovement = GetComponent<FPSMovement>();
            if (rb == null) rb = GetComponent<Rigidbody>();
            if (aimCamera == null) aimCamera = Camera.main;
            
            playerHealth = GetComponent<PlayerHealth>();
            
            // Récupérer les modules
            slowMo = GetComponent<DashSlowMo>();
            bounce = GetComponent<DashBounce>();
            highlight = GetComponent<DashHighlight>();
            hitStop = GetComponent<DashHitStop>();
            
            ConfigureModules();
        }
        
        private void ConfigureModules()
        {
            if (Config == null) return;
            
            slowMo?.Configure(Config.slowMo);
            bounce?.Configure(Config.groundBounce, Config.airBounce);
            hitStop?.Configure(Config.hitStop);
        }

        private void Update()
        {
            // Sécurité: détecter si le dash est bloqué
            if (isDashing && Time.unscaledTime - dashStartTime > 0.5f)
            {
                Debug.LogWarning("[DashCible] Dash bloqué depuis plus de 0.5s! Réinitialisation forcée.");
                FinalizeDash();
                if (chainActive) EndChain();
            }
            
            // Sécurité: fin du slow-mo (mais pas si on bounce encore)
            bool isBouncing = bounce != null && bounce.IsBouncing;
            if (slowMo != null && !slowMo.IsActive && chainActive && !isDashing && !isBouncing)
            {
                EndChain();
            }
            
            // Sécurité: timeScale anormal (seulement si rien n'est actif)
            bool anyEffectActive = (slowMo != null && slowMo.IsActive) || 
                                   (hitStop != null && hitStop.IsActive) || 
                                   isDashing || isBouncing;
            if (!anyEffectActive && Time.timeScale < 0.5f && Time.timeScale > 0f)
            {
                Debug.LogWarning($"[DashCible] TimeScale anormal ({Time.timeScale})! Restauration à 1.");
                Time.timeScale = 1f;
                Time.fixedDeltaTime = 0.02f;
            }

            // Démarrer le cooldown à l'atterrissage
            if (waitingForLandingCooldown && fpsMovement != null && fpsMovement.IsGrounded)
            {
                waitingForLandingCooldown = false;
                nextAvailableTime = Time.time + ConfigCooldown;
            }

            if (Input.GetKeyDown(activationKey))
            {
                TryTriggerOrChain();
            }
            
            UpdateHighlight();
        }
        
        private void OnDisable()
        {
            CleanupState();
        }
        
        private void OnDestroy()
        {
            CleanupState();
        }
        
        private void CleanupState()
        {
            slowMo?.Clear();
            hitStop?.Restore();
            bounce?.CancelBounce();
            highlight?.ClearHighlight();
            fpsMovement?.EnableMovement();
        }
        
        #endregion

        #region Targeting
        
        private void UpdateHighlight()
        {
            EnemyHealth aimed = GetAimedEnemy();
            bool canShow = !IsCooldownActive || (slowMo.IsActive && highlight.ShowDuringSlowMo);
            highlight?.UpdateHighlight(aimed, canShow);
        }
        
        private EnemyHealth GetAimedEnemy()
        {
            if (aimCamera == null) return null;

            Ray ray = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            
            if (Physics.Raycast(ray, out RaycastHit hit, ConfigDistanceDash, EnemyMask, QueryTriggerInteraction.Ignore))
            {
                var eh = hit.collider.GetComponentInParent<EnemyHealth>() ?? hit.collider.GetComponent<EnemyHealth>();
                if (eh != null && !eh.IsDead && !IsObstructed(aimCamera.transform.position, eh.transform.position))
                    return eh;
            }

            return FindBestTargetInCone();
        }
        
        private EnemyHealth FindBestTargetInCone()
        {
            var aliveEnemies = EnemyRegistry.Instance.GetAliveEnemies();
            EnemyHealth best = null;
            float bestScore = float.MaxValue;
            Vector3 camPos = aimCamera.transform.position;
            Vector3 camFwd = aimCamera.transform.forward;

            foreach (var eh in aliveEnemies)
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
            LayerMask obstructionMask = ObstacleMask & ~EnemyMask;
            return Physics.Raycast(from, dir.normalized, dist - 0.1f, obstructionMask, QueryTriggerInteraction.Ignore);
        }
        
        #endregion

        #region Dash Logic
        
        private void TryTriggerOrChain()
        {
            if (fpsMovement != null && fpsMovement.IsMovementDisabled && !isDashing)
            {
                fpsMovement.EnableMovement();
            }
            
            if (isDashing) return;

            if (chainActive && !slowMo.IsActive && Time.time >= nextAvailableTime)
            {
                chainActive = false;
            }

            bool isFirstDash = !chainActive;

            if (!chainActive)
            {
                // Bloquer si cooldown actif OU si on attend l'atterrissage
                if (waitingForLandingCooldown || Time.time < nextAvailableTime) return;
                remainingChains = ConfigCountDash;
                chainActive = true;
            }
            else
            {
                // On peut re-dasher SEULEMENT quand le slow-mo est actif (pas avant)
                if (!slowMo.IsActive || remainingChains <= 0) return;
            }

            var target = GetAimedEnemy();
            if (target == null)
            {
                if (isFirstDash) chainActive = false;
                return;
            }

            remainingChains = Mathf.Max(0, remainingChains - 1);
            StartCoroutine(DoTargetDash(target));
        }

        private IEnumerator DoTargetDash(EnemyHealth target)
        {
            isDashing = true;
            dashStartTime = Time.unscaledTime;
            pathElectricStunned = false;

            Vector3 start = transform.position;
            Vector3 targetPos = target.transform.position;
            Vector3 dirToTarget = (targetPos - start).normalized;
            lastDashDirection = dirToTarget;

            fpsMovement?.SetSpeedToMax();
            fpsMovement?.DisableMovement();

            yield return StartCoroutine(MoveToDashTarget(start, target));

            if (target == null)
            {
                FinalizeDash();
                yield break;
            }

            float finalDistance = Vector3.Distance(transform.position, target.transform.position);
            bool reachedTarget = finalDistance <= ConfigStopOffset + 3f;

            // Gestion ennemi électrique
            var electric = target.GetComponent<Ennemies.Effect.ElectricEnnemis>();
            if (electric != null)
            {
                ApplyElectricStun(electric);
                if (electric.ResistToDash)
                {
                    FinalizeDash();
                    slowMo?.Clear();
                    EndChain();
                    yield break;
                }
            }

            // Appliquer dégâts et effets
            if (reachedTarget && target != null)
            {
                ApplyDashDamage(target, dirToTarget);
            }

            FinalizeDash();

            if (remainingChains <= 0 && !slowMo.IsActive)
            {
                EndChain();
            }
        }
        
        private IEnumerator MoveToDashTarget(Vector3 start, EnemyHealth target)
        {
            float t0 = Time.unscaledTime;
            float dur = ConfigDashTravelTime;
            Vector3 prev = transform.position;

            // Vérifier si l'ennemi a un bouclier actif face au joueur
            var shield = target.GetComponentInChildren<Ennemies.Effect.EnemyShield>();
            bool shieldBlocking = false;
            Vector3 shieldPos = Vector3.zero;
            
            if (shield != null && shield.ShieldActive)
            {
                Vector3 dirFromEnemy = (start - target.transform.position).normalized;
                float angle = Vector3.Angle(target.transform.forward, dirFromEnemy);
                // Si on vient de devant (dans le cône de blocage du shield)
                if (angle <= 45f)
                {
                    shieldBlocking = true;
                    // Utiliser la position du bouclier comme point d'arrêt
                    shieldPos = shield.transform.position;
                }
            }

            while (Time.unscaledTime - t0 < dur)
            {
                if (target == null) break;
                
                Vector3 targetPos = target.transform.position;
                Vector3 dirToTarget = (targetPos - start).normalized;
                
                // Si le bouclier bloque, s'arrêter au bouclier au lieu du centre de l'ennemi
                Vector3 effectiveTargetPos = shieldBlocking ? shieldPos : targetPos;
                float currentDistToTarget = Vector3.Distance(start, effectiveTargetPos);
                float stopDist = Mathf.Clamp(ConfigStopOffset, 0f, Mathf.Max(0f, currentDistToTarget - 0.1f));
                
                // Pour le bouclier, s'arrêter plus loin pour ne pas rentrer dedans
                if (shieldBlocking)
                {
                    stopDist = Mathf.Max(stopDist, 1.0f);
                }
                
                Vector3 end = effectiveTargetPos - dirToTarget * stopDist;

                float u = (Time.unscaledTime - t0) / dur;
                Vector3 desiredPos = Vector3.Lerp(start, end, u);
                
                float distToEnd = Vector3.Distance(transform.position, end);
                if (distToEnd < 0.5f)
                {
                    MoveToSafePosition(end);
                    break;
                }
                
                Vector3 delta = desiredPos - prev;
                if (delta.sqrMagnitude > 0.0001f)
                {
                    MoveToSafePosition(prev + delta);
                }

                prev = transform.position;
                TryStunElectricOnPath(prev);

                yield return null;
            }
        }
        
        private void MoveToSafePosition(Vector3 targetPos)
        {
            Vector3 delta = targetPos - transform.position;
            Vector3 safePos = GetSafePosition(transform.position, delta);
            
            if (rb != null)
                rb.MovePosition(safePos);
            else
                transform.position = safePos;
        }
        
        private void ApplyDashDamage(EnemyHealth target, Vector3 dirToTarget)
        {
            var shield = target.GetComponentInChildren<Ennemies.Effect.EnemyShield>();
            Collider hitCol = null;

            if (shield != null && shield.ShieldActive)
            {
                Vector3 enemyForward = target.transform.forward;
                Vector3 attackerDir = -dirToTarget;
                float angle = Vector3.Angle(enemyForward, attackerDir);

                if (angle <= 45f)
                {
                    hitCol = shield.GetComponent<Collider>();
                }
            }

            if (hitCol == null)
            {
                hitCol = target.GetComponentInChildren<Collider>();
            }

            var dmg = new DamageInfo(
                amount: ConfigDashDamage,
                zoneName: "Dash",
                type: DamageType.Dash,
                hitPoint: target.transform.position,
                hitNormal: -dirToTarget,
                attacker: transform,
                hitCollider: hitCol
            );
            
            // Notifier les listeners de l'impact AVANT d'appliquer les dégâts
            // pour que les effets visuels se déclenchent avant la destruction potentielle de l'ennemi
            OnDashImpact?.Invoke(target.transform.position);
            
            bool applied = target.TryApplyDamage(dmg);

            if (applied)
            {
                // Bounce joueur - IMMÉDIAT à l'impact
                bounce?.StartBounce(dirToTarget);
                
                // ScreenShake à l'impact
                ApplyScreenShake();
                
                // Knockback ennemi - IMMÉDIAT à l'impact
                ApplyEnemyKnockback(target, dirToTarget);
                
                // Repousse les ennemis autour de la cible
                ApplyAreaKnockback(target.transform.position);
                
                // HitStop (non-bloquant, s'exécute en parallèle)
                if (hitStop != null && hitStop.IsEnabled)
                {
                    hitStop.Apply(null); // Pas de callback, on n'attend pas
                }
                
                // SlowMo
                slowMo?.ApplyOrRefresh();
            }
        }
        
        private void ApplyEnemyKnockback(EnemyHealth target, Vector3 direction)
        {
            if (Config?.knockback == null || !Config.knockback.enabled) return;
            
            var knockback = target.GetComponent<EnemyKnockback>();
            if (knockback == null || knockback.ResistToKnockback) return;
            
            knockback.ApplyKnockback(
                direction,
                Config.knockback.force,
                Config.knockback.duration,
                Config.knockback.affectsYAxis
            );
        }
        
        private void ApplyScreenShake()
        {
            if (Config?.screenShake == null || !Config.screenShake.enabled) return;
            if (CameraShake.Instance == null) return;
            
            CameraShake.Instance.ShakeWithRotation(
                Config.screenShake.duration,
                Config.screenShake.positionMagnitude,
                Config.screenShake.rotationMagnitude
            );
        }
        
        /// <summary>
        /// Repousse les ennemis dans un cercle autour du point d'impact du dash
        /// </summary>
        private void ApplyAreaKnockback(Vector3 impactPosition)
        {
            if (Config?.areaKnockback == null || !Config.areaKnockback.enabled) return;
            
            float radius = Config.areaKnockback.radius;
            int count = Physics.OverlapSphereNonAlloc(
                impactPosition, 
                radius, 
                OverlapBuffer, 
                EnemyMask, 
                QueryTriggerInteraction.Ignore
            );
            
            for (int i = 0; i < count; i++)
            {
                var col = OverlapBuffer[i];
                if (col == null) continue;
                
                // Récupérer le knockback de l'ennemi
                var knockback = col.GetComponentInParent<EnemyKnockback>();
                if (knockback == null) knockback = col.GetComponent<EnemyKnockback>();
                if (knockback == null || knockback.ResistToKnockback) continue;
                
                // Ne pas repousser l'ennemi qu'on vient de toucher directement (déjà knockback)
                if (knockback.IsKnockbackActive) continue;
                
                // Direction depuis le point d'impact vers l'ennemi (repousse vers l'extérieur)
                Vector3 pushDir = (col.transform.position - impactPosition).normalized;
                if (pushDir.sqrMagnitude < 1e-4f) pushDir = Vector3.up;
                
                knockback.ApplyKnockback(
                    pushDir,
                    Config.areaKnockback.force,
                    Config.areaKnockback.duration,
                    Config.areaKnockback.affectsYAxis
                );
            }
        }
        
        private void ApplyElectricStun(Ennemies.Effect.ElectricEnnemis electric)
        {
            var playerStun = GetComponent<PlayerStunAutoFire>();
            if (playerStun == null) playerStun = gameObject.AddComponent<PlayerStunAutoFire>();
            
            if (electric.OverrideAutoFireInterval)
                playerStun.ApplyStun(electric.StunDuration, electric.StunAutoFireInterval);
            else
                playerStun.ApplyStun(electric.StunDuration);
        }
        
        #endregion

        #region Navigation
        
        private Vector3 GetSafePosition(Vector3 fromPos, Vector3 delta)
        {
            if (delta.sqrMagnitude < 0.0001f) return fromPos;
            
            float moveLen = delta.magnitude;
            Vector3 moveDir = delta.normalized;
            float radius = ConfigCapsuleRadius > 0 ? ConfigCapsuleRadius : 0.4f;
            LayerMask collisionMask = ObstacleMask & ~EnemyMask;
            
            if (!Physics.SphereCast(fromPos + Vector3.up * 0.5f, radius, moveDir, out RaycastHit hit, moveLen, collisionMask, QueryTriggerInteraction.Ignore))
            {
                return fromPos + delta;
            }
            
            Vector3 slideResult = SlideMove(fromPos, delta, collisionMask);
            float slideProgress = slideResult.magnitude / moveLen;
            
            if (slideProgress >= minAcceptableProgress)
                return fromPos + slideResult;
            
            Vector3 bestMove = slideResult;
            float bestScore = slideProgress;
            
            for (int i = 0; i < steerSamples; i++)
            {
                float t = (float)(i + 1) / (steerSamples + 1);
                float angle = Mathf.Lerp(-steerMaxAngle, steerMaxAngle, t);
                Vector3 altDir = Quaternion.Euler(0f, angle, 0f) * moveDir;
                
                if (!Physics.SphereCast(fromPos + Vector3.up * 0.5f, radius, altDir, out RaycastHit altHit, moveLen, collisionMask, QueryTriggerInteraction.Ignore))
                {
                    float score = 1f - (Mathf.Abs(angle) / steerMaxAngle * 0.3f);
                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestMove = altDir * moveLen;
                    }
                }
                else
                {
                    float altProgress = altHit.distance / moveLen;
                    if (altProgress > bestScore)
                    {
                        bestScore = altProgress;
                        bestMove = altDir * Mathf.Max(0f, altHit.distance - 0.1f);
                    }
                }
            }
            
            if (bestScore >= minAcceptableProgress)
                return fromPos + bestMove;
            
            return fromPos + moveDir * Mathf.Max(0f, hit.distance - 0.1f);
        }

        private Vector3 SlideMove(Vector3 currentPos, Vector3 desiredDelta, LayerMask collisionMask)
        {
            Vector3 totalMove = Vector3.zero;
            Vector3 remainingMove = desiredDelta;

            for (int i = 0; i < 3; i++)
            {
                if (remainingMove.sqrMagnitude < 0.0001f) break;

                float moveLen = remainingMove.magnitude;
                Vector3 moveDir = remainingMove / moveLen;
                Vector3 top = currentPos + totalMove + Vector3.up * 1.5f;
                Vector3 bottom = currentPos + totalMove + Vector3.up * 0.2f;

                if (Physics.CapsuleCast(top, bottom, ConfigCapsuleRadius, moveDir, out RaycastHit hit, moveLen, collisionMask, QueryTriggerInteraction.Ignore))
                {
                    float safeDistance = Mathf.Max(0f, hit.distance - 0.05f);
                    totalMove += moveDir * safeDistance;
                    
                    float leftoverDist = moveLen - hit.distance;
                    if (leftoverDist > 0.01f)
                        remainingMove = Vector3.ProjectOnPlane(moveDir * leftoverDist, hit.normal);
                    else
                        break;
                }
                else
                {
                    totalMove += remainingMove;
                    break;
                }
            }

            return totalMove;
        }
        
        #endregion

        #region State Management
        
        private void FinalizeDash()
        {
            isDashing = false;
            fpsMovement?.EnableMovement();

            if (Config != null && Config.postDashNoFireDuration > 0f)
            {
                var ws = GetComponentInChildren<WeaponSystem>();
                ws?.DisableShootingFor(Config.postDashNoFireDuration);
            }
        }

        private void EndChain()
        {
            chainActive = false;
            remainingChains = 0;
            
            // Le cooldown ne démarre qu'à l'atterrissage
            if (fpsMovement != null && fpsMovement.IsGrounded)
            {
                nextAvailableTime = Time.time + ConfigCooldown;
            }
            else
            {
                waitingForLandingCooldown = true;
            }
            
            fpsMovement?.EnableMovement();
        }
        
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

                ApplyElectricStun(electric);
                pathElectricStunned = true;
                break;
            }
        }
        
        #endregion
    }
}
