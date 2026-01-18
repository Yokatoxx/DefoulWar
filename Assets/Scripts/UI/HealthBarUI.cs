using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using FPS;

namespace UI
{
    /// <summary>
    /// Jauge de vie du joueur avec effets DOTween.
    /// - Barre principale qui se remplit/vide smoothly
    /// - Barre de "dégâts" qui suit avec délai (effet de retard visuel)
    /// - Shake et flash quand on prend des dégâts
    /// - Pulse et glow quand la vie est basse
    /// </summary>
    public class HealthBarUI : MonoBehaviour
    {
        [Header("Références")]
        [SerializeField] private PlayerHealth playerHealth;
        [SerializeField] private WeaponSystem weaponSystem;
        [SerializeField] private Image healthFill;
        [SerializeField] private Image damageFill;
        [SerializeField] private RectTransform barContainer;
        
        [Header("Couleurs")]
        [SerializeField] private Gradient healthGradient;
        [SerializeField] private Color damageTrailColor = new Color(1f, 0.3f, 0.3f, 0.8f);
        
        [Header("Seuils")]
        [SerializeField] [Range(0f, 1f)] private float criticalThreshold = 0.25f;
        
        [Header("Animation Settings")]
        [SerializeField] private float fillDuration = 0.3f;
        [SerializeField] private float damageTrailDelay = 0.5f;
        [SerializeField] private float damageTrailDuration = 0.4f;
        [SerializeField] private float shakeStrength = 10f;
        [SerializeField] private float shakeDuration = 0.2f;
        [SerializeField] private float flashDuration = 0.1f;
        
        [Header("Low Health Pulse")]
        [SerializeField] private bool enableLowHealthPulse = true;
        [SerializeField] private float pulseScale = 1.05f;
        [SerializeField] private float pulseDuration = 0.5f;
        
        [Header("Blood Bullet Mode")]
        [SerializeField] private Color bloodBulletColor = new Color(0.8f, 0.1f, 0.2f);
        [SerializeField] private float bloodBulletPulseDuration = 0.3f;
        
        private float currentDisplayedHealth = 1f;
        private float targetHealth = 1f;
        private Tween healthTween;
        private Tween damageTween;
        private Tween pulseTween;
        private Tween shakeTween;
        private Tween bloodBulletTween;
        private Vector2 originalAnchoredPosition;
        private bool isInBloodBulletMode;
        
        private void Awake()
        {
            if (playerHealth == null)
                playerHealth = FindAnyObjectByType<PlayerHealth>();
            if (weaponSystem == null)
                weaponSystem = FindAnyObjectByType<WeaponSystem>();
            
            if (damageFill != null)
                damageFill.color = damageTrailColor;
            
            if (barContainer != null)
                originalAnchoredPosition = barContainer.anchoredPosition;
        }
        
        private void OnEnable()
        {
            if (playerHealth != null)
                playerHealth.OnHealthChanged.AddListener(OnHealthChanged);
            if (weaponSystem != null)
                weaponSystem.OnBloodBulletModeChanged.AddListener(OnBloodBulletModeChanged);
            
            SetHealthImmediate(1f);
        }
        
        private void OnDisable()
        {
            if (playerHealth != null)
                playerHealth.OnHealthChanged.RemoveListener(OnHealthChanged);
            if (weaponSystem != null)
                weaponSystem.OnBloodBulletModeChanged.RemoveListener(OnBloodBulletModeChanged);
            
            KillAllTweens();
        }
        
        private void OnHealthChanged(float healthPercent)
        {
            float previousHealth = targetHealth;
            targetHealth = healthPercent;
            
            bool tookDamage = healthPercent < previousHealth;
            
            // Animation de la barre principale
            AnimateHealthBar(healthPercent, tookDamage);
            
            // Couleur basée sur le niveau de vie
            UpdateBarColor(healthPercent);
            
            // Effets quand on prend des dégâts
            if (tookDamage)
            {
                PlayDamageEffects();
            }
            
            // Pulse si vie basse
            UpdateLowHealthPulse(healthPercent);
        }
        
        private void AnimateHealthBar(float targetPercent, bool tookDamage)
        {
            healthTween?.Kill();
            
            // Barre principale
            healthTween = healthFill.DOFillAmount(targetPercent, fillDuration)
                .SetEase(Ease.OutQuart)
                .SetUpdate(true);
            
            // Barre de dégâts (trail)
            if (damageFill != null && tookDamage)
            {
                damageTween?.Kill();
                
                // Délai avant que le trail suive
                damageTween = DOVirtual.DelayedCall(damageTrailDelay, () =>
                {
                    damageFill.DOFillAmount(targetPercent, damageTrailDuration)
                        .SetEase(Ease.InOutQuad)
                        .SetUpdate(true);
                }, false).SetUpdate(true);
            }
            else if (damageFill != null && !tookDamage)
            {
                // Heal: le trail suit immédiatement
                damageTween?.Kill();
                damageFill.DOFillAmount(targetPercent, fillDuration)
                    .SetEase(Ease.OutQuart)
                    .SetUpdate(true);
            }
        }
        
        private void UpdateBarColor(float healthPercent)
        {
            if (healthGradient == null || healthFill == null) return;
            
            // Le gradient va de 0 (mort) à 1 (pleine vie)
            Color targetColor = healthGradient.Evaluate(healthPercent);
            healthFill.DOColor(targetColor, fillDuration).SetUpdate(true);
        }
        
        private void PlayDamageEffects()
        {
            // Shake (utilise anchoredPosition pour les éléments UI)
            if (barContainer != null)
            {
                shakeTween?.Kill();
                barContainer.anchoredPosition = originalAnchoredPosition;
                shakeTween = barContainer.DOShakeAnchorPos(shakeDuration, shakeStrength, 20, 90, false, true)
                    .SetUpdate(true)
                    .OnComplete(() => barContainer.anchoredPosition = originalAnchoredPosition);
            }
            
            // Flash blanc
            healthFill.DOColor(Color.white, flashDuration)
                .SetLoops(2, LoopType.Yoyo)
                .SetUpdate(true)
                .OnComplete(() => UpdateBarColor(targetHealth));
        }
        
        private void UpdateLowHealthPulse(float healthPercent)
        {
            if (!enableLowHealthPulse || healthFill == null) return;
            
            if (healthPercent <= criticalThreshold && healthPercent > 0)
            {
                // Pulse de couleur (rouge -> rouge clair) au lieu de scale
                if (pulseTween == null || !pulseTween.IsActive())
                {
                    Color pulseColor = new Color(1f, 0.5f, 0.5f); // Rouge clair
                    pulseTween = healthFill.DOColor(pulseColor, pulseDuration)
                        .SetLoops(-1, LoopType.Yoyo)
                        .SetEase(Ease.InOutSine)
                        .SetUpdate(true);
                }
            }
            else
            {
                // Arrêter le pulse et restaurer la couleur normale
                pulseTween?.Kill();
                UpdateBarColor(healthPercent);
            }
        }
        
        private void SetHealthImmediate(float healthPercent)
        {
            targetHealth = healthPercent;
            currentDisplayedHealth = healthPercent;
            
            if (healthFill != null)
                healthFill.fillAmount = healthPercent;
            if (damageFill != null)
                damageFill.fillAmount = healthPercent;
            
            UpdateBarColor(healthPercent);
        }
        
        private void OnBloodBulletModeChanged(bool isActive)
        {
            isInBloodBulletMode = isActive;
            
            if (isActive)
            {
                // Arrêter les autres pulses
                pulseTween?.Kill();
                
                // Pulse rouge sang
                bloodBulletTween?.Kill();
                Color brightBlood = new Color(1f, 0.3f, 0.4f);
                bloodBulletTween = healthFill.DOColor(brightBlood, bloodBulletPulseDuration)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine)
                    .SetUpdate(true);
            }
            else
            {
                // Arrêter le pulse blood bullet
                bloodBulletTween?.Kill();
                
                // Restaurer la couleur normale
                UpdateBarColor(targetHealth);
            }
        }
        
        private void KillAllTweens()
        {
            healthTween?.Kill();
            damageTween?.Kill();
            pulseTween?.Kill();
            shakeTween?.Kill();
            bloodBulletTween?.Kill();
        }
        
        private void OnDestroy()
        {
            KillAllTweens();
        }
    }
}
