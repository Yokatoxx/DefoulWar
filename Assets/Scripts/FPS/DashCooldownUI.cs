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
        
        [SerializeField, Tooltip("Afficher le texte de pourcentage")]
        private bool showPercentageText = true;
        
        [SerializeField, Tooltip("Cacher la jauge quand elle est complètement chargée")]
        private bool hideWhenReady = false;

        private CanvasGroup canvasGroup;
        private Color initialTextColor;
        private bool warnedAboutSlotShortage;
        
        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }

            if (showPercentageText && cooldownText == null)
            {
                cooldownText = GetComponentInChildren<TMPro.TextMeshProUGUI>();
            }
            
            if (cooldownText != null)
            {
                initialTextColor = cooldownText.color;
            }
        }
        
        private void Update()
        {
            if (dashCible == null || dashIcons.Count == 0) return;
            
            int totalCharges = Mathf.Max(1, dashCible.CountDash);
            int availableCharges = dashCible.IsChainActive ? Mathf.Clamp(dashCible.RemainingChains, 0, totalCharges) : totalCharges;
            
            bool isSlowMo = dashCible.IsSlowMoActive;
            UpdateDashIcons(totalCharges, availableCharges, isSlowMo);
            UpdateCooldownText(availableCharges, totalCharges, isSlowMo);
            UpdateVisibility(availableCharges, totalCharges);
        }
        
        private void UpdateDashIcons(int totalCharges, int availableCharges, bool isSlowMo)
        {
            int iconCap = dashIcons.Count;
            if (totalCharges > iconCap && !warnedAboutSlotShortage)
            {
                Debug.LogWarning($"DashCooldownUI: {name} n'a que {iconCap} icônes mais DashCible en demande {totalCharges}. Seules les premières seront utilisées.");
                warnedAboutSlotShortage = true;
            }
            int activeIcons = Mathf.Min(totalCharges, iconCap);
        
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
                
                float targetFill = i < availableCharges ? 1f : 0f;
                icon.fillAmount = targetFill;
                if (isSlowMo && targetFill > 0f)
                {
                    icon.color = slowMoColor;
                }
                else
                {
                    icon.color = targetFill >= 1f ? readyColor : cooldownColor;
                }
            }
        }
        
        private void UpdateCooldownText(int availableCharges, int totalCharges, bool isSlowMo)
        {
            if (!showPercentageText || cooldownText == null) return;
        
            float percent = totalCharges > 0 ? (availableCharges / (float)totalCharges) * 100f : 0f;
            cooldownText.text = $"{Mathf.RoundToInt(percent)}%";
            cooldownText.color = isSlowMo && availableCharges > 0 ? slowMoColor : initialTextColor;
        }
        
        private void UpdateVisibility(int availableCharges, int totalCharges)
        {
            if (!hideWhenReady)
            {
                canvasGroup.alpha = 1f;
                return;
            }
        
            bool allReady = availableCharges >= totalCharges;
            canvasGroup.alpha = allReady ? 0f : 1f;
        }
        
        public void SetDashCible(DashCible cible)
        {
            dashCible = cible;
        }
    }
}