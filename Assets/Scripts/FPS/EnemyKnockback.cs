using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace FPS
{
    /// <summary>
    /// Gère le knockback physique des ennemis lors d'un dash.
    /// Utilise un Rigidbody pour la physique et réactive le NavMeshAgent après le knockback.
    /// Supporte l'effet domino (repousse les ennemis sur le passage).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class EnemyKnockback : MonoBehaviour
    {
        [Header("Knockback Settings")]
        [Tooltip("Si true, cet ennemi résiste au knockback (ne sera pas repoussé)")]
        [SerializeField] private bool resistToKnockback = false;
        
        [Header("Effet Domino")]
        [Tooltip("Si true, cet ennemi peut repousser d'autres ennemis quand il est projeté")]
        [SerializeField] private bool canPushOtherEnemies = true;
        
        [Tooltip("Force transmise aux autres ennemis (multiplicateur de la force reçue)")]
        [Range(0.1f, 1f)]
        [SerializeField] private float dominoForceMultiplier = 0.6f;
        
        [Tooltip("Rayon de détection des autres ennemis pour l'effet domino")]
        [SerializeField] private float dominoPushRadius = 1.2f;
        
        [Tooltip("Layer des ennemis pour la détection domino")]
        [SerializeField] private LayerMask enemyLayer;
        
        [Header("Visual Feedback")]
        [Tooltip("Particule d'impact à instancier lors du knockback")]
        [SerializeField] private GameObject impactParticlePrefab;
        
        [Tooltip("Offset de position pour la particule d'impact")]
        [SerializeField] private Vector3 particleOffset = Vector3.up * 0.5f;
        
        [Header("Audio Feedback")]
        [Tooltip("Son d'impact à jouer lors du knockback")]
        [SerializeField] private AudioClip impactSound;
        
        [Tooltip("Volume du son d'impact")]
        [Range(0f, 1f)]
        [SerializeField] private float impactVolume = 0.8f;
        
        private Rigidbody rb;
        private NavMeshAgent agent;
        private EnemyVisualFeedback visualFeedback;
        private EnemyHealth enemyHealth;
        private bool isKnockbackActive;
        private Coroutine knockbackCoroutine;
        
        // Pour éviter les boucles infinies de knockback domino
        private float lastDominoTime;
        private const float DOMINO_COOLDOWN = 0.3f;
        
        private static readonly Collider[] DominoBuffer = new Collider[8];
        
        public bool ResistToKnockback => resistToKnockback;
        public bool IsKnockbackActive => isKnockbackActive;
        
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            agent = GetComponent<NavMeshAgent>();
            visualFeedback = GetComponent<EnemyVisualFeedback>();
            enemyHealth = GetComponent<EnemyHealth>();
            
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                rb.constraints = RigidbodyConstraints.FreezeRotation;
            }
        }
        
        /// <summary>
        /// Applique un knockback à l'ennemi dans la direction spécifiée.
        /// </summary>
        public void ApplyKnockback(Vector3 direction, float force, float duration, bool affectsYAxis = false)
        {
            ApplyKnockbackInternal(direction, force, duration, affectsYAxis, true);
        }
        
        /// <summary>
        /// Applique un knockback domino (avec force réduite, pas de propagation).
        /// </summary>
        public void ApplyDominoKnockback(Vector3 direction, float force, float duration, bool affectsYAxis = false)
        {
            // Éviter les boucles de knockback domino
            if (Time.unscaledTime - lastDominoTime < DOMINO_COOLDOWN) return;
            
            ApplyKnockbackInternal(direction, force, duration, affectsYAxis, false);
        }
        
        private void ApplyKnockbackInternal(Vector3 direction, float force, float duration, bool affectsYAxis, bool canTriggerDomino)
        {
            if (resistToKnockback) return;
            if (enemyHealth != null && enemyHealth.IsDead) return;
            if (rb == null) return;
            
            if (knockbackCoroutine != null)
            {
                StopCoroutine(knockbackCoroutine);
            }
            
            knockbackCoroutine = StartCoroutine(KnockbackCoroutine(direction, force, duration, affectsYAxis, canTriggerDomino));
        }
        
        private IEnumerator KnockbackCoroutine(Vector3 direction, float force, float duration, bool affectsYAxis, bool canTriggerDomino)
        {
            isKnockbackActive = true;
            
            direction = direction.normalized;
            
            if (!affectsYAxis)
            {
                direction.y = 0f;
                direction = direction.normalized;
            }
            
            if (agent != null && agent.enabled)
            {
                agent.isStopped = true;
                agent.enabled = false;
            }
            
            rb.isKinematic = false;
            rb.linearVelocity = Vector3.zero;
            rb.AddForce(direction * force, ForceMode.VelocityChange);
            
            PlayImpactFeedback();
            
            // Effet domino : repousser les ennemis proches
            if (canTriggerDomino && canPushOtherEnemies)
            {
                TryPushNearbyEnemies(direction, force, duration);
            }
            
            float elapsed = 0f;
            while (elapsed < duration)
            {
                if (enemyHealth != null && enemyHealth.IsDead)
                {
                    isKnockbackActive = false;
                    yield break;
                }
                
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
            
            if (agent != null)
            {
                if (NavMesh.SamplePosition(rb.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                {
                    agent.enabled = true;
                    agent.Warp(hit.position);
                    agent.isStopped = false;
                }
                else
                {
                    agent.enabled = true;
                    agent.isStopped = false;
                }
            }
            
            isKnockbackActive = false;
            knockbackCoroutine = null;
        }
        
        private void TryPushNearbyEnemies(Vector3 knockbackDirection, float originalForce, float duration)
        {
            lastDominoTime = Time.unscaledTime;
            
            int count = Physics.OverlapSphereNonAlloc(
                transform.position, 
                dominoPushRadius, 
                DominoBuffer, 
                enemyLayer, 
                QueryTriggerInteraction.Ignore
            );
            
            for (int i = 0; i < count; i++)
            {
                var col = DominoBuffer[i];
                if (col == null || col.gameObject == gameObject) continue;
                
                var otherKnockback = col.GetComponentInParent<EnemyKnockback>();
                if (otherKnockback == null) continue;
                if (otherKnockback.ResistToKnockback) continue;
                if (otherKnockback.IsKnockbackActive) continue;
                
                // Direction depuis cet ennemi vers l'autre
                Vector3 pushDir = (col.transform.position - transform.position).normalized;
                if (pushDir.sqrMagnitude < 1e-4f) pushDir = knockbackDirection;
                
                float dominoForce = originalForce * dominoForceMultiplier;
                float dominoDuration = duration * 0.7f;
                
                otherKnockback.ApplyDominoKnockback(pushDir, dominoForce, dominoDuration, false);
            }
        }
        
        private void PlayImpactFeedback()
        {
            if (visualFeedback != null)
            {
                visualFeedback.ShowHitFeedback();
            }
            
            if (impactParticlePrefab != null)
            {
                Vector3 spawnPos = transform.position + particleOffset;
                GameObject particle = Instantiate(impactParticlePrefab, spawnPos, Quaternion.identity);
                
                var ps = particle.GetComponent<ParticleSystem>();
                if (ps != null)
                {
                    Destroy(particle, ps.main.duration + ps.main.startLifetime.constantMax);
                }
                else
                {
                    Destroy(particle, 2f);
                }
            }
            
            if (impactSound != null)
            {
                AudioSource.PlayClipAtPoint(impactSound, transform.position, impactVolume);
            }
        }
        
        public void CancelKnockback()
        {
            if (knockbackCoroutine != null)
            {
                StopCoroutine(knockbackCoroutine);
                knockbackCoroutine = null;
            }
            
            isKnockbackActive = false;
            
            if (rb != null)
            {
                if (!rb.isKinematic)
                {
                    rb.linearVelocity = Vector3.zero;
                }
                rb.isKinematic = true;
            }
        }
        
        private void OnDestroy()
        {
            CancelKnockback();
        }
    }
}
