using UnityEngine;
using UnityEngine.Events;

namespace Proto3GD.FPS
{
    public class PlayerHealth : MonoBehaviour
    {
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
        
        private void Awake()
        {
            currentHealth = maxHealth;
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
            if (isDead) return;
            
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
    }
}

