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
        
        // Limite maximale de régénération (en pourcentage, 1 = 100%)
        private float regenCapPercentage = 1f;
        
        [Header("References")]
        [Tooltip("Référence au DashCible pour vérifier l'état du dash. Auto-détecté si non assigné.")]
        [SerializeField] private DashCible dashCible;
        
        [Header("Camera Shake - Dégâts")]
        [Tooltip("Activer le screenshake quand le joueur prend des dégâts")]
        [SerializeField] private bool enableDamageShake = true;
        [Tooltip("Durée du shake")]
        [SerializeField] private float damageShakeDuration = 0.25f;
        [Tooltip("Intensité du déplacement de la caméra")]
        [SerializeField] private float damageShakePositionMag = 0.08f;
        [Tooltip("Intensité de la rotation de la caméra")]
        [SerializeField] private float damageShakeRotationMag = 2f;
        
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
            float regenCap = maxHealth * regenCapPercentage;
            if (enableRegen && !isDead && currentHealth < regenCap)
            {
                timeSinceLastDamage += Time.deltaTime;
                
                if (timeSinceLastDamage >= regenDelay)
                {
                    // Ne pas dépasser le plafond de regen
                    float healAmount = regenRate * Time.deltaTime;
                    float newHealth = Mathf.Min(regenCap, currentHealth + healAmount);
                    if (newHealth > currentHealth)
                    {
                        currentHealth = newHealth;
                        OnHealthChanged?.Invoke(currentHealth / maxHealth);
                    }
                }
            }
        }
        
        public void TakeDamage(float damage)
        {
            TakeDamageInternal(damage, null);
        }
        
        public void TakeDamage(float damage, Vector3 attackerPosition)
        {
            TakeDamageInternal(damage, attackerPosition);
        }

        private void TakeDamageInternal(float damage, Vector3? attackerPosition)
        {
            if (isDead || isInvulnerable) return;
            if (dashCible != null && dashCible.isDashing) return;

            if (soundPlayer != null)
            {
                soundPlayer.PlayOneShot("OuchRoblox", 0.5f, Random.Range(0.9f, 1.1f));
            }

            currentHealth = Mathf.Max(0, currentHealth - damage);
            timeSinceLastDamage = 0f;

            // Screenshake directionnel ou classique selon si on a la position de l'attaquant
            if (enableDamageShake && CameraShake.Instance != null)
            {
                if (attackerPosition.HasValue)
                {
                    CameraShake.Instance.DirectionalShake(
                        attackerPosition.Value, 
                        damageShakeDuration, 
                        damageShakePositionMag, 
                        damageShakeRotationMag);
                }
                else
                {
                    CameraShake.Instance.ShakeWithRotation(
                        damageShakeDuration, 
                        damageShakePositionMag, 
                        damageShakeRotationMag);
                }
            }

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
        
        /// <summary>
        /// Définit le plafond de régénération en pourcentage (0 à 1).
        /// Par exemple, 0.5f = la regen s'arrête à 50% de la vie max.
        /// </summary>
        public void SetRegenCap(float percentage)
        {
            regenCapPercentage = Mathf.Clamp01(percentage);
        }
        
        /// <summary>
        /// Réinitialise le plafond de régénération à 100%.
        /// </summary>
        public void ResetRegenCap()
        {
            regenCapPercentage = 1f;
        }
        
        public float RegenCapPercentage => regenCapPercentage;
    }
}
