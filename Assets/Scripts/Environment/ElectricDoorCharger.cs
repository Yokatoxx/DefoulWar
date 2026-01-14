using UnityEngine;
using FPS;
using Ennemies.Effect;

namespace Environment
{
    /// <summary>
    /// Props dans le décor qui se charge quand un ennemi électrique subit des dégâts.
    /// Quand l'ennemi électrique meurt et que la charge est pleine, la porte s'ouvre.
    /// </summary>
    public class ElectricDoorCharger : MonoBehaviour
    {
        [Header("Target Enemy")]
        [Tooltip("L'ennemi électrique à surveiller. Si null, cherche le plus proche avec ElectricEnnemis.")]
        [SerializeField] private EnemyHealth targetEnemy;
        
        [Tooltip("Rayon de détection pour trouver un ennemi électrique automatiquement")]
        [SerializeField] private float detectionRadius = 20f;
        
        [Header("Charge Settings")]
        [Tooltip("Pourcentage de vie perdue par l'ennemi requis pour ouvrir la porte (0-1). 1 = doit perdre toute sa vie.")]
        [SerializeField, Range(0f, 1f)] private float chargeThreshold = 1f;
        
        [Header("Door Reference")]
        [Tooltip("La porte à ouvrir quand chargée et que l'ennemi meurt")]
        [SerializeField] private ElectricDoor targetDoor;
        
        [Header("Visual Feedback")]
        [Tooltip("Renderer pour afficher la couleur de charge")]
        [SerializeField] private Renderer chargeRenderer;
        [Tooltip("Couleur quand peu chargé")]
        [SerializeField] private Color lowChargeColor = Color.blue;
        [Tooltip("Couleur quand pleinement chargé")]
        [SerializeField] private Color fullChargeColor = Color.yellow;
        
        private bool isSubscribed;
        private float maxHealthCache;
        
        private void Start()
        {
            // Si pas d'ennemi assigné, chercher le plus proche avec ElectricEnnemis
            if (targetEnemy == null)
            {
                FindNearestElectricEnemy();
            }
            
            SubscribeToEnemy();
            UpdateVisualFeedback();
        }
        
        private void OnDestroy()
        {
            UnsubscribeFromEnemy();
        }
        
        private void FindNearestElectricEnemy()
        {
            float closestDist = float.MaxValue;
            EnemyHealth closest = null;
            
            foreach (var enemy in EnemyRegistry.Instance.GetAliveEnemies())
            {
                // Vérifier si c'est un ennemi électrique
                var electric = enemy.GetComponent<ElectricEnnemis>();
                if (electric == null) continue;
                
                float dist = Vector3.Distance(transform.position, enemy.transform.position);
                if (dist < detectionRadius && dist < closestDist)
                {
                    closestDist = dist;
                    closest = enemy;
                }
            }
            
            if (closest != null)
            {
                targetEnemy = closest;
            }
        }
        
        private void SubscribeToEnemy()
        {
            if (targetEnemy == null || isSubscribed) return;
            
            maxHealthCache = targetEnemy.MaxHealth;
            targetEnemy.OnDamageTaken.AddListener(OnEnemyDamageTaken);
            targetEnemy.OnDeath.AddListener(OnEnemyDeath);
            isSubscribed = true;
        }
        
        private void UnsubscribeFromEnemy()
        {
            if (targetEnemy == null || !isSubscribed) return;
            
            targetEnemy.OnDamageTaken.RemoveListener(OnEnemyDamageTaken);
            targetEnemy.OnDeath.RemoveListener(OnEnemyDeath);
            isSubscribed = false;
        }
        
        private void OnEnemyDamageTaken(float damage, string zone)
        {
            UpdateVisualFeedback();
        }
        
        private void OnEnemyDeath()
        {
            // Quand l'ennemi meurt, ouvrir la porte
            if (targetDoor != null)
            {
                targetDoor.Open();
            }
        }
        
        private void UpdateVisualFeedback()
        {
            if (chargeRenderer == null) return;
            
            float percent = ChargePercent;
            Color currentColor = Color.Lerp(lowChargeColor, fullChargeColor, percent);
            
            // Utiliser MaterialPropertyBlock pour éviter d'instancier le material
            MaterialPropertyBlock props = new MaterialPropertyBlock();
            chargeRenderer.GetPropertyBlock(props);
            props.SetColor("_Color", currentColor);
            props.SetColor("_EmissionColor", currentColor * 2f);
            chargeRenderer.SetPropertyBlock(props);
        }
        
        /// <summary>
        /// Charge basée sur la vie perdue de l'ennemi cible.
        /// </summary>
        public float ChargePercent
        {
            get
            {
                if (targetEnemy == null || maxHealthCache <= 0) return 0f;
                
                // Si l'ennemi est mort, charge = 100%
                if (targetEnemy.IsDead) return 1f;
                
                return 1f - (targetEnemy.CurrentHealth / maxHealthCache);
            }
        }
        
        /// <summary>
        /// Vrai si la charge a atteint le seuil requis.
        /// </summary>
        public bool IsFullyCharged => ChargePercent >= chargeThreshold;
        
        /// <summary>
        /// Assigne manuellement un ennemi électrique à surveiller.
        /// </summary>
        public void SetTargetEnemy(EnemyHealth enemy)
        {
            UnsubscribeFromEnemy();
            targetEnemy = enemy;
            SubscribeToEnemy();
            UpdateVisualFeedback();
        }
        
        private void OnDrawGizmosSelected()
        {
            // Afficher le rayon de détection
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);
            
            // Ligne vers l'ennemi cible
            if (targetEnemy != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, targetEnemy.transform.position);
            }
            
            // Ligne vers la porte
            if (targetDoor != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position, targetDoor.transform.position);
            }
        }
    }
}
