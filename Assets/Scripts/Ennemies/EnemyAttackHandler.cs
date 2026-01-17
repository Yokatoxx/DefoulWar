using UnityEngine;
using FPS;
using Ennemies.Settings;
using Ennemies.Effect;

namespace Ennemies
{
    /// <summary>
    /// Gère les attaques d'un ennemi (melee ou ranged).
    /// </summary>
    public class EnemyAttackHandler : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("Point d'origine des tirs (pour les attaques à distance)")]
        [SerializeField] private Transform shootPoint;

        [Header("Hitscan FX (optionnel)")]
        [Tooltip("Composant pour les effets visuels hitscan")]
        [SerializeField] private MagicEnemyHitScan hitscanFX;

        [Header("Shield Attack Animation (optionnel)")]
        [Tooltip("Composant d'animation du shield pour les attaques melee")]
        [SerializeField] private ShieldAttackAnimation shieldAttackAnimation;

        [Header("Layers")]
        [Tooltip("Layer du joueur pour les raycasts")]
        [SerializeField] private LayerMask playerLayer;

        private EnemyBehaviorSettings settings;
        private Transform player;
        private float lastAttackTime = -999f;

        // Animator pour les animations futures
        // private Animator animator;

        /// <summary>
        /// Initialise le handler avec les settings et la référence au joueur.
        /// </summary>
        public void Initialize(EnemyBehaviorSettings settings, Transform player)
        {
            this.settings = settings;
            this.player = player;

            // animator = GetComponent<Animator>();

            if (shootPoint == null)
            {
                shootPoint = transform;
            }

            // Recherche automatique du ShieldAttackAnimation s'il n'est pas assigné
            if (shieldAttackAnimation == null)
            {
                shieldAttackAnimation = GetComponentInChildren<ShieldAttackAnimation>();
            }

            // Connecter l'événement d'impact du shield aux dégâts
            if (shieldAttackAnimation != null)
            {
                shieldAttackAnimation.OnImpactHit.RemoveListener(ApplyMeleeDamage);
                shieldAttackAnimation.OnImpactHit.AddListener(ApplyMeleeDamage);
            }
        }

        // Position cible override (pour système de charge sniper)
        private Vector3? targetPositionOverride = null;

        /// <summary>
        /// Tente d'effectuer une attaque si le cooldown est terminé.
        /// </summary>
        /// <returns>True si l'attaque a été effectuée</returns>
        public bool TryAttack()
        {
            return TryAttackInternal();
        }

        /// <summary>
        /// Tente d'effectuer une attaque vers une position spécifique (pour système sniper).
        /// </summary>
        public bool TryAttackAtPosition(Vector3 targetPosition)
        {
            targetPositionOverride = targetPosition;
            bool result = TryAttackInternal();
            targetPositionOverride = null;
            return result;
        }

        private bool TryAttackInternal()
        {
            if (settings == null || player == null) return false;

            if (Time.time < lastAttackTime + settings.attackCooldown)
            {
                return false;
            }

            lastAttackTime = Time.time;

            switch (settings.attackType)
            {
                case AttackType.Melee:
                    PerformMeleeAttack();
                    break;
                case AttackType.Ranged:
                    PerformRangedAttack();
                    break;
            }

            return true;
        }

        private void PerformMeleeAttack()
        {
            // Si le shield a une animation, les dégâts sont appliqués lors de l'impact
            if (shieldAttackAnimation != null)
            {
                shieldAttackAnimation.PlayAttackAnimation();
                // Les dégâts seront infligés via l'événement OnImpactHit
                return;
            }

            // Sans animation, appliquer les dégâts immédiatement
            ApplyMeleeDamage();
        }

        /// <summary>
        /// Applique les dégâts melee au joueur. Appelé directement ou via l'événement OnImpactHit du shield.
        /// </summary>
        public void ApplyMeleeDamage()
        {
            if (player == null || settings == null) return;

            float distance = Vector3.Distance(transform.position, player.position);
            
            if (distance <= settings.attackRange)
            {
                PlayerHealth playerHealth = player.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(settings.attackDamage);
                    Debug.Log($"[EnemyAttack] Melee attack: {settings.attackDamage} damage!");
                }
            }

            // Notifier l'indicateur de hit
            var indicator = FindFirstObjectByType<EnemyScreenDetector>();
            if (indicator != null)
            {
                indicator.RegisterHit(transform, settings.attackDamage);
            }
        }

        private void PerformRangedAttack()
        {
            if (settings.isHitscan)
            {
                PerformHitscanAttack();
            }
            else
            {
                PerformProjectileAttack();
            }
        }

        private void PerformHitscanAttack()
        {
            Vector3 origin = shootPoint.position;

            // Utiliser la position override (système sniper) ou viser le joueur
            Vector3 targetPoint;
            if (targetPositionOverride.HasValue)
            {
                targetPoint = targetPositionOverride.Value;
            }
            else
            {
                targetPoint = player.position + Vector3.up * 1f;
                Collider playerCollider = player.GetComponentInChildren<Collider>();
                if (playerCollider != null)
                {
                    targetPoint = playerCollider.bounds.center;
                }
            }

            Vector3 direction = (targetPoint - origin).normalized;
            float distance = Vector3.Distance(origin, targetPoint);
            Vector3 hitPoint = targetPoint;
            bool hitPlayer = false;

            // Raycast vers le joueur - on vérifie tous les hits
            RaycastHit[] hits = Physics.RaycastAll(origin, direction, distance + 1f);
            
            // Trier par distance
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit hit in hits)
            {
                // Ignorer l'ennemi lui-même
                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                    continue;

                hitPoint = hit.point;

                // Vérifier si on a touché le joueur
                PlayerHealth playerHealth = hit.collider.GetComponent<PlayerHealth>();
                if (playerHealth == null)
                {
                    playerHealth = hit.collider.GetComponentInParent<PlayerHealth>();
                }

                if (playerHealth != null)
                {
                    playerHealth.TakeDamage(settings.attackDamage);
                    hitPlayer = true;
                    
                    // Notifier l'indicateur de hit
                    var indicator = FindFirstObjectByType<EnemyScreenDetector>();
                    if (indicator != null)
                    {
                        indicator.RegisterHit(transform, settings.attackDamage);
                    }
                    break;
                }
                else
                {
                    // On a touché un obstacle avant le joueur, arrêter
                    break;
                }
            }

            // Effet visuel hitscan FX
            if (hitscanFX != null)
            {
                hitscanFX.FireTo(hitPoint);
            }

            // Trail pour hitscan
            if (settings.bulletTrailPrefab != null)
            {
                SpawnHitscanTrail(origin, hitPoint);
            }
        }

        private void SpawnHitscanTrail(Vector3 start, Vector3 end)
        {
            TrailRenderer trail = Instantiate(settings.bulletTrailPrefab, start, Quaternion.identity);
            StartCoroutine(AnimateHitscanTrail(trail, start, end));
        }

        private System.Collections.IEnumerator AnimateHitscanTrail(TrailRenderer trail, Vector3 start, Vector3 end)
        {
            float duration = settings.trailDuration;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                trail.transform.position = Vector3.Lerp(start, end, t);
                yield return null;
            }

            trail.transform.position = end;
            
            // Attendre que le trail disparaisse
            yield return new WaitForSeconds(trail.time);
            
            if (trail != null)
            {
                Destroy(trail.gameObject);
            }
        }

        private void PerformProjectileAttack()
        {
            if (settings.bulletPrefab == null)
            {
                Debug.LogWarning("[EnemyAttack] Bullet prefab not assigned!");
                return;
            }

            Vector3 origin = shootPoint.position;
            Vector3 targetPoint = player.position;
            
            // Viser le centre du joueur
            Collider playerCollider = player.GetComponentInChildren<Collider>();
            if (playerCollider != null)
            {
                targetPoint = playerCollider.bounds.center;
            }

            Vector3 direction = (targetPoint - origin).normalized;

            // Créer le projectile
            GameObject bullet = Instantiate(settings.bulletPrefab, origin, Quaternion.LookRotation(direction));
            
            // Ajouter le trail si configuré
            if (settings.bulletTrailPrefab != null)
            {
                TrailRenderer trail = Instantiate(settings.bulletTrailPrefab, bullet.transform);
                trail.transform.localPosition = Vector3.zero;
            }
            
            Rigidbody rb = bullet.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(direction * settings.bulletSpeed, ForceMode.Impulse);
            }

            // Détruire après un certain temps
            Destroy(bullet, settings.bulletLifetime);
        }

        /// <summary>
        /// Définit le point de tir.
        /// </summary>
        public void SetShootPoint(Transform point)
        {
            shootPoint = point;
        }

        private void OnDrawGizmosSelected()
        {
            if (shootPoint != null && settings != null)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(shootPoint.position, 0.1f);
                
                if (settings.attackType == AttackType.Ranged)
                {
                    Gizmos.DrawLine(shootPoint.position, shootPoint.position + shootPoint.forward * settings.attackRange);
                }
            }
        }
    }
}

