using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Proto3GD.FPS
{

    // Gère la santé de l'ennemi avec zones de dégâts et tracking des hits.

    public class EnemyHealth : MonoBehaviour
    {
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;
        
        [Header("Hit Tracking")]
        [SerializeField] private Dictionary<string, int> zoneHitCount = new Dictionary<string, int>();
        
        [Header("Events")]
        public UnityEvent OnDeath;
        
        private bool isDead;
        private WaveManager waveManager;
        
        private void Awake()
        {
            currentHealth = maxHealth;
            waveManager = FindFirstObjectByType<WaveManager>();
        }
        
        private void EnsureWaveManager()
        {
            if (waveManager == null)
            {
                waveManager = FindFirstObjectByType<WaveManager>();
            }
        }
        
        // Inflige des dégâts à l'ennemi et enregistre la zone touchée.
        public void TakeDamage(float damage, string zoneName)
        {
            if (isDead) return;
            
            currentHealth -= damage;
            
            // Enregistrer le hit dans la zone
            if (!zoneHitCount.ContainsKey(zoneName))
            {
                zoneHitCount[zoneName] = 0;
            }
            zoneHitCount[zoneName]++;
            
            EnsureWaveManager();
            if (waveManager != null)
            {
                waveManager.RecordHit(zoneName);
            }
            
            // Déclencher l'effet électrique si c'est un ennemi électrique mais seulement si les dégâts ne viennent pas déjà d'une décharge électrique
            
            if (zoneName != "Electric")
            {
                var electricEnemy = GetComponent<Ennemies.Effect.ElectricEnnemis>();
                if (electricEnemy != null)
                {
                    electricEnemy.TriggerElectricDischarge();
                }
            }
            
            if (currentHealth <= 0)
            {
                Die();
            }
        }
        
        private void Die()
        {
            if (isDead) return;
            
            isDead = true;
            OnDeath?.Invoke();
            
            // Notifier le wave manager qu'un ennemi est mort
            EnsureWaveManager();
            if (waveManager != null)
            {
                waveManager.OnEnemyDeath(this);
            }
            
            Destroy(gameObject, 0.5f);
        }
        

        // Applique des armures

        public void ApplyArmorToZones(List<string> zoneNames)
        {
            var levels = new Dictionary<string, int>();
            foreach (var z in zoneNames)
            {
                levels[z] = 1;
            }
            ApplyArmorLevels(levels);
        }
        

        // Applique des niveaux d'armure par zone

        public void ApplyArmorLevels(Dictionary<string, int> zoneLevels)
        {
            HitZone[] hitZones = GetComponentsInChildren<HitZone>();
            foreach (HitZone zone in hitZones)
            {
                string key = NormalizeZoneKey(zone.ZoneName);
                if (zoneLevels != null && zoneLevels.TryGetValue(key, out int level))
                {
                    zone.SetArmorLevel(level);
                }
                else
                {
                    zone.RemoveArmor();
                }
            }
        }
        
        private static string NormalizeZoneKey(string zone)
        {
            return string.IsNullOrWhiteSpace(zone) ? string.Empty : zone.Trim().ToLowerInvariant();
        }
        
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsDead => isDead;
        public Dictionary<string, int> ZoneHitCount => zoneHitCount;
        
        public void TakeDamage(float damage)
        {
            TakeDamage(damage, "Body");
        }
        

        // Tue immédiatement cet ennemi sans enregistrer de hit

        public void KillImmediate()
        {
            if (isDead) return;
            currentHealth = 0f;
            Die();
        }
    }
}
