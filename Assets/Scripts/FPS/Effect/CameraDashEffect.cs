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

        [Header("Dash Particles")]
        [Tooltip("Particle System à jouer au début du dash.")]
        [SerializeField] private ParticleSystem dashParticleSystem;
        [Tooltip("Désactiver le GameObject du particle system après l'arrêt.")]
        [SerializeField] private bool deactivateParticleGOOnStop = false;

        private LensDistortion lensDistortion;
        private DepthOfField depthOfField;
        private Vignette vignette;

        // Sauvegarde des valeurs d'origine DoF
        private float initialFocusDistance;
        private float initialNearFocusStart;
        private float initialNearFocusEnd;
        private float initialFarFocusStart;
        private float initialFarFocusEnd;

        private bool wasDashing;
        private Coroutine currentRoutine;

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
                // Stocker les valeurs initiales pour HDRP
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

            // S'assurer que le particle system est à l'arrêt au départ
            StopParticles(forceClear: true);
        }

        private void Update()
        {
            bool nowDashing = dashSystem != null && (dashSystem.isDashing || dashSystem.slowMoApplied);

            // Front montant: démarrage des effets
            if (nowDashing && !wasDashing)
            {
                if (currentRoutine != null)
                    StopCoroutine(currentRoutine);

                PlayParticles();
                currentRoutine = StartCoroutine(PlayDashEffect());
            }

            // Front descendant: arrêt des particules
            if (!nowDashing && wasDashing)
            {
                StopParticles(forceClear: false);
            }

            wasDashing = nowDashing;
        }

        private System.Collections.IEnumerator PlayDashEffect()
        {
            float elapsed = 0f;

            // Lerp depuis la valeur "dash" vers les valeurs neutres sur la durée
            while (elapsed < dashEffectDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / dashEffectDuration);

                if (lensDistortion != null)
                    lensDistortion.intensity.value = Mathf.Lerp(dashEffectLensIntensity, 0f, t);

                if (depthOfField != null)
                {
                    // HDRP utilise focusDistance + nearFocus/farFocus ranges
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

        private void OnDisable()
        {
            // Sécurité: remettre les valeurs d'origine si le script est désactivé
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