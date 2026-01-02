using System.Collections;
using UnityEngine;

namespace FPS
{
    /// <summary>
    /// Gère le rebond (bounce) après un dash réussi.
    /// Extrait de DashCible pour simplifier la logique.
    /// </summary>
    public class DashBounce : MonoBehaviour
    {
        [Header("Bounce Configurations")]
        [SerializeField] private BounceDefinition groundBounce;
        [SerializeField] private BounceDefinition airBounce;
        
        private FPSMovement fpsMovement;
        private Rigidbody rb;
        private Coroutine bounceCoroutine;
        
        public BounceDefinition GroundBounce => groundBounce;
        public BounceDefinition AirBounce => airBounce;
        
        private void Awake()
        {
            fpsMovement = GetComponent<FPSMovement>();
            rb = GetComponent<Rigidbody>();
        }
        
        /// <summary>
        /// Configure les définitions de rebond.
        /// </summary>
        public void Configure(BounceDefinition ground, BounceDefinition air)
        {
            groundBounce = ground;
            airBounce = air;
        }
        
        /// <summary>
        /// Retourne la configuration de rebond appropriée selon l'état du joueur.
        /// </summary>
        public BounceDefinition GetCurrentBounce()
        {
            bool grounded = fpsMovement == null || fpsMovement.IsGrounded;
            return grounded ? (groundBounce ?? airBounce) : (airBounce ?? groundBounce);
        }
        
        /// <summary>
        /// Démarre l'impulsion de rebond.
        /// </summary>
        public void StartBounce(Vector3 dashDirection)
        {
            if (bounceCoroutine != null)
            {
                StopCoroutine(bounceCoroutine);
            }
            bounceCoroutine = StartCoroutine(ApplyBounceOverTime(dashDirection, GetCurrentBounce()));
        }
        
        /// <summary>
        /// Arrête le rebond en cours.
        /// </summary>
        public void CancelBounce()
        {
            if (bounceCoroutine != null)
            {
                StopCoroutine(bounceCoroutine);
                bounceCoroutine = null;
            }
        }
        
        private IEnumerator ApplyBounceOverTime(Vector3 dashDirection, BounceDefinition config)
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

            // Application instantanée si pas de durée ou pas de courbe
            if (config.duration <= 0f || config.forceOverTime == null)
            {
                Vector3 instantImpulse = dir.normalized * config.force;
                ApplyMomentum(instantImpulse);
                bounceCoroutine = null;
                yield break;
            }

            // Application sur la durée avec la courbe
            float elapsed = 0f;
            float duration = config.duration;
            Vector3 direction = dir.normalized;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float curveValue = config.forceOverTime.Evaluate(t);
                Vector3 velocity = direction * (config.force * curveValue);
                
                ApplyMomentum(velocity);

                elapsed += Time.deltaTime;
                yield return null;
            }

            bounceCoroutine = null;
        }
        
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
        
        private void OnDisable()
        {
            CancelBounce();
        }
        
        private void OnDestroy()
        {
            CancelBounce();
        }
    }
}
