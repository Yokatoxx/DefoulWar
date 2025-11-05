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
        
        [Header("Protection contre le dash")]
        [Tooltip("Les ennemis électriques résistent au dash et ne meurent pas")]
        [SerializeField] private bool resistToDash = true;

        private EnemyHealth health;
        private static readonly Collider[] DischargeBuffer = new Collider[32];
        private float lastDischargeTime = -999f;

     
        
        



       

       
        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            if (health != null)
            {
                health.OnDeath.AddListener(OnDeath);
            }
            

        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.OnDeath.RemoveListener(OnDeath);
            }
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

            int count = Physics.OverlapSphereNonAlloc(transform.position, electricDischargeRadius, DischargeBuffer);
            for (int i = 0; i < count; i++)
            {
                var col = DischargeBuffer[i];
                if (col == null) continue;
                var enemyHealth = col.GetComponent<EnemyHealth>();
                if (enemyHealth == null) enemyHealth = col.GetComponentInParent<EnemyHealth>();
                if (enemyHealth != null && enemyHealth != this.health && !enemyHealth.IsDead)
                {
                    enemyHealth.TakeDamage(new DamageInfo(electricDamage, "Electric", DamageType.Electric));
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
    }
}
