using UnityEngine;
using UnityEngine.UI;

namespace Proto3GD.FPS
{
    /// <summary>
    /// Affiche une jauge visuelle du cooldown du dash sur l'HUD
    /// </summary>
    public class DashCooldownUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("Référence au système de dash du joueur")]
        private PillarDashSystem dashSystem;
        
        [SerializeField, Tooltip("Image de remplissage de la jauge")]
        private Image fillImage;
        
        [SerializeField, Tooltip("Texte optionnel pour afficher le pourcentage")]
        private TMPro.TextMeshProUGUI cooldownText;
        
        [Header("Visual Settings")]
        [SerializeField, Tooltip("Couleur quand le dash est en cooldown")]
        private Color cooldownColor = new Color(1f, 0.3f, 0.3f, 0.8f);
        
        [SerializeField, Tooltip("Couleur quand le dash est disponible")]
        private Color readyColor = new Color(0.3f, 1f, 0.3f, 0.8f);
        
        [SerializeField, Tooltip("Afficher le texte de pourcentage")]
        private bool showPercentageText = true;
        
        [SerializeField, Tooltip("Cacher la jauge quand elle est pleine")]
        private bool hideWhenReady = false;
        
        [SerializeField, Tooltip("Durée de l'animation de transition")]
        private float transitionSpeed = 5f;

        private CanvasGroup canvasGroup;
        private float currentFillAmount;

        private void Awake()
        {
            // Trouver automatiquement le système de dash si pas assigné
            if (dashSystem == null)
            {
                dashSystem = FindFirstObjectByType<PillarDashSystem>();
                if (dashSystem == null)
                {
                    Debug.LogError("DashCooldownUI: Aucun PillarDashSystem trouvé dans la scène!");
                }
            }
            
            // Créer un CanvasGroup pour la transparence
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            
            // Si pas de fillImage assignée, chercher dans les enfants
            if (fillImage == null)
            {
                fillImage = GetComponentInChildren<Image>();
            }
            
            // Si pas de texte assigné, chercher dans les enfants
            if (cooldownText == null && showPercentageText)
            {
                cooldownText = GetComponentInChildren<TMPro.TextMeshProUGUI>();
            }
        }

        private void Update()
        {
            if (dashSystem == null || fillImage == null) return;

            // Récupérer le progrès du cooldown (0 = en cooldown, 1 = disponible)
            float progress = dashSystem.DashCooldownProgress;
            
            // Lisser l'animation
            currentFillAmount = Mathf.Lerp(currentFillAmount, progress, Time.deltaTime * transitionSpeed);
            fillImage.fillAmount = currentFillAmount;
            
            // Changer la couleur selon l'état
            Color targetColor = progress >= 1f ? readyColor : cooldownColor;
            fillImage.color = Color.Lerp(fillImage.color, targetColor, Time.deltaTime * transitionSpeed);
            
            // Mettre à jour le texte si activé
            if (showPercentageText && cooldownText != null)
            {
                cooldownText.text = $"{Mathf.RoundToInt(progress * 100)}%";
            }
            
            // Cacher la jauge si l'option est activée et que le dash est prêt
            if (hideWhenReady)
            {
                float targetAlpha = progress >= 1f ? 0f : 1f;
                canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * transitionSpeed);
            }
            else
            {
                canvasGroup.alpha = 1f;
            }
        }

        /// <summary>
        /// Définir le système de dash manuellement
        /// </summary>
        public void SetDashSystem(PillarDashSystem system)
        {
            dashSystem = system;
        }
    }
}
