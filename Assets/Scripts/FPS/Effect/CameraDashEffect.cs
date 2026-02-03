using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Serialization;

namespace FPS.Effect
{
    public class CameraDashEffect : MonoBehaviour
    {
        [SerializeField] private Volume volume;
        [FormerlySerializedAs("pillarDashSystem")][SerializeField] private DashCible dashSystem;

        [Header("Lens Distortion")]
        [SerializeField] private float dashEffectLensIntensity = -0.5f;
        [SerializeField] private float dashEffectDuration = 0.2f;

        [Header("Depth Of Field - HDRP")]
        [Tooltip("Distance de mise au point pendant le dash (mètres).")]
        [SerializeField] private float dashFocusDistance = 0.5f;
        [Tooltip("Début du flou proche pendant le dash (mètres).")]
        [SerializeField] private float dashNearFocusStart = 0.0f;
        [Tooltip("Fin du flou proche pendant le dash (mètres).")]
        [SerializeField] private float dashNearFocusEnd = 0.3f;
        [Tooltip("Début du flou éloigné pendant le dash (mètres).")]
        [SerializeField] private float dashFarFocusStart = 0.5f;
        [Tooltip("Fin du flou éloigné pendant le dash (mètres).")]
        [SerializeField] private float dashFarFocusEnd = 2.0f;

        [Header("Vignette")]
        [SerializeField] private float dashVignetteIntensity = 0.4f;
        [SerializeField] private float dashVignetteDuration = 0.2f;

        [Header("Chromatic Aberration - Impact")]
        [Tooltip("Activer l'aberration chromatique à l'impact du dash.")]
        [SerializeField] private bool enableImpactChromaticAberration = true;
        [Tooltip("Intensité de l'aberration chromatique à l'impact (0-1).")]
        [Range(0f, 1f)]
        [SerializeField] private float impactChromaticIntensity = 0.8f;
        [Tooltip("Durée du flash d'aberration chromatique.")]
        [SerializeField] private float impactChromaticDuration = 0.08f;

        [Header("Dash Particles")]
        [Tooltip("Particle System à jouer au début du dash.")]
        [SerializeField] private ParticleSystem dashParticleSystem;
        [Tooltip("Désactiver le GameObject du particle system après l'arrêt.")]
        [SerializeField] private bool deactivateParticleGOOnStop = false;

        private LensDistortion lensDistortion;
        private DepthOfField depthOfField;
        private Vignette vignette;
        private ChromaticAberration chromaticAberration;

        // Sauvegarde des valeurs d'origine
        private float initialFocusDistance;
        private float initialNearFocusStart;
        private float initialNearFocusEnd;
        private float initialFarFocusStart;
        private float initialFarFocusEnd;
        private float initialChromaticIntensity;

        private bool wasDashing;
        private Coroutine currentRoutine;
        private Coroutine chromaticRoutine;

        private void Start()
        {
            if (volume == null)
                volume = GetComponent<Volume>();

            if (volume == null || volume.profile == null)
            {
                Debug.LogError("Volume ou Volume.profile introuvable.");
                return;
            }

            if (volume.profile.TryGet(out lensDistortion))
            {
                lensDistortion.intensity.value = 0f;
            }
            else
            {
                Debug.LogError("LensDistortion non trouvé dans le Volume profile.");
            }

            if (volume.profile.TryGet(out depthOfField))
            {
                initialFocusDistance = depthOfField.focusDistance.value;
                initialNearFocusStart = depthOfField.nearFocusStart.value;
                initialNearFocusEnd = depthOfField.nearFocusEnd.value;
                initialFarFocusStart = depthOfField.farFocusStart.value;
                initialFarFocusEnd = depthOfField.farFocusEnd.value;
            }
            else
            {
                Debug.LogWarning("DepthOfField non trouvé dans le Volume profile (le blur ne sera pas appliqué).");
            }

            if(volume.profile.TryGet(out vignette))
            {
                vignette.intensity.value = 0f;
            }
            else
            {
                Debug.LogWarning("Vignette non trouvé dans le Volume profile (la vignette ne sera pas appliquée).");
            }

            if (volume.profile.TryGet(out chromaticAberration))
            {
                initialChromaticIntensity = chromaticAberration.intensity.value;
            }
            else
            {
                Debug.LogWarning("ChromaticAberration non trouvé dans le Volume profile.");
            }

            StopParticles(forceClear: true);
        }

        private void OnEnable()
        {
            DashCible.OnDashImpact += HandleDashImpact;
        }

        private void OnDisable()
        {
            DashCible.OnDashImpact -= HandleDashImpact;
            ResetAllEffects();
        }

