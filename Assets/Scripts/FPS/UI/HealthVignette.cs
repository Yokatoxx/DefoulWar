using UnityEngine;
using UnityEngine.UI;

namespace FPS.UI
{
    public class HealthVignette : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private Image vignetteImage;
        
        [Header("Settings")]
        [Tooltip("Seuil de vie en dessous duquel la vignette commence à apparaître (0-1)")]
        [Range(0f, 1f)]
        [SerializeField] private float healthThreshold = 0.5f;
        
        [Tooltip("Alpha maximum de la vignette quand la vie est très basse")]
        [Range(0f, 1f)]
        [SerializeField] private float maxAlpha = 0.6f;
        
        [Tooltip("Couleur de la vignette")]
        [SerializeField] private Color vignetteColor = new Color(0.5f, 0f, 0f, 1f);
        
        [Header("Pulse Effect")]
        [Tooltip("Activer l'effet de pulsation quand la vie est critique")]
        [SerializeField] private bool enablePulse = true;
        
        [Tooltip("Seuil de vie pour l'effet pulsation (0-1)")]
        [Range(0f, 1f)]
        [SerializeField] private float pulseThreshold = 0.25f;
        
        [Tooltip("Vitesse de pulsation")]
        [SerializeField] private float pulseSpeed = 3f;
        
        [Tooltip("Intensité de la pulsation")]
        [Range(0f, 0.5f)]
        [SerializeField] private float pulseIntensity = 0.2f;
        
        private float currentHealthPercent = 1f;
        private float baseAlpha;
        
        private void Awake()
        {
            if (playerHealth == null)
                playerHealth = FindFirstObjectByType<PlayerHealth>();
                
            if (vignetteImage != null)
            {
                vignetteImage.color = new Color(vignetteColor.r, vignetteColor.g, vignetteColor.b, 0f);
            }
        }
        
        private void OnEnable()
        {
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged.AddListener(OnHealthChanged);
                // Initialiser avec la vie actuelle
                OnHealthChanged(playerHealth.HealthPercentage);
            }
        }
        
        private void OnDisable()
        {
            if (playerHealth != null)
            {
                playerHealth.OnHealthChanged.RemoveListener(OnHealthChanged);
            }
        }
        
        private void Update()
        {
            if (!enablePulse || vignetteImage == null) return;
            
            // Effet de pulsation quand la vie est critique
            if (currentHealthPercent <= pulseThreshold && currentHealthPercent > 0f)
            {
                float pulse = Mathf.Sin(Time.time * pulseSpeed) * 0.5f + 0.5f;
                float pulseAlpha = baseAlpha + (pulse * pulseIntensity);
                SetVignetteAlpha(Mathf.Min(pulseAlpha, maxAlpha));
            }
        }
        
        private void OnHealthChanged(float healthPercent)
        {
            currentHealthPercent = healthPercent;
            
            if (vignetteImage == null) return;
            
            // Calculer l'alpha en fonction de la vie
            if (healthPercent >= healthThreshold)
            {
                baseAlpha = 0f;
            }
            else
            {
                // Interpolation inverse : plus la vie est basse, plus l'alpha est haut
                float t = 1f - (healthPercent / healthThreshold);
                baseAlpha = Mathf.Lerp(0f, maxAlpha, t);
            }
            
            // Appliquer l'alpha si pas d'effet de pulsation actif
            if (!enablePulse || currentHealthPercent > pulseThreshold)
            {
                SetVignetteAlpha(baseAlpha);
            }
        }
        
        private void SetVignetteAlpha(float alpha)
        {
            Color color = vignetteImage.color;
            color.a = alpha;
            vignetteImage.color = color;
        }
    }
}
