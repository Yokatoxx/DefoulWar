using System.Collections;
using UnityEngine;

namespace FPS
{
    // Dash ciblé vers un ennemi: slow-mo à l'impact pour enchaîner jusqu'à CountDash
    // Variables: CountDash, SlowMoTime, DistanceDash, Cooldown
    // Compat: stun si ennemi électrique croisé sur le trajet ou à l'impact; heal magique via DamageType.Dash déjà géré côté ennemis
    public class DashCible : MonoBehaviour
    {
        [Header("Ciblage")]
        [SerializeField, Tooltip("Caméra d'aim")] private Camera aimCamera;
        [SerializeField, Tooltip("LayerMask pour les ennemis")] private LayerMask enemyMask = ~0;
        [SerializeField, Tooltip("LayerMask pour les obstacles bloquants")] private LayerMask obstacleMask = ~0;
        [SerializeField, Tooltip("Angle max depuis le centre pour considérer une cible (degrés)")] private float maxAimAngle = 30f;

        [Header("Dash Ciblé Settings")]
        [SerializeField, Tooltip("Nombre d'enchaînements possibles par séquence")] private int countDash = 3;
        [SerializeField, Tooltip("Durée du slow-mo après chaque hit (secondes temps réel)")] private float slowMoTime = 0.75f;
        [SerializeField, Tooltip("Échelle de temps pendant le slow-mo (0-1)")] private float slowMoScale = 0.2f;
        [SerializeField, Tooltip("Distance maximale de ciblage / dash (m)")] private float distanceDash = 25f;
        [SerializeField, Tooltip("Cooldown entre séquences (secondes)")] private float cooldown = 1.5f;
        [SerializeField, Tooltip("Dégâts du dash (assez haut pour tuer)")] private float dashDamage = 9999f;

        [Header("Déplacement")]
        [SerializeField, Tooltip("Durée du déplacement vers la cible (secondes, en temps réel)")] private float dashTravelTime = 0.08f;
        [SerializeField, Tooltip("Rayon du capsule/sphere cast pour la détection")] private float capsuleRadius = 0.4f;
        [SerializeField, Tooltip("Distance d'arrêt avant le centre de l'ennemi")] private float stopOffset = 1.0f;

        [Header("Input")]
        [SerializeField, Tooltip("Touche pour lancer/enchaîner le dash ciblé")] private KeyCode activationKey = KeyCode.Q;

        [Header("Références Joueur")]
        [SerializeField] private FPSPlayerController playerController;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private FPSMovement fpsMovement;

        // Runtime
        private bool isDashing;
        private bool chainActive;
        private int remainingChains;
        private float nextAvailableTime;
        private float slowMoEndUnscaled;
        private bool slowMoApplied;
        private float previousTimeScale = 1f;

        // Buffers réutilisés pour éviter GC
        private static readonly RaycastHit[] HitBuffer = new RaycastHit[32];
        private static readonly Collider[] OverlapBuffer = new Collider[64];

        // Caches
        private Transform camTransform;
        private PlayerStunAutoFire stunComp;

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
            camTransform = aimCamera != null ? aimCamera.transform : null;
            stunComp = GetComponent<PlayerStunAutoFire>();
        }

        private void Update()
        {
            // Fin de fenêtre slow-mo
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

            // Nouvelle séquence
            if (!chainActive)
            {
                if (Time.time < nextAvailableTime) return; // cooldown
                remainingChains = Mathf.Max(1, countDash);
                chainActive = true;
            }
            else
            {
                // Enchaînement: nécessite slow-mo actif + charges restantes
                if (!slowMoApplied || remainingChains <= 0)
                {
                    return;
                }
            }

            // Choisir la cible
            var target = AcquireTarget();
            if (target == null)
            {
                if (chainActive)
                {
                    ClearSlowMo();
                    EndChain();
                }
                return;
            }

            // Consommer 1 dash
            remainingChains = Mathf.Max(0, remainingChains - 1);
            StartCoroutine(DoTargetDash(target));
        }

        private EnemyHealth AcquireTarget()
        {
            if (aimCamera == null) return null;

            // 1) Raycast centre écran
            Ray ray = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            if (Physics.Raycast(ray, out RaycastHit hit, distanceDash, enemyMask, QueryTriggerInteraction.Ignore))
            {
                var eh = hit.collider.GetComponentInParent<EnemyHealth>() ?? hit.collider.GetComponent<EnemyHealth>();
                if (eh != null && !eh.IsDead)
                {
                    if (!IsObstructed(aimCamera.transform.position, eh.transform.position))
                        return eh;
                }
            }

            // 2) Overlap sphere pour trouver rapidement les candidats autour de la caméra
            if (camTransform == null) camTransform = aimCamera.transform;
            int count = Physics.OverlapSphereNonAlloc(camTransform.position, distanceDash, OverlapBuffer, enemyMask, QueryTriggerInteraction.Ignore);
            EnemyHealth best = null;
            float bestScore = float.MaxValue;
            Vector3 camPos = camTransform.position;
            Vector3 camFwd = camTransform.forward;

            for (int i = 0; i < count; i++)
            {
                var col = OverlapBuffer[i];
                if (col == null) continue;
                var eh = col.GetComponentInParent<EnemyHealth>() ?? col.GetComponent<EnemyHealth>();
                if (eh == null || eh.IsDead) continue;

                Vector3 to = eh.transform.position - camPos;
                float dist = to.magnitude;
                if (dist < 0.001f) continue;
                Vector3 dir = to / dist;
                float cos = Vector3.Dot(camFwd, dir);
                if (cos <= 0f) continue;
                float angle = Mathf.Acos(Mathf.Clamp(cos, -1f, 1f)) * Mathf.Rad2Deg;
                if (angle > maxAimAngle) continue;
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
            return Physics.Raycast(from, dir.normalized, dist - 0.1f, obstacleMask, QueryTriggerInteraction.Ignore);
        }

        private IEnumerator DoTargetDash(EnemyHealth target)
        {
            isDashing = true;

            Vector3 start = transform.position;
            Vector3 targetPos = target.transform.position;
            Vector3 dirToTarget = (targetPos - start).normalized;
            float distToTarget = Vector3.Distance(start, targetPos);

            float stopDist = Mathf.Clamp(stopOffset, 0f, Mathf.Max(0f, distToTarget - 0.1f));
            Vector3 end = targetPos - dirToTarget * stopDist;

            // Obstacles: ajuster la destination si bloqué
            Vector3 top = start + Vector3.up * 1.5f;
            Vector3 bottom = start + Vector3.up * 0.2f;
            Vector3 moveDir = end - start;
            float moveLen = moveDir.magnitude;
            if (moveLen > 0.01f && Physics.CapsuleCast(top, bottom, capsuleRadius, moveDir.normalized, out RaycastHit hit, moveLen, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                end = hit.point - moveDir.normalized * 0.2f;
                moveDir = end - start;
                moveLen = moveDir.magnitude;
            }

            // Pré-détection d'un ennemi électrique sur la trajectoire
            if (moveLen > 0.01f && CheckFirstElectricOnPath(start, end, out var electricFirst, out var hitPoint))
            {
                // Se placer juste avant la rencontre
                Vector3 dest = hitPoint - moveDir.normalized * 0.2f;
                Vector3 delta = dest - transform.position;
                if (characterController != null)
                    characterController.Move(delta);
                else
                    transform.position = dest;

                ApplyStunFromElectric(electricFirst);
                isDashing = false;
                ClearSlowMo();
                EndChain();
                yield break;
            }

            if (fpsMovement != null)
            {
                fpsMovement.SetSpeedToMax();
            }

            // Déplacement court (unscaled)
            float t0 = Time.unscaledTime;
            float dur = Mathf.Max(0.01f, dashTravelTime);
            Vector3 prev = start;

            while (Time.unscaledTime - t0 < dur)
            {
                float u = (Time.unscaledTime - t0) / dur;
                Vector3 pos = Vector3.Lerp(start, end, u);

                Vector3 delta = pos - prev;
                if (characterController != null)
                    characterController.Move(delta);
                else
                    transform.position = pos;
                prev = pos;
                yield return null;
            }

            // Snap final
            Vector3 finalDelta = end - prev;
            if (characterController != null)
                characterController.Move(finalDelta);
            else
                transform.position = end;

            // Impact: électrique d'abord
            var electric = target.GetComponent<Ennemies.Effect.ElectricEnnemis>();
            if (electric != null)
            {
                ApplyStunFromElectric(electric);
                if (electric.ResistToDash)
                {
                    isDashing = false;
                    ClearSlowMo();
                    EndChain();
                    yield break;
                }
            }

            // Dégâts de dash
            var hitCol = target.GetComponentInChildren<Collider>();
            var dmg = new DamageInfo(amount: dashDamage, zoneName: "Dash", type: DamageType.Dash, hitPoint: target.transform.position, hitNormal: -dirToTarget, attacker: transform, hitCollider: hitCol);
            bool applied = target.TryApplyDamage(dmg);

            if (applied)
            {
                ApplyOrRefreshSlowMo();
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

        private void ApplyStunFromElectric(Ennemies.Effect.ElectricEnnemis electric)
        {
            if (electric == null) return;
            if (stunComp == null) stunComp = GetComponent<PlayerStunAutoFire>();
            if (stunComp == null) stunComp = gameObject.AddComponent<PlayerStunAutoFire>();

            if (electric.OverrideAutoFireInterval)
                stunComp.ApplyStun(electric.StunDuration, electric.StunAutoFireInterval);
            else
                stunComp.ApplyStun(electric.StunDuration);
        }

        private bool CheckFirstElectricOnPath(Vector3 from, Vector3 to, out Ennemies.Effect.ElectricEnnemis electric, out Vector3 point)
        {
            electric = null;
            point = Vector3.zero;
            Vector3 dir = to - from;
            float len = dir.magnitude;
            if (len < 0.001f) return false;

            int hits = Physics.SphereCastNonAlloc(from, capsuleRadius, dir.normalized, HitBuffer, len, enemyMask, QueryTriggerInteraction.Ignore);
            float bestDist = float.MaxValue;
            for (int i = 0; i < hits; i++)
            {
                var h = HitBuffer[i];
                if (h.collider == null) continue;
                var e = h.collider.GetComponentInParent<Ennemies.Effect.ElectricEnnemis>() ?? h.collider.GetComponent<Ennemies.Effect.ElectricEnnemis>();
                if (e != null && h.distance < bestDist)
                {
                    bestDist = h.distance;
                    electric = e;
                    point = h.point;
                }
            }
            return electric != null;
        }

        private void ApplyOrRefreshSlowMo()
        {
            slowMoEndUnscaled = Time.unscaledTime + Mathf.Max(0.01f, slowMoTime);
            if (!slowMoApplied)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = Mathf.Clamp(slowMoScale, 0.01f, 1f);
                Time.fixedDeltaTime = 0.02f * Time.timeScale; // garder la physique en phase
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
            nextAvailableTime = Time.time + Mathf.Max(0f, cooldown);
        }

        private void OnDisable()
        {
            if (slowMoApplied) ClearSlowMo();
        }

        private void OnDestroy()
        {
            if (slowMoApplied) ClearSlowMo();
        }

        // Exposer les propriétés principales
        public int CountDash { get => countDash; set => countDash = Mathf.Max(1, value); }
        public float SlowMoTime { get => slowMoTime; set => slowMoTime = Mathf.Max(0.01f, value); }
        public float DistanceDash { get => distanceDash; set => distanceDash = Mathf.Max(0.5f, value); }
        public float Cooldown { get => cooldown; set => cooldown = Mathf.Max(0f, value); }
    }
}