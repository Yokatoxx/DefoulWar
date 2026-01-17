using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

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

        [Header("Visual Settings")]
        [SerializeField, Tooltip("Couleur quand le dash est complètement chargé")]
        private Color readyColor = Color.white;

        [SerializeField, Tooltip("Couleur lorsqu'un slow-mo est actif")]
        private Color slowMoColor = Color.yellow;
        
        [Header("Animation Settings")]
        [SerializeField, Tooltip("Durée du depop quand on utilise un dash")]
        private float depopDuration = 0.05f;
        
        [SerializeField, Tooltip("Scale du depop (1.3 = 30% plus grand avant de disparaître)")]
        [Range(1f, 1.5f)]
        private float depopScale = 1.3f;
        
        [SerializeField, Tooltip("Durée du pop quand une charge revient")]
        private float popDuration = 0.15f;
        
        [SerializeField, Tooltip("Scale du pop (1.2 = 20% plus grand)")]
        [Range(1f, 1.5f)]
        private float popScale = 1.2f;
        
        [Header("Pulse Effect (Slow-Mo)")]
        [SerializeField, Tooltip("Activer la pulsation pendant le slow-mo")]
        private bool enablePulse = true;
        
        [SerializeField, Tooltip("Intensité de la pulsation pendant le slow-mo")]
        [Range(0f, 0.5f)]
        private float pulseIntensity = 0.15f;
        
        [SerializeField, Tooltip("Durée d'un cycle de pulsation")]
        private float pulseDuration = 0.3f;
        
        [SerializeField, Tooltip("Afficher le texte de pourcentage")]
        private bool showPercentageText = true;
        
        [SerializeField, Tooltip("Cacher la jauge quand elle est complètement chargée")]
        private bool hideWhenReady = false;

        private CanvasGroup canvasGroup;
        private readonly List<Vector3> iconOriginalScales = new();
        private readonly List<bool> iconIsActive = new();
        private readonly List<Tweener> pulseTweens = new();
        private Color initialTextColor;
        private bool warnedAboutSlotShortage;
        private int lastAvailableCharges = -1;
        private bool isInitialized;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
            
            CacheOriginalScales();
            
            if (showPercentageText && cooldownText == null)
            {
                cooldownText = GetComponentInChildren<TMPro.TextMeshProUGUI>();
            }
            
            if (cooldownText != null)
            {
                initialTextColor = cooldownText.color;
            }
        }
        
        private void CacheOriginalScales()
        {
            iconOriginalScales.Clear();
            iconIsActive.Clear();
            pulseTweens.Clear();
            
            for (int i = 0; i < dashIcons.Count; i++)
            {
                if (dashIcons[i] != null)
                {
                    iconOriginalScales.Add(dashIcons[i].transform.localScale);
                    iconIsActive.Add(true);
                }
                else
                {
                    iconOriginalScales.Add(Vector3.one);
                    iconIsActive.Add(false);
                }
                pulseTweens.Add(null);
            }
        }

        private void Start()
        {
            if (dashCible == null)
                dashCible = FindFirstObjectByType<DashCible>();
            
            if (dashCible != null)
            {
                lastAvailableCharges = dashCible.CountDash;
                RefreshAllIcons(lastAvailableCharges, false);
                isInitialized = true;
            }
        }
        
        private void Update()
        {
            if (dashCible == null || dashIcons.Count == 0 || !isInitialized) return;
            
            int totalCharges = Mathf.Max(1, dashCible.CountDash);
            int availableCharges = dashCible.IsChainActive 
                ? Mathf.Clamp(dashCible.RemainingChains, 0, totalCharges) 
                : totalCharges;
            
            bool isSlowMo = dashCible.IsSlowMoActive;
            
            // Détecter les changements de charges
            if (availableCharges != lastAvailableCharges)
            {
                HandleChargesChanged(lastAvailableCharges, availableCharges, totalCharges);
                lastAvailableCharges = availableCharges;
            }
            
            // Gérer la pulsation pendant le slow-mo
            HandleSlowMoPulse(isSlowMo, availableCharges);
            
            // Mise à jour du texte
            UpdateCooldownText(availableCharges, totalCharges, isSlowMo);
            
            // Visibilité
            UpdateVisibility(availableCharges, totalCharges);
        }

        private void HandleChargesChanged(int oldCharges, int newCharges, int totalCharges)
        {
            if (newCharges < oldCharges)
            {
                // On a utilisé des dashes - depop les icônes consommées
                for (int i = newCharges; i < oldCharges && i < dashIcons.Count; i++)
                {
                    DepopIcon(i);
                }
            }
            else if (newCharges > oldCharges)
            {
                // Des charges sont revenues - pop les icônes rechargées
                for (int i = oldCharges; i < newCharges && i < dashIcons.Count; i++)
                {
                    PopIcon(i);
                }
            }
        }

        private void DepopIcon(int index)
        {
            if (index < 0 || index >= dashIcons.Count) return;
            
            Image icon = dashIcons[index];
            if (icon == null) return;
            
            // Kill les animations en cours
            icon.DOKill();
            icon.transform.DOKill();
            StopPulseTween(index);
            
            // Effet depop : grossit puis disparaît
            Sequence seq = DOTween.Sequence();
            seq.Append(icon.transform.DOScale(iconOriginalScales[index] * depopScale, depopDuration).SetEase(Ease.OutQuad));
            seq.AppendCallback(() => {
                icon.gameObject.SetActive(false);
                icon.transform.localScale = iconOriginalScales[index];
            });
            seq.SetUpdate(true);
            
            iconIsActive[index] = false;
        }

        private void PopIcon(int index)
        {
            if (index < 0 || index >= dashIcons.Count) return;
            
            Image icon = dashIcons[index];
            if (icon == null) return;
            
            // Kill les animations en cours
            icon.DOKill();
            icon.transform.DOKill();
            
            // Réactiver et préparer
            icon.gameObject.SetActive(true);
            icon.transform.localScale = iconOriginalScales[index] * 0.5f;
            icon.color = readyColor;
            
            // Effet pop : apparaît puis grossit puis revient
            Sequence seq = DOTween.Sequence();
            seq.Append(icon.transform.DOScale(iconOriginalScales[index], popDuration * 0.5f).SetEase(Ease.OutBack));
            seq.Append(icon.transform.DOScale(iconOriginalScales[index] * popScale, popDuration * 0.25f).SetEase(Ease.OutQuad));
            seq.Append(icon.transform.DOScale(iconOriginalScales[index], popDuration * 0.25f).SetEase(Ease.OutBounce));
            seq.SetUpdate(true);
            
            iconIsActive[index] = true;
        }

        private void HandleSlowMoPulse(bool isSlowMo, int availableCharges)
        {
            for (int i = 0; i < dashIcons.Count; i++)
            {
                Image icon = dashIcons[i];
                if (icon == null || !iconIsActive[i]) continue;
                
                bool shouldPulse = enablePulse && isSlowMo && i < availableCharges;
                
                if (shouldPulse && pulseTweens[i] == null)
                {
                    // Démarrer la pulsation
                    icon.DOColor(slowMoColor, 0.1f).SetUpdate(true);
                    pulseTweens[i] = icon.transform
                        .DOScale(iconOriginalScales[i] * (1f + pulseIntensity), pulseDuration)
                        .SetEase(Ease.InOutSine)
                        .SetLoops(-1, LoopType.Yoyo)
                        .SetUpdate(true);
                }
                else if (!shouldPulse && pulseTweens[i] != null)
                {
                    // Arrêter la pulsation
                    StopPulseTween(i);
                    icon.transform.DOScale(iconOriginalScales[i], 0.1f).SetUpdate(true);
                    icon.DOColor(readyColor, 0.1f).SetUpdate(true);
                }
            }
        }

        private void StopPulseTween(int index)
        {
            if (index < pulseTweens.Count && pulseTweens[index] != null)
            {
                pulseTweens[index].Kill();
                pulseTweens[index] = null;
            }
        }

        private void RefreshAllIcons(int availableCharges, bool animate)
        {
            int iconCap = dashIcons.Count;
            if (dashCible != null && dashCible.CountDash > iconCap && !warnedAboutSlotShortage)
            {
                Debug.LogWarning($"DashCooldownUI: {name} n'a que {iconCap} icônes mais DashCible en demande {dashCible.CountDash}.");
                warnedAboutSlotShortage = true;
            }
            
            for (int i = 0; i < dashIcons.Count; i++)
            {
                Image icon = dashIcons[i];
                if (icon == null) continue;
                
                bool shouldBeActive = i < availableCharges;
                
                if (animate)
                {
                    if (shouldBeActive && !iconIsActive[i])
                        PopIcon(i);
                    else if (!shouldBeActive && iconIsActive[i])
                        DepopIcon(i);
                }
                else
                {
                    // Setup immédiat sans animation
                    icon.gameObject.SetActive(shouldBeActive);
                    icon.transform.localScale = iconOriginalScales[i];
                    icon.color = readyColor;
                    iconIsActive[i] = shouldBeActive;
                }
            }
        }
        
        private void UpdateCooldownText(int availableCharges, int totalCharges, bool isSlowMo)
        {
            if (!showPercentageText || cooldownText == null) return;
        
            float percent = totalCharges > 0 ? (availableCharges / (float)totalCharges) * 100f : 0f;
            
            if (isSlowMo)
            {
                cooldownText.color = slowMoColor;
            }
            else
            {
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
            canvasGroup.DOFade(targetAlpha, 0.2f).SetUpdate(true);
        }
        
        public void SetDashCible(DashCible cible)
        {
            dashCible = cible;
        }

        private void OnDestroy()
        {
            // Nettoyer tous les tweens
            foreach (var icon in dashIcons)
            {
                if (icon != null)
                {
                    icon.DOKill();
                    icon.transform.DOKill();
                }
            }
            
            for (int i = 0; i < pulseTweens.Count; i++)
            {
                StopPulseTween(i);
            }
        }
    }
}
