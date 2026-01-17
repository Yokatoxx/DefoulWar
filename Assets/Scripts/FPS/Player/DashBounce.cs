using System.Collections;
using UnityEngine;

namespace FPS
{
    /// <summary>
    /// Gère le rebond (bounce) après un dash réussi.
    /// Utilise BounceSettings pour la configuration via DashSettings.
    /// </summary>
    public class DashBounce : MonoBehaviour
    {
        private BounceSettings groundBounce;
        private BounceSettings airBounce;
        
        private FPSMovement fpsMovement;
        private Rigidbody rb;
        private Coroutine bounceCoroutine;
        private Vector3 lastDashDirection;
        
        public bool IsBouncing => bounceCoroutine != null;
        
        private void Awake()
        {
            fpsMovement = GetComponent<FPSMovement>();
            rb = GetComponent<Rigidbody>();
        }
        
        /// <summary>
        /// Configure le module avec les paramètres Bounce depuis DashSettings.
        /// </summary>
        public void Configure(BounceSettings ground, BounceSettings air)
        {
            groundBounce = ground;
            airBounce = air;
        }
        
        /// <summary>
        /// Retourne la configuration appropriée selon l'état du joueur.
        /// </summary>
        public BounceSettings GetCurrentBounce()
        {
            bool grounded = fpsMovement == null || fpsMovement.IsGrounded;
            return grounded ? (groundBounce ?? airBounce) : (airBounce ?? groundBounce);
        }
        
        /// <summary>
        /// Démarre l'impulsion de rebond.
        /// </summary>
        public void StartBounce(Vector3 dashDirection)
        {
            lastDashDirection = dashDirection;
            
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
        
        private IEnumerator ApplyBounceOverTime(Vector3 dashDirection, BounceSettings config)
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

            // Impulsion instantanée si pas de durée ou pas de courbe
            if (config.duration <= 0f || config.forceOverTime == null)
            {
                Vector3 instantImpulse = dir.normalized * config.force;
                ApplyMomentum(instantImpulse);
                bounceCoroutine = null;
                yield break;
            }

            // Application progressive avec la courbe
            float elapsed = 0f;
            float duration = config.duration;
            Vector3 direction = dir.normalized;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float curveValue = config.forceOverTime.Evaluate(t);
                Vector3 velocity = direction * (config.force * curveValue);
                
                ApplyMomentum(velocity);

                elapsed += Time.unscaledDeltaTime;
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

        private Vector3 ResolveBounceDirection(Vector3 fallbackDashDirection, BounceSettings config)
        {
            Vector3 dir;
            
            // Mode dynamique: utilise l'inverse de la direction du dash + composante verticale
            if (config.useDashDirectionAsBounce)
            {
                Vector3 inverseDash = -fallbackDashDirection.normalized;
                inverseDash.y = 0f;
                if (inverseDash.sqrMagnitude > 1e-4f)
                {
                    inverseDash = inverseDash.normalized;
                }
                else
                {
                    inverseDash = -transform.forward;
                }
                
                dir = inverseDash + Vector3.up * config.verticalComponent;
            }
            else
            {
                // Mode statique: direction configurée
                dir = config.directionIsLocal ? transform.TransformDirection(config.direction) : config.direction;
            }
            
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
