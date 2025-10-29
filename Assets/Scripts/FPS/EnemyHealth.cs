using System.Collections;
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
        private Dictionary<string, int> zoneHitCount = new Dictionary<string, int>();
        
        [Header("Events")]
        public UnityEvent OnDeath;
        public UnityEvent<float, string> OnDamageTaken;
        
        [Header("Dash Tracking")]
        [SerializeField] private bool killedByDash;
        
        private bool isDead;
        private WaveManager waveManager;
        private PillarDashSystem dashSystem;
        
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
        
        private void EnsureDashSystem()
        {
            if (dashSystem == null)
            {
                dashSystem = FindFirstObjectByType<PillarDashSystem>();
            }
        }
        
        // Inflige des dégâts à l'ennemi et enregistre la zone touchée.
        public void TakeDamage(float damage, string zoneName)
        {
            if (isDead) return;
            
            // Détecter si les dégâts viennent d'un dash
            bool isDashDamage = zoneName == "Dash";
            
            // Vérifier si c'est un ennemi électrique
            var electricEnemy = GetComponent<Ennemies.Effect.ElectricEnnemis>();
            
            // Si c'est un ennemi électrique ET que c'est un dash, résister et stun le joueur
            if (isDashDamage && electricEnemy != null)
            {
                // NE PAS appliquer de dégâts aux ennemis électriques lors du dash
                // Mais appliquer le stun au joueur
                EnsureDashSystem();
                if (dashSystem != null)
                {
                    var player = dashSystem.gameObject;
                    var playerStun = player.GetComponent<PlayerStunAutoFire>();
                    if (playerStun == null)
                    {
                        playerStun = player.AddComponent<PlayerStunAutoFire>();
                    }
                    
                    if (electricEnemy.OverrideAutoFireInterval)
                    {
                        playerStun.ApplyStun(electricEnemy.StunDuration, electricEnemy.StunAutoFireInterval);
                    }
                    else
                    {
                        playerStun.ApplyStun(electricEnemy.StunDuration);
                    }
                    
                    Debug.Log($"[EnemyHealth] Ennemi électrique touché par dash - Résiste et stun le joueur !");
                }
                
                // Ne pas continuer le traitement des dégâts
                return;
            }
            
            // Appliquer les dégâts normalement pour les autres cas
            currentHealth -= damage;
            
            // Enregistrer le hit dans la zone
            if (!zoneHitCount.ContainsKey(zoneName))
            {
                zoneHitCount[zoneName] = 0;
            }
            zoneHitCount[zoneName]++;
            
            // Déclencher l'événement de dégâts pris
            OnDamageTaken?.Invoke(damage, zoneName);
            
            EnsureWaveManager();
            if (waveManager != null)
            {
                waveManager.RecordHit(zoneName);
            }
            
            // Si c'est un dash et l'ennemi va mourir (non électrique)
            if (isDashDamage && currentHealth <= 0)
            {
                killedByDash = true;
                
                // Désactiver les collisions immédiatement pour permettre le dash à travers
                DisableCollisions();
            }
            
            // Déclencher l'effet électrique si c'est un ennemi électrique mais seulement si les dégâts ne viennent pas déjà d'une décharge électrique
            if (zoneName != "Electric" && zoneName != "Dash")
            {
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
            
            // Si c'est un DashEnergyEnemy, recharger le dash du joueur
            var dashEnergyEnemy = GetComponent<DashEnergyEnemy>();
            if (dashEnergyEnemy != null)
            {
                EnsureDashSystem();
                if (dashSystem != null)
                {
                    float energyAmount = dashEnergyEnemy.DashEnergyAmount;
                    dashSystem.OnDashEnemyKilled(energyAmount);
                    Debug.Log($"[EnemyHealth] DashEnergyEnemy tué, recharge du dash: {energyAmount}");
                }
            }
            
            Destroy(gameObject);
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
        
        // Désactive toutes les collisions de cet ennemi pour permettre le dash de passer à travers
        private void DisableCollisions()
        {
            Collider[] colliders = GetComponentsInChildren<Collider>();
            foreach (Collider col in colliders)
            {
                col.enabled = false;
            }
            
            // Désactiver également le rigidbody pour éviter les interactions physiques
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = true;
                rb.detectCollisions = false;
            }
            
            Rigidbody[] childRbs = GetComponentsInChildren<Rigidbody>();
            foreach (Rigidbody childRb in childRbs)
            {
                childRb.isKinematic = true;
                childRb.detectCollisions = false;
            }
        }
        
        public bool KilledByDash => killedByDash;
    }
}
