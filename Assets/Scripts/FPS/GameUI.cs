using FPS;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FPS
{
    /// <summary>
    /// Gère l'interface utilisateur du jeu (santé, munitions, ennemis).
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        [Header("Health UI")]
        [SerializeField] private Slider healthBar;
        [SerializeField] private TextMeshProUGUI healthText;
        
        [Header("Weapon UI")]
        [SerializeField] private TextMeshProUGUI ammoText;
        [SerializeField] private TextMeshProUGUI reloadText;
        
        [Header("Enemy Count UI")]
        [SerializeField] private TextMeshProUGUI enemiesRemainingText;
        
        [Header("Crosshair")]
        [SerializeField] private Image crosshair;
        
        [Header("References")]
        [SerializeField] private PlayerHealth playerHealth;
        
        private void Start()
        {
            if (playerHealth == null)
                playerHealth = FindFirstObjectByType<PlayerHealth>();
            
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged.AddListener(UpdateHealthUI);
            }
            
            UpdateHealthUI(playerHealth != null ? playerHealth.HealthPercentage : 1f);
        }
        
        private void Update()
        {
            // Mettre à jour le compte d'ennemis depuis le registry
            if (enemiesRemainingText != null)
            {
                int count = EnemyRegistry.Instance.Count;
                enemiesRemainingText.text = $"Ennemis: {count}";
            }
        }
        
        private void UpdateHealthUI(float healthPercentage)
        {
            if (healthBar != null)
            {
                healthBar.value = healthPercentage;
            }
            
            if (healthText != null && playerHealth != null)
            {
                healthText.text = $"{Mathf.Ceil(playerHealth.CurrentHealth)}/{playerHealth.MaxHealth}";
            }
        }
        
        private void OnDestroy()
        {
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged.RemoveListener(UpdateHealthUI);
            }
        }
    }
}