        private void Update()
        {
            bool nowDashing = dashSystem != null && (dashSystem.isDashing || dashSystem.slowMoApplied);

            if (nowDashing && !wasDashing)
            {
                if (currentRoutine != null)
                    StopCoroutine(currentRoutine);

                PlayParticles();
                currentRoutine = StartCoroutine(PlayDashEffect());
            }

            if (!nowDashing && wasDashing)
            {
                StopParticles(forceClear: false);
            }

            wasDashing = nowDashing;
        }

        private void HandleDashImpact(Vector3 impactPosition)
        {
            Debug.Log($"[CameraDashEffect] Impact reçu à {impactPosition}");
            
            if (!enableImpactChromaticAberration)
            {
                Debug.Log("[CameraDashEffect] Chromatic Aberration désactivée dans l'inspector");
                return;
            }
            
            if (chromaticAberration == null)
            {
                Debug.LogWarning("[CameraDashEffect] ChromaticAberration est null! Ajoute 'Chromatic Aberration' dans ton Volume Profile HDRP.");
                return;
            }

            if (chromaticRoutine != null)
                StopCoroutine(chromaticRoutine);

            chromaticRoutine = StartCoroutine(PlayChromaticFlash());
        }

        private System.Collections.IEnumerator PlayChromaticFlash()
        {
            float elapsed = 0f;

            while (elapsed < impactChromaticDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / impactChromaticDuration;
                
                // Flash rapide puis retour à la normale
                chromaticAberration.intensity.value = Mathf.Lerp(impactChromaticIntensity, initialChromaticIntensity, t);

                yield return null;
            }

            chromaticAberration.intensity.value = initialChromaticIntensity;
            chromaticRoutine = null;
        }

        private System.Collections.IEnumerator PlayDashEffect()
        {
            float elapsed = 0f;

            while (elapsed < dashEffectDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / dashEffectDuration);

                if (lensDistortion != null)
                    lensDistortion.intensity.value = Mathf.Lerp(dashEffectLensIntensity, 0f, t);

                if (depthOfField != null)
                {
                    depthOfField.focusDistance.value = Mathf.Lerp(dashFocusDistance, initialFocusDistance, t);
                    depthOfField.nearFocusStart.value = Mathf.Lerp(dashNearFocusStart, initialNearFocusStart, t);
                    depthOfField.nearFocusEnd.value = Mathf.Lerp(dashNearFocusEnd, initialNearFocusEnd, t);
                    depthOfField.farFocusStart.value = Mathf.Lerp(dashFarFocusStart, initialFarFocusStart, t);
                    depthOfField.farFocusEnd.value = Mathf.Lerp(dashFarFocusEnd, initialFarFocusEnd, t);
                }

                if (vignette != null)
                    vignette.intensity.value = Mathf.Lerp(dashVignetteIntensity, 0f, t);

                yield return null;
            }

            // Réinitialisation stricte
            if (lensDistortion != null)
                lensDistortion.intensity.value = 0f;

            if (depthOfField != null)
            {
                depthOfField.focusDistance.value = initialFocusDistance;
                depthOfField.nearFocusStart.value = initialNearFocusStart;
                depthOfField.nearFocusEnd.value = initialNearFocusEnd;
                depthOfField.farFocusStart.value = initialFarFocusStart;
                depthOfField.farFocusEnd.value = initialFarFocusEnd;
            }

            if(vignette != null)
                vignette.intensity.value = 0f;

            currentRoutine = null;
        }

        private void ResetAllEffects()
        {
            if (lensDistortion != null)
                lensDistortion.intensity.value = 0f;

            if (depthOfField != null)
            {
                depthOfField.focusDistance.value = initialFocusDistance;
                depthOfField.nearFocusStart.value = initialNearFocusStart;
                depthOfField.nearFocusEnd.value = initialNearFocusEnd;
                depthOfField.farFocusStart.value = initialFarFocusStart;
                depthOfField.farFocusEnd.value = initialFarFocusEnd;
            }

            if (vignette != null)
                vignette.intensity.value = 0f;

            if (chromaticAberration != null)
                chromaticAberration.intensity.value = initialChromaticIntensity;

            StopParticles(forceClear: true);
        }

        private void PlayParticles()
        {
            if (dashParticleSystem == null) return;

            if (!dashParticleSystem.gameObject.activeSelf)
                dashParticleSystem.gameObject.SetActive(true);

            if (!dashParticleSystem.isPlaying)
                dashParticleSystem.Play(true);
        }

        private void StopParticles(bool forceClear)
        {
            if (dashParticleSystem == null) return;

            if (dashParticleSystem.isPlaying)
            {
                var stopMode = forceClear ? ParticleSystemStopBehavior.StopEmittingAndClear
                                          : ParticleSystemStopBehavior.StopEmitting;
                dashParticleSystem.Stop(true, stopMode);
            }

            if (deactivateParticleGOOnStop && dashParticleSystem.gameObject.activeSelf)
                dashParticleSystem.gameObject.SetActive(false);
        }
    }
}