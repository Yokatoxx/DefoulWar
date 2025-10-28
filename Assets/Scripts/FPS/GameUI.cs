using FPS;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Proto3GD.FPS
{
    /// <summary>
    /// Gère l'interface utilisateur du jeu (santé, munitions, vague).
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        [Header("Health UI")]
        [SerializeField] private Slider healthBar;
        [SerializeField] private TextMeshProUGUI healthText;
        
        [Header("Weapon UI")]
        [SerializeField] private TextMeshProUGUI ammoText;
        [SerializeField] private TextMeshProUGUI reloadText;
        
        [Header("Wave UI")]
        [SerializeField] private TextMeshProUGUI waveNumberText;
        [SerializeField] private TextMeshProUGUI enemiesRemainingText;
        [SerializeField] private TextMeshProUGUI waveTimerText;
        [SerializeField] private GameObject waveCompletePanel;
        [SerializeField] private TextMeshProUGUI waveCompleteText;
        
        [Header("Crosshair")]
        [SerializeField] private Image crosshair;
        
        [Header("References")]
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private WeaponController weaponController; // hitscan
        [SerializeField] private ProjectileWeaponController projectileWeaponController; // projectiles
        [SerializeField] private WaveManager waveManager;
        
        private void Start()
        {
            // Auto-trouver les références si non assignées
            if (playerHealth == null)
                playerHealth = FindFirstObjectByType<PlayerHealth>();
            if (weaponController == null)
                weaponController = FindFirstObjectByType<WeaponController>();
            if (projectileWeaponController == null)
                projectileWeaponController = FindFirstObjectByType<ProjectileWeaponController>();
            if (waveManager == null)
                waveManager = FindFirstObjectByType<WaveManager>();
            
            // S'abonner aux événements
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged.AddListener(UpdateHealthUI);
            }
            
            if (waveManager != null)
            {
                waveManager.onWaveStart.AddListener(OnWaveStart);
                waveManager.onWaveComplete.AddListener(OnWaveComplete);
                waveManager.onEnemyCountChanged.AddListener(UpdateEnemyCount);
            }
            
            // Cacher le panneau de fin de vague
            if (waveCompletePanel != null)
                waveCompletePanel.SetActive(false);
            
            // Initialiser l'UI
            UpdateHealthUI(playerHealth != null ? playerHealth.HealthPercentage : 1f);
        }
        
        private void Update()
        {
            UpdateWeaponUI();
            UpdateWaveTimerUI();
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
        
        private void UpdateWeaponUI()
        {
            // Priorité à l'arme à projectiles si présente
            if (projectileWeaponController != null)
            {
                if (ammoText != null)
                {
                    ammoText.text = $"{projectileWeaponController.CurrentAmmo} / {projectileWeaponController.MaxAmmo}";
                }
                if (reloadText != null)
                {
                    reloadText.gameObject.SetActive(projectileWeaponController.IsReloading);
                }
                return;
            }
            
            if (weaponController != null)
            {
                if (ammoText != null)
                {
                    ammoText.text = $"{weaponController.CurrentAmmo} / {weaponController.MaxAmmo}";
                }
                if (reloadText != null)
                {
                    reloadText.gameObject.SetActive(weaponController.IsReloading);
                }
            }
        }
        
        private void UpdateEnemyCount(int remaining)
        {
            if (enemiesRemainingText != null)
            {
                enemiesRemainingText.text = $"Ennemis: {remaining}";
            }
        }
        
        private void UpdateWaveTimerUI()
        {
            if (waveTimerText != null && waveManager != null)
            {
                if (waveManager.IsWaveActive)
                {
                    float timeRemaining = waveManager.WaveTimeRemaining;
                    int minutes = Mathf.FloorToInt(timeRemaining / 60f);
                    int seconds = Mathf.FloorToInt(timeRemaining % 60f);
                    waveTimerText.text = $"Temps: {minutes:00}:{seconds:00}";
                    
                    // Changer la couleur si le temps est presque écoulé
                    if (timeRemaining <= 10f && waveTimerText != null)
                    {
                        waveTimerText.color = Color.Lerp(Color.red, Color.yellow, Mathf.PingPong(Time.time * 2f, 1f));
                    }
                    else if (waveTimerText != null)
                    {
                        waveTimerText.color = Color.white;
                    }
                }
                else
                {
                    waveTimerText.text = "Temps: --:--";
                    waveTimerText.color = Color.white;
                }
            }
        }
        
        private void OnWaveStart(int waveNumber)
        {
            if (waveNumberText != null)
            {
                waveNumberText.text = $"Vague {waveNumber}";
            }
            
            if (waveCompletePanel != null)
            {
                waveCompletePanel.SetActive(false);
            }
        }
        
        private void OnWaveComplete(int waveNumber)
        {
            if (waveCompletePanel != null && waveCompleteText != null)
            {
                waveCompletePanel.SetActive(true);
                waveCompleteText.text = $"Vague {waveNumber} Terminée!\nProchaine vague dans quelques secondes...";
                
                // Cacher après 3 secondes
                Invoke(nameof(HideWaveCompletePanel), 3f);
            }
        }
        
        private void HideWaveCompletePanel()
        {
            if (waveCompletePanel != null)
                waveCompletePanel.SetActive(false);
        }
        
        private void OnDestroy()
        {
            // Se désabonner des événements
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged.RemoveListener(UpdateHealthUI);
            }
            
            if (waveManager != null)
            {
                waveManager.onWaveStart.RemoveListener(OnWaveStart);
                waveManager.onWaveComplete.RemoveListener(OnWaveComplete);
                waveManager.onEnemyCountChanged.RemoveListener(UpdateEnemyCount);
            }
        }
    }
}
