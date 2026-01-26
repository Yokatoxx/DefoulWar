using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace FPS.UI
{
    public class AmmoArcHUD : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, Tooltip("Référence au WeaponSystem pour écouter les changements de munitions")]
        private WeaponSystem weaponSystem;
        
        [SerializeField, Tooltip("Liste des 6 segments représentant les munitions (dans l'ordre)")]
        private List<Image> ammoSegments = new();

        [Header("Animation Settings")]
        [SerializeField, Tooltip("Durée du fade out quand on tire")]
        private float fadeOutDuration = 0.15f;
        
        [SerializeField, Tooltip("Durée du fade in quand on recharge")]
        private float fadeInDuration = 0.1f;
        
        [SerializeField, Tooltip("Délai entre chaque segment lors du reload (effet cascade)")]
        private float reloadStagger = 0.05f;
        
        [SerializeField, Tooltip("Activer l'effet de pop au reload")]
        private bool enablePopEffect = true;
        
        [SerializeField, Tooltip("Intensité du pop (1.2 = 20% plus grand)")]
        [Range(1f, 1.5f)]
        private float popScale = 1.2f;
        
        [SerializeField, Tooltip("Durée de l'animation de pop")]
        private float popDuration = 0.15f;

        [Header("Visual")]
        [SerializeField, Tooltip("Couleur quand le segment est actif")]
        private Color activeColor = Color.white;
        
        [SerializeField, Tooltip("Couleur quand on est en mode Blood Bullets")]
        private Color bloodColor = new Color(0.8f, 0.1f, 0.1f, 1f);

        private int lastKnownMagazine = -1;
        private List<Vector3> originalScales = new();
        private bool isInitialized;

        private void Awake()
        {
            CacheOriginalScales();
        }

        private void Start()
        {
            if (weaponSystem == null)
                weaponSystem = FindFirstObjectByType<WeaponSystem>();
            
            if (weaponSystem != null)
            {
                weaponSystem.OnMagazineChanged.AddListener(OnMagazineChanged);
                weaponSystem.OnReloadComplete.AddListener(OnReloadComplete);
                
                // Init avec état actuel
                lastKnownMagazine = weaponSystem.CurrentMagazine;
                RefreshAllSegments(lastKnownMagazine, false);
                isInitialized = true;
            }
            else
            {
                Debug.LogWarning("[AmmoArcHUD] WeaponSystem non trouvé!");
            }
        }

        private void CacheOriginalScales()
        {
            originalScales.Clear();
            foreach (var segment in ammoSegments)
            {
                if (segment != null)
                    originalScales.Add(segment.transform.localScale);
                else
                    originalScales.Add(Vector3.one);
            }
        }

        private void OnMagazineChanged(int currentMagazine)
        {
            if (!isInitialized) return;
            
            // On a tiré : certains segments doivent disparaître
            if (currentMagazine < lastKnownMagazine)
            {
                // Fade out les segments qui viennent d'être consommés
                for (int i = currentMagazine; i < lastKnownMagazine && i < ammoSegments.Count; i++)
                {
                    FadeOutSegment(i);
                }
            }
            // Le chargeur a augmenté (rare, mais possible via AddAmmo direct dans le mag)
            else if (currentMagazine > lastKnownMagazine)
            {
                for (int i = lastKnownMagazine; i < currentMagazine && i < ammoSegments.Count; i++)
                {
                    FadeInSegment(i, 0f);
                }
            }
            
            lastKnownMagazine = currentMagazine;
            
            // Vérifier si on est en mode Blood Bullets
            UpdateBloodBulletsVisual();
        }

        private void OnReloadComplete()
        {
            if (!isInitialized || weaponSystem == null) return;
            
            int newMagazine = weaponSystem.CurrentMagazine;
            
            // Animer seulement les segments qui étaient désactivés (munitions rechargées)
            int reloadedCount = 0;
            for (int i = lastKnownMagazine; i < newMagazine && i < ammoSegments.Count; i++)
            {
                float delay = reloadedCount * reloadStagger;
                FadeInSegment(i, delay);
                reloadedCount++;
            }
            
            lastKnownMagazine = newMagazine;
            UpdateBloodBulletsVisual();
        }

        private void FadeOutSegment(int index)
        {
            if (index < 0 || index >= ammoSegments.Count) return;
            
            Image segment = ammoSegments[index];
            if (segment == null) return;
            
            // Kill toute animation en cours sur ce segment
            segment.DOKill();
            segment.transform.DOKill();
            
            // Effet "depop" : grossit puis disparaît instantanément
            Sequence seq = DOTween.Sequence();
            seq.Append(segment.transform.DOScale(originalScales[index] * 1.3f, 0.05f).SetEase(Ease.OutQuad));
            seq.AppendCallback(() => {
                segment.gameObject.SetActive(false);
                segment.transform.localScale = originalScales[index];
            });
        }

        private void FadeInSegment(int index, float delay)
        {
            if (index < 0 || index >= ammoSegments.Count) return;
            
            Image segment = ammoSegments[index];
            if (segment == null) return;
            
            // Réactiver le segment s'il était désactivé
            segment.gameObject.SetActive(true);
            
            // Kill toute animation en cours
            segment.DOKill();
            segment.transform.DOKill();
            
            // Reset scale pour le fade in
            segment.transform.localScale = originalScales[index] * 0.5f;
            segment.color = new Color(activeColor.r, activeColor.g, activeColor.b, 0f);
            
            Sequence seq = DOTween.Sequence();
            seq.SetDelay(delay);
            
            // Fade in avec pop
            seq.Append(segment.DOFade(1f, fadeInDuration).SetEase(Ease.OutQuad));
            seq.Join(segment.transform.DOScale(originalScales[index], fadeInDuration).SetEase(Ease.OutBack));
            
            // Pop effect
            if (enablePopEffect)
            {
                seq.Append(segment.transform.DOScale(originalScales[index] * popScale, popDuration * 0.5f).SetEase(Ease.OutQuad));
                seq.Append(segment.transform.DOScale(originalScales[index], popDuration * 0.5f).SetEase(Ease.OutBounce));
            }
        }

        private void RefreshAllSegments(int magazineCount, bool animate)
        {
            for (int i = 0; i < ammoSegments.Count; i++)
            {
                Image segment = ammoSegments[i];
                if (segment == null) continue;
                
                bool isActive = i < magazineCount;
                
                if (animate)
                {
                    if (isActive)
                        FadeInSegment(i, i * reloadStagger);
                    else
                        FadeOutSegment(i);
                }
                else
                {
                    // Setup immédiat sans animation
                    Color c = activeColor;
                    c.a = isActive ? 1f : 0f;
                    segment.color = c;
                    segment.transform.localScale = originalScales[i];
                }
            }
        }

        private void UpdateBloodBulletsVisual()
        {
            if (weaponSystem == null) return;
            
            // Si on est en mode Blood Bullets, teinter les segments restants en rouge
            bool isBlood = weaponSystem.IsUsingBloodBullets;
            Color targetColor = isBlood ? bloodColor : activeColor;
            
            foreach (var segment in ammoSegments)
            {
                if (segment != null && segment.color.a > 0.5f)
                {
                    segment.DOColor(targetColor, 0.2f);
                }
            }
        }

        private void OnDestroy()
        {
            if (weaponSystem != null)
            {
                weaponSystem.OnMagazineChanged.RemoveListener(OnMagazineChanged);
                weaponSystem.OnReloadComplete.RemoveListener(OnReloadComplete);
            }
            
            // Nettoyer les tweens
            foreach (var segment in ammoSegments)
            {
                if (segment != null)
                {
                    segment.DOKill();
                    segment.transform.DOKill();
                }
            }
        }
    }
}
