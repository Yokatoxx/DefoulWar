using UnityEngine;
using UnityEngine.Events;

namespace FPS
{
    public class PlayerHealth : MonoBehaviour
    {
        [SerializeField] private SoundPlayer soundPlayer;
        [Header("Health Settings")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private float currentHealth;
        
        [Header("Regeneration")]
        [SerializeField] private bool enableRegen = true;
        [SerializeField] private float regenDelay = 3f;
        [SerializeField] private float regenRate = 5f;
        
        [Header("Events")]
        public UnityEvent<float> OnHealthChanged;
        public UnityEvent OnDeath;
        
        private float timeSinceLastDamage;
        private bool isDead;
        private bool isInvulnerable;
        private DashCible dashCible;
        private DashSystem dashSystem;
        
        private void Awake()
        {
            currentHealth = maxHealth;
            // Chercher DashCible sur cet objet, dans les enfants, ou dans les parents
            dashCible = GetComponent<DashCible>();
            if (dashCible == null)
                dashCible = GetComponentInChildren<DashCible>();
            if (dashCible == null)
                dashCible = GetComponentInParent<DashCible>();
            if (dashCible == null)
                dashCible = FindFirstObjectByType<DashCible>();
            
            // Chercher DashSystem de la même façon
            dashSystem = GetComponent<DashSystem>();
            if (dashSystem == null)
                dashSystem = GetComponentInChildren<DashSystem>();
            if (dashSystem == null)
                dashSystem = GetComponentInParent<DashSystem>();
            if (dashSystem == null)
                dashSystem = FindFirstObjectByType<DashSystem>();
        }
        
        /// <summary>
        /// Vérifie si le joueur est en train de dasher (avec l'un ou l'autre système)
        /// </summary>
        private bool IsDashing()
        {
            if (dashCible != null && dashCible.isDashing) return true;
            if (dashSystem != null && dashSystem.isDashing) return true;
            return false;
        }
        
        private void Update()
        {
            if (enableRegen && !isDead && currentHealth < maxHealth)
            {
                timeSinceLastDamage += Time.deltaTime;
                
                if (timeSinceLastDamage >= regenDelay)
                {
                    Heal(regenRate * Time.deltaTime);
                }
            }
        }
        
        public void TakeDamage(float damage)
        {
            // Ignorer les dégâts si mort, invulnérable, ou en train de dasher
            if (isDead || isInvulnerable) return;
            if (IsDashing()) return;

            soundPlayer.PlayOneShot("OuchRoblox", 0.5f, Random.Range(0.9f, 1.1f));  

            currentHealth = Mathf.Max(0, currentHealth - damage);
            timeSinceLastDamage = 0f;
            
            OnHealthChanged?.Invoke(currentHealth / maxHealth);
            
            if (currentHealth <= 0)
            {
                Die();
            }
        }

        public void Heal(float amount)
        {
            if (isDead) return;
            
            currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
            OnHealthChanged?.Invoke(currentHealth / maxHealth);
        }
        
        public void ResetHealth()
        {
            currentHealth = maxHealth;
            isDead = false;
            timeSinceLastDamage = 0f;
            OnHealthChanged?.Invoke(1f);
        }
        
        private void Die()
        {
            isDead = true;
            OnDeath?.Invoke();
            Debug.Log("Player died!");
        }
        
        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public float HealthPercentage => currentHealth / maxHealth;
        public bool IsDead => isDead;
        public bool IsInvulnerable => isInvulnerable;

        public void SetInvulnerable(bool invulnerable)
        {
            isInvulnerable = invulnerable;
        }
    }
}
