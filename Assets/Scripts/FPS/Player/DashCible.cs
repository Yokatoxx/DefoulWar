using System.Collections;
using UnityEngine;

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

        [Header("Références Joueur")]
        [SerializeField] private FPSPlayerController playerController;
        [SerializeField] private CharacterController characterController;
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
            if (characterController == null && playerController != null)
                characterController = playerController.Controller;
            if (fpsMovement == null)
                fpsMovement = GetComponent<FPSMovement>();
            if (aimCamera == null)
                aimCamera = Camera.main;
            playerHealth = GetComponent<PlayerHealth>();
        }

        private void Update()
        {
            if (slowMoApplied && Time.unscaledTime >= slowMoEndUnscaled)
            {
                ClearSlowMo();
                if (!isDashing && chainActive)
                {
                    EndChain();
                }
            }

            if (Input.GetKeyDown(activationKey))
            {
                TryTriggerOrChain();
            }
        }

        private void TryTriggerOrChain()
        {
            if (isDashing) return;

            bool isFirstDash = !chainActive;

            if (!chainActive)
            {
                if (Time.time < nextAvailableTime) return;
                remainingChains = ConfigCountDash;
                chainActive = true;
            }
            else
            {
                if (!slowMoApplied || remainingChains <= 0)
                {
                    return;
                }
            }

            var target = AcquireTarget();
            if (target == null)
            {
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
            isDashing = true;
            pathElectricStunned = false;

            Vector3 start = transform.position;
            Vector3 targetPos = target.transform.position;
            Vector3 dirToTarget = (targetPos - start).normalized;
            float distToTarget = Vector3.Distance(start, targetPos);

            float stopDist = Mathf.Clamp(ConfigStopOffset, 0f, Mathf.Max(0f, distToTarget - 0.1f));
            Vector3 end = targetPos - dirToTarget * stopDist;

            if (fpsMovement != null)
            {
                fpsMovement.SetSpeedToMax();
            }

            float t0 = Time.unscaledTime;
            float dur = Mathf.Max(0.01f, ConfigDashTravelTime);
            Vector3 prev = transform.position;

            while (Time.unscaledTime - t0 < dur)
            {
                // Recalculer la destination en temps réel (l'ennemi peut bouger)
                targetPos = target.transform.position;
                dirToTarget = (targetPos - start).normalized;
                stopDist = Mathf.Clamp(ConfigStopOffset, 0f, Mathf.Max(0f, Vector3.Distance(start, targetPos) - 0.1f));
                end = targetPos - dirToTarget * stopDist;

                float u = (Time.unscaledTime - t0) / dur;
                Vector3 desiredPos = Vector3.Lerp(start, end, u);
                Vector3 delta = desiredPos - prev;

                // Appliquer le mouvement avec sliding sur les obstacles
                if (characterController != null && delta.sqrMagnitude > 0.0001f)
                {
                    delta = SlideMove(prev, delta);
                    characterController.Move(delta);
                }
                else if (delta.sqrMagnitude > 0.0001f)
                {
                    transform.position = desiredPos;
                }

                prev = transform.position;

                TryStunElectricOnPath(prev);

                yield return null;
            }

            // Mouvement final vers la destination
            Vector3 finalDelta = end - transform.position;
            if (characterController != null && finalDelta.sqrMagnitude > 0.0001f)
            {
                finalDelta = SlideMove(transform.position, finalDelta);
                characterController.Move(finalDelta);
            }
            else if (finalDelta.sqrMagnitude > 0.0001f)
            {
                transform.position = end;
            }

            // Vérifier si on est assez proche de l'ennemi pour appliquer les dégâts
            float finalDistance = Vector3.Distance(transform.position, target.transform.position);
            bool reachedTarget = finalDistance <= ConfigStopOffset + 1f;

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
                    isDashing = false;
                    ClearSlowMo();
                    EndChain();
                    yield break;
                }
            }

            // Appliquer les dégâts seulement si on a atteint la cible
            if (reachedTarget)
            {
                var hitCol = target.GetComponentInChildren<Collider>();
                var dmg = new DamageInfo(amount: ConfigDashDamage, zoneName: "Dash", type: DamageType.Dash, hitPoint: target.transform.position, hitNormal: -dirToTarget, attacker: transform, hitCollider: hitCol);
                bool applied = target.TryApplyDamage(dmg);

                if (applied)
                {
                    ApplyOrRefreshSlowMo();
                    ApplyBounceImpulse(dirToTarget);
                }
            }

            isDashing = false;

            if (remainingChains <= 0)
            {
                if (!slowMoApplied)
                {
                    EndChain();
                }
            }
        }

        /// <summary>
        /// Calcule un vecteur de déplacement qui glisse le long des obstacles.
        /// </summary>
        private Vector3 SlideMove(Vector3 currentPos, Vector3 desiredDelta)
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

                if (Physics.CapsuleCast(top, bottom, ConfigCapsuleRadius, moveDir, out RaycastHit hit, moveLen, ObstacleMask, QueryTriggerInteraction.Ignore))
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

        private void ApplyOrRefreshSlowMo()
        {
            slowMoEndUnscaled = Time.unscaledTime + ConfigSlowMoTime;
            if (!slowMoApplied)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = ConfigSlowMoScale;
                Time.fixedDeltaTime = 0.02f * Time.timeScale;
                slowMoApplied = true;
            }
        }

        private void ClearSlowMo()
        {
            if (!slowMoApplied) return;
            Time.timeScale = previousTimeScale;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            slowMoApplied = false;
        }

        private void EndChain()
        {
            chainActive = false;
            remainingChains = 0;
            nextAvailableTime = Time.time + ConfigCooldown;
        }

        private void OnDisable()
        {
            if (slowMoApplied) ClearSlowMo();
        }

        private void OnDestroy()
        {
            if (slowMoApplied) ClearSlowMo();
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

        private void ApplyBounceImpulse(Vector3 dashDirection)
        {
            BounceDefinition config = CurrentBounce;
            if (config == null || config.force <= 0f)
                return;

            Vector3 dir = ResolveBounceDirection(dashDirection, config);
            if (dir.sqrMagnitude <= 1e-4f)
                return;

            Vector3 momentum = dir.normalized * config.force;

            if (fpsMovement != null)
            {
                fpsMovement.ApplyExternalMomentum(momentum);
            }
            else if (characterController != null)
            {
                characterController.Move(momentum * Time.deltaTime);
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

