using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FPS
{
    public class DashCooldownUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("Référence au dash ciblé pour connaître le nombre de charges")]
        private DashCible dashCible;
        
        [SerializeField, Tooltip("Texte optionnel pour afficher l'état des charges")]
        private TMPro.TextMeshProUGUI cooldownText;
        
        [Header("Dash Icons")]
        [SerializeField, Tooltip("Liste ordonnée des icônes représentant chaque charge")]
        private List<Image> dashIcons = new();

        [Header("Visual Settings")] [SerializeField, Tooltip("Couleur quand le dash est en cours de charge")]
        private Color cooldownColor;
        [SerializeField, Tooltip("Couleur quand le dash est complètement chargé")]
        private Color readyColor;

        [SerializeField, Tooltip("Couleur lorsqu'un slow-mo est actif")]
        private Color slowMoColor;
        
        [SerializeField, Tooltip("Durée de l'animation de transition")]
        private float transitionSpeed = 5f;
        
        [Header("Pulse Effect (Slow-Mo)")]
        [SerializeField, Tooltip("Intensité de la pulsation pendant le slow-mo (0.1 à 0.3 recommandé)")]
        [Range(0f, 0.5f)]
        private float pulseIntensity = 0.15f;
        
        [SerializeField, Tooltip("Vitesse de la pulsation pendant le slow-mo")]
        private float pulseSpeed = 4f;
        
        [SerializeField, Tooltip("Afficher le texte de pourcentage")]
        private bool showPercentageText = true;
        
        [SerializeField, Tooltip("Cacher la jauge quand elle est complètement chargée")]
        private bool hideWhenReady = false;

        private CanvasGroup canvasGroup;
        private readonly List<float> iconFillAmounts = new();
        private readonly List<Vector3> iconOriginalScales = new();
        private Color initialTextColor;
        private bool warnedAboutSlotShortage;
        private float pulseTimer;
        
        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            
            SyncFillBuffer();
            
            if (showPercentageText && cooldownText == null)
            {
                cooldownText = GetComponentInChildren<TMPro.TextMeshProUGUI>();
            }
            
            if (cooldownText != null)
            {
                initialTextColor = cooldownText.color;
            }
        }
        
        private void SyncFillBuffer()
        {
            iconFillAmounts.Clear();
            iconOriginalScales.Clear();
            for (int i = 0; i < dashIcons.Count; i++)
            {
                iconFillAmounts.Add(0f);
                if (dashIcons[i] != null)
                    iconOriginalScales.Add(dashIcons[i].transform.localScale);
                else
                    iconOriginalScales.Add(Vector3.one);
            }
        }
        
        private void Update()
        {
            if (dashCible == null || dashIcons.Count == 0) return;
            if (iconFillAmounts.Count != dashIcons.Count)
            {
                SyncFillBuffer();
            }
            
            int totalCharges = Mathf.Max(1, dashCible.CountDash);
            int availableCharges = dashCible.IsChainActive ? Mathf.Clamp(dashCible.RemainingChains, 0, totalCharges) : totalCharges;
            
            bool isSlowMo = dashCible.IsSlowMoActive;
            bool isCooldown = dashCible.IsCooldownActive;
            float cooldownProgress = dashCible.CooldownProgress;
            
            UpdateDashIcons(totalCharges, availableCharges, isSlowMo, isCooldown, cooldownProgress);
            UpdateCooldownText(availableCharges, totalCharges, isSlowMo, isCooldown, cooldownProgress);
            UpdateVisibility(availableCharges, totalCharges);
        }
        
        private void UpdateDashIcons(int totalCharges, int availableCharges, bool isSlowMo, bool isCooldown, float cooldownProgress)
        {
            int iconCap = dashIcons.Count;
            if (totalCharges > iconCap && !warnedAboutSlotShortage)
            {
                Debug.LogWarning($"DashCooldownUI: {name} n'a que {iconCap} icônes mais DashCible en demande {totalCharges}. Seules les premières seront utilisées.");
                warnedAboutSlotShortage = true;
            }
            int activeIcons = Mathf.Min(totalCharges, iconCap);
            
            // Mise à jour du timer de pulsation (en temps non-scalé pour fonctionner pendant le slow-mo)
            if (isSlowMo)
            {
                pulseTimer += Time.unscaledDeltaTime * pulseSpeed;
            }
            else
            {
                pulseTimer = 0f;
            }
        
            for (int i = 0; i < dashIcons.Count; i++)
            {
                Image icon = dashIcons[i];
                if (icon == null) continue;
                bool active = i < activeIcons;
                if (icon.gameObject.activeSelf != active)
                {
                    icon.gameObject.SetActive(active);
                }
                if (!active) continue;
                
                float targetFill;
                Color targetColor;
                float pulseScale = 1f;
                
                if (isSlowMo)
                {
                    // Pendant le slow-mo : bulles consommées vides, bulles restantes pleines et jaunes avec pulsation
                    targetFill = i < availableCharges ? 1f : 0f;
                    targetColor = slowMoColor;
                    
                    // Effet de pulsation sur les bulles restantes
                    if (i < availableCharges)
                    {
                        pulseScale = 1f + Mathf.Sin(pulseTimer) * pulseIntensity;
                    }
                }
                else if (isCooldown)
                {
                    // Pendant le cooldown : rechargement progressif en cascade
                    // Chaque bulle se remplit l'une après l'autre
                    float progressPerIcon = 1f / activeIcons;
                    float iconStartProgress = i * progressPerIcon;
                    float iconEndProgress = (i + 1) * progressPerIcon;
                    
                    // Calcul du remplissage pour cette bulle spécifique
                    if (cooldownProgress >= iconEndProgress)
                    {
                        targetFill = 1f;
                    }
                    else if (cooldownProgress <= iconStartProgress)
                    {
                        targetFill = 0f;
                    }
                    else
                    {
                        // Remplissage progressif de cette bulle
                        targetFill = (cooldownProgress - iconStartProgress) / progressPerIcon;
                    }
                    
                    targetColor = targetFill >= 0.999f ? readyColor : cooldownColor;
                }
                else
                {
                    // État normal : toutes les bulles sont pleines et prêtes
                    targetFill = 1f;
                    targetColor = readyColor;
                }
                
                // Animation fluide du remplissage
                iconFillAmounts[i] = Mathf.Lerp(iconFillAmounts[i], targetFill, Time.unscaledDeltaTime * transitionSpeed);
                icon.fillAmount = iconFillAmounts[i];
                icon.color = targetColor;
                
                // Application de l'effet de pulsation (scale)
                if (i < iconOriginalScales.Count)
                {
                    icon.transform.localScale = iconOriginalScales[i] * pulseScale;
                }
            }
        }
        
        private void UpdateCooldownText(int availableCharges, int totalCharges, bool isSlowMo, bool isCooldown, float cooldownProgress)
        {
            if (!showPercentageText || cooldownText == null) return;
        
            float percent;
            if (isSlowMo)
            {
                // Pendant le slow-mo, afficher le pourcentage de charges restantes
                percent = totalCharges > 0 ? (availableCharges / (float)totalCharges) * 100f : 0f;
                cooldownText.color = slowMoColor;
            }
            else if (isCooldown)
            {
                // Pendant le cooldown, afficher la progression du rechargement
                percent = cooldownProgress * 100f;
                cooldownText.color = cooldownColor;
            }
            else
            {
                // État normal : 100%
                percent = 100f;
                cooldownText.color = initialTextColor;
            }
            
            cooldownText.text = $"{Mathf.RoundToInt(percent)}%";
        }
        
        private void UpdateVisibility(int availableCharges, int totalCharges)
        {
            if (!hideWhenReady)
            {
                canvasGroup.alpha = 1f;
                return;
            }
        
            bool allReady = availableCharges >= totalCharges;
            float targetAlpha = allReady ? 0f : 1f;
            canvasGroup.alpha = Mathf.Lerp(canvasGroup.alpha, targetAlpha, Time.deltaTime * transitionSpeed);
        }
        
        public void SetDashCible(DashCible cible)
        {
            dashCible = cible;
        }
    }
}
