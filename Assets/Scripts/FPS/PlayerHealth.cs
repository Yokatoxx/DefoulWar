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
        
        [Header("References")]
        [Tooltip("Référence au DashCible pour vérifier l'état du dash. Auto-détecté si non assigné.")]
        [SerializeField] private DashCible dashCible;
        
        [Header("Events")]
        public UnityEvent<float> OnHealthChanged;
        public UnityEvent OnDeath;
        
        private float timeSinceLastDamage;
        private bool isDead;
        private bool isInvulnerable;
        
        private void Awake()
        {
            currentHealth = maxHealth;
            // Auto-détection si non assigné dans l'inspecteur
            if (dashCible == null)
                dashCible = GetComponent<DashCible>();
            if (dashCible == null)
                dashCible = GetComponentInChildren<DashCible>();
            if (dashCible == null)
                dashCible = GetComponentInParent<DashCible>();
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
            if (dashCible != null && dashCible.isDashing) return;

            if (soundPlayer != null)
            {
                soundPlayer.PlayOneShot("OuchRoblox", 0.5f, Random.Range(0.9f, 1.1f));
            }

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
