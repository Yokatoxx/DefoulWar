using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace FPS
{
    /// <summary>
    /// Gère le knockback physique des ennemis lors d'un dash.
    /// Utilise un Rigidbody pour la physique et réactive le NavMeshAgent après le knockback.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class EnemyKnockback : MonoBehaviour
    {
        [Header("Knockback Settings")]
        [Tooltip("Si true, cet ennemi résiste au knockback (ne sera pas repoussé)")]
        [SerializeField] private bool resistToKnockback = false;
        
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
        
        /// <summary>
        /// Indique si cet ennemi résiste au knockback
        /// </summary>
        public bool ResistToKnockback => resistToKnockback;
        
        /// <summary>
        /// Indique si un knockback est actuellement en cours
        /// </summary>
        public bool IsKnockbackActive => isKnockbackActive;
        
        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            agent = GetComponent<NavMeshAgent>();
            visualFeedback = GetComponent<EnemyVisualFeedback>();
            enemyHealth = GetComponent<EnemyHealth>();
            
            // Configuration initiale du Rigidbody
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                // Bloquer la rotation pour éviter que l'ennemi tourne pendant le knockback
                rb.constraints = RigidbodyConstraints.FreezeRotation;
            }
        }
        
        /// <summary>
        /// Applique un knockback à l'ennemi dans la direction spécifiée.
        /// </summary>
        /// <param name="direction">Direction du knockback (normalisée)</param>
        /// <param name="force">Force du knockback</param>
        /// <param name="duration">Durée du knockback avant de réactiver l'IA</param>
        /// <param name="affectsYAxis">Si true, applique aussi la force sur l'axe Y</param>
        public void ApplyKnockback(Vector3 direction, float force, float duration, bool affectsYAxis = false)
        {
            // Vérifier la résistance au knockback
            if (resistToKnockback) return;
            
            // Vérifier si l'ennemi est mort
            if (enemyHealth != null && enemyHealth.IsDead) return;
            
            // Vérifier les composants nécessaires
            if (rb == null) return;
            
            // Si un knockback est déjà en cours, l'interrompre
            if (knockbackCoroutine != null)
            {
                StopCoroutine(knockbackCoroutine);
            }
            
            knockbackCoroutine = StartCoroutine(KnockbackCoroutine(direction, force, duration, affectsYAxis));
        }
        
        private IEnumerator KnockbackCoroutine(Vector3 direction, float force, float duration, bool affectsYAxis)
        {
            isKnockbackActive = true;
            
            // Normaliser la direction
            direction = direction.normalized;
            
            // Optionnellement ignorer l'axe Y
            if (!affectsYAxis)
            {
                direction.y = 0f;
                direction = direction.normalized;
            }
            
            // Désactiver le NavMeshAgent
            if (agent != null && agent.enabled)
            {
                agent.isStopped = true;
                agent.enabled = false;
            }
            
            // Activer la physique du Rigidbody
            rb.isKinematic = false;
            
            // Appliquer l'impulsion de knockback
            rb.linearVelocity = Vector3.zero; // Reset velocity
            rb.AddForce(direction * force, ForceMode.VelocityChange);
            
            // Jouer les feedbacks
            PlayImpactFeedback();
            
            // Attendre la durée du knockback (utiliser unscaledDeltaTime pour ignorer le slow-mo)
            float elapsed = 0f;
            while (elapsed < duration)
            {
                // Vérifier si l'ennemi est mort pendant le knockback
                if (enemyHealth != null && enemyHealth.IsDead)
                {
                    isKnockbackActive = false;
                    yield break;
                }
                
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            
            // Arrêter le mouvement
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            
            // Réactiver le Rigidbody en mode kinématique
            rb.isKinematic = true;
            
            // Réactiver le NavMeshAgent avec Warp pour éviter les problèmes de position
            if (agent != null)
            {
                // Trouver la position valide la plus proche sur le NavMesh
                if (NavMesh.SamplePosition(rb.position, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                {
                    agent.enabled = true;
                    agent.Warp(hit.position);
                    agent.isStopped = false;
                }
                else
                {
                    // Si pas de position NavMesh trouvée, activer quand même l'agent
                    agent.enabled = true;
                    agent.isStopped = false;
                }
            }
            
            isKnockbackActive = false;
            knockbackCoroutine = null;
        }
        
        private void PlayImpactFeedback()
        {
            // Feedback visuel (flash)
            if (visualFeedback != null)
            {
                visualFeedback.ShowHitFeedback();
            }
            
            // Particule d'impact
            if (impactParticlePrefab != null)
            {
                Vector3 spawnPos = transform.position + particleOffset;
                GameObject particle = Instantiate(impactParticlePrefab, spawnPos, Quaternion.identity);
                
                // Auto-destruction de la particule
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
            
            // Son d'impact
            if (impactSound != null)
            {
                AudioSource.PlayClipAtPoint(impactSound, transform.position, impactVolume);
            }
        }
        
        /// <summary>
        /// Force l'arrêt du knockback en cours (utile si l'ennemi meurt)
        /// </summary>
        public void CancelKnockback()
        {
            if (knockbackCoroutine != null)
            {
                StopCoroutine(knockbackCoroutine);
                knockbackCoroutine = null;
            }
            
            isKnockbackActive = false;
            
            // Remettre le Rigidbody en mode kinématique
            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.isKinematic = true;
            }
        }
        
        private void OnDestroy()
        {
            CancelKnockback();
        }
    }
}

