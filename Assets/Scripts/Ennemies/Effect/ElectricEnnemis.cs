using UnityEngine;
using FPS;

namespace Ennemies.Effect
{
    [RequireComponent(typeof(EnemyHealth))]
    public class ElectricEnnemis : MonoBehaviour
    {
        [Header("Effet appliqué au joueur si ce PNJ est touché par un dash")]
        [SerializeField] private float stunDuration = 2.5f;

        [Header("Auto-fire pendant le stun (override optionnel)")]
        [Tooltip("Si activé, remplace l'intervalle d'auto-fire du joueur pendant ce stun.")]
        [SerializeField] private bool overrideAutoFireInterval;
        [SerializeField, Min(0.01f)] private float stunAutoFireInterval = 0.12f;

        [Header("Dégâts électriques aux ennemis proches")]
        [Tooltip("Rayon de la décharge électrique quand l'ennemi est touché par un tir.")]
        [SerializeField] private float electricDischargeRadius = 5f;
        [Tooltip("Dégâts infligés aux ennemis dans le rayon de décharge.")]
        [SerializeField] private float electricDamage = 15f;
        [Tooltip("Effet visuel de décharge (optionnel).")]
        [SerializeField] private GameObject electricEffectPrefab;
        [Tooltip("Durée de l'effet visuel en secondes.")]
        [SerializeField] private float effectDuration = 0.5f;
        [Tooltip("Temps minimum entre deux décharges (en secondes).")]
        [SerializeField] private float dischargeCooldown = 0.2f;
        
        [Header("Power Scaling (basé sur les ennemis proches)")]
        [Tooltip("Tag des ennemis à compter pour le bonus de puissance")]
        [SerializeField] private string targetEnemyTag = "BasicEnemy";
        [Tooltip("Rayon de détection des ennemis pour le bonus")]
        [SerializeField] private float detectionRadius = 10f;
        [Tooltip("Nombre maximum d'ennemis pris en compte pour le bonus")]
        [SerializeField, Min(1)] private int maxEnemiesForBonus = 5;
        [Tooltip("Bonus de vitesse par ennemi détecté")]
        [SerializeField] private float speedBonusPerEnemy = 0.5f;
        [Tooltip("Multiplicateur de dégâts par ennemi (0.2 = +20% par ennemi)")]
        [SerializeField, Range(0f, 1f)] private float damageBonusPerEnemy = 0.2f;
        [Tooltip("Intervalle de mise à jour de la détection (en secondes)")]
        [SerializeField] private float detectionInterval = 0.5f;
        
        [Header("Protection contre le dash")]
        [Tooltip("Les ennemis électriques résistent au dash et ne meurent pas")]
        [SerializeField] private bool resistToDash = true;

        [Header("Ralentissement du joueur")]
        [Tooltip("Durée du ralentissement appliqué au joueur lors d'un contact")]
        [SerializeField] private float slowDuration = 2f;
        [Tooltip("Multiplicateur de vitesse pendant le ralentissement (0.5 = 50% de la vitesse)")]
        [SerializeField, Range(0.1f, 1f)] private float slowAmount = 0.5f;
        [Tooltip("Appliquer le ralentissement lors des attaques")]
        [SerializeField] private bool applySlowOnAttack = true;

        private EnemyHealth health;
        private static readonly Collider[] DischargeBuffer = new Collider[32];
        private float lastDischargeTime = -999f;
        
        // Power scaling system
        private UnityEngine.AI.NavMeshAgent navAgent;
        private float baseSpeed = -1f;
        private int currentNearbyEnemyCount = 0;
        private float nextDetectionTime = 0f;
        private static readonly Collider[] DetectionBuffer = new Collider[32];

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            if (health != null)
            {
                health.OnDeath.AddListener(OnDeath);
            }
            
            navAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (navAgent != null)
            {
                baseSpeed = navAgent.speed;
            }
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.OnDeath.RemoveListener(OnDeath);
            }
        }
        
        private void Update()
        {
            if (health != null && health.IsDead) return;
            
            if (Time.time >= nextDetectionTime)
            {
                UpdateNearbyEnemyCount();
                UpdateSpeedBonus();
                nextDetectionTime = Time.time + detectionInterval;
            }
        }
        
        private void UpdateNearbyEnemyCount()
        {
            if (detectionRadius <= 0f) 
            {
                currentNearbyEnemyCount = 0;
                return;
            }
            
            int count = 0;
            int detected = Physics.OverlapSphereNonAlloc(transform.position, detectionRadius, DetectionBuffer);
            
            for (int i = 0; i < detected && count < maxEnemiesForBonus; i++)
            {
                var col = DetectionBuffer[i];
                if (col == null) continue;
                
                // Vérifier le tag
                if (!col.CompareTag(targetEnemyTag)) continue;
                
                // Vérifier que c'est un ennemi vivant
                var enemyHealth = col.GetComponent<EnemyHealth>();
                if (enemyHealth == null) enemyHealth = col.GetComponentInParent<EnemyHealth>();
                
                if (enemyHealth != null && enemyHealth != this.health && !enemyHealth.IsDead)
                {
                    count++;
                }
            }
            
            currentNearbyEnemyCount = count;
        }
        
        private void UpdateSpeedBonus()
        {
            if (navAgent == null || baseSpeed < 0f) return;
            
            float newSpeed = baseSpeed + (currentNearbyEnemyCount * speedBonusPerEnemy);
            navAgent.speed = newSpeed;
        }

        private void OnDeath()
        {

            TriggerElectricDischarge();
        }
        
        public void TriggerElectricDischarge()
        {
            if (electricDischargeRadius <= 0f || electricDamage <= 0f) return;
            if (Time.time - lastDischargeTime < dischargeCooldown)
            {
                return;
            }
            lastDischargeTime = Time.time;
            
            // Calculer les dégâts avec le bonus
            float damageMultiplier = 1f + (currentNearbyEnemyCount * damageBonusPerEnemy);
            float scaledDamage = electricDamage * damageMultiplier;

            int count = Physics.OverlapSphereNonAlloc(transform.position, electricDischargeRadius, DischargeBuffer);
            for (int i = 0; i < count; i++)
            {
                var col = DischargeBuffer[i];
                if (col == null) continue;
                var enemyHealth = col.GetComponent<EnemyHealth>();
                if (enemyHealth == null) enemyHealth = col.GetComponentInParent<EnemyHealth>();
                if (enemyHealth != null && enemyHealth != this.health && !enemyHealth.IsDead)
                {
                    enemyHealth.TakeDamage(new DamageInfo(scaledDamage, "Electric", DamageType.Electric));
                    if (electricEffectPrefab != null)
                    {
                        CreateElectricArc(enemyHealth.transform.position);
                    }
                }
            }
            if (electricEffectPrefab != null)
            {
                GameObject effect = Instantiate(electricEffectPrefab, transform.position, Quaternion.identity);
                Destroy(effect, effectDuration);
            }
        }

        private void CreateElectricArc(Vector3 targetPosition)
        {
            if (electricEffectPrefab == null) return;

            Vector3 midPoint = (transform.position + targetPosition) / 2f;
            Vector3 direction = targetPosition - transform.position;
            
            GameObject arc = Instantiate(electricEffectPrefab, midPoint, Quaternion.LookRotation(direction));
            
            float distance = direction.magnitude;
            arc.transform.localScale = new Vector3(1f, 1f, distance);
            
            Destroy(arc, effectDuration);
        }

        public float StunDuration => stunDuration;
        public bool OverrideAutoFireInterval => overrideAutoFireInterval;
        public float StunAutoFireInterval => stunAutoFireInterval;
        public float ElectricDischargeRadius => electricDischargeRadius;
        public float ElectricDamage => electricDamage;
        public bool ResistToDash => resistToDash;
        public float SlowDuration => slowDuration;
        public float SlowAmount => slowAmount;
        public bool ApplySlowOnAttack => applySlowOnAttack;
        
        /// <summary>
        /// Applique l'effet de ralentissement électrique au joueur.
        /// </summary>
        public void ApplySlowToPlayer(Transform playerTransform)
        {
            if (playerTransform == null) return;
            
            var movement = playerTransform.GetComponent<FPSMovement>();
            if (movement != null)
            {
                movement.ApplySlow(slowDuration, slowAmount);
                Debug.Log($"[ElectricEnnemis] Ralentissement électrique appliqué au joueur: {slowAmount * 100}% pendant {slowDuration}s");
            }
        }
        
        // Propriétés publiques pour debug/inspection
        public int CurrentNearbyEnemyCount => currentNearbyEnemyCount;
        public float CurrentSpeed => navAgent != null ? navAgent.speed : baseSpeed;
        public float CurrentDamageMultiplier => 1f + (currentNearbyEnemyCount * damageBonusPerEnemy);
        
        private void OnDrawGizmosSelected()
        {
            if (detectionRadius > 0f)
            {
                Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
                Gizmos.DrawWireSphere(transform.position, detectionRadius);
            }
        }
    }
}
