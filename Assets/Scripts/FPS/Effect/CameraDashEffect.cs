using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

namespace FPS.Effect
{
    public class CameraDashEffect : MonoBehaviour
    {
        [SerializeField] private Volume volume;
        [FormerlySerializedAs("pillarDashSystem")] [SerializeField] private DashCible dashSystem;

        [Header("Lens Distortion")]
        [SerializeField] private float dashEffectLensIntensity = -0.5f;
        [SerializeField] private float dashEffectDuration = 0.2f;

        [Header("Depth Of Field - Bokeh")]
        [Tooltip("f-stop pendant le dash (plus petit = plus de flou). Utilis� si le DoF est en mode Bokeh.")]
        [SerializeField] private float dashBokehAperture = 0.4f;
        [Tooltip("Distance de mise au point pendant le dash (m�tres). Utilis� si le DoF est en mode Bokeh.")]
        [SerializeField] private float dashBokehFocusDistance = 0.5f;

        [Header("Depth Of Field - Gaussian")]
        [Tooltip("D�but du flou pendant le dash (m�tres). Utilis� si le DoF est en mode Gaussian.")]
        [SerializeField] private float dashGaussianStart = 0.1f;
        [Tooltip("Fin du flou pendant le dash (m�tres). Utilis� si le DoF est en mode Gaussian.")]
        [SerializeField] private float dashGaussianEnd = 0.5f;

        [Header("Dash Particles")]
        [Tooltip("Particle System � jouer au d�but du dash.")]
        [SerializeField] private ParticleSystem dashParticleSystem;
        [Tooltip("D�sactiver le GameObject du particle system apr�s l'arr�t.")]
        [SerializeField] private bool deactivateParticleGOOnStop = false;

        private LensDistortion lensDistortion;
        private DepthOfField depthOfField;

        // Sauvegarde des valeurs d�origine DoF
        private float initialAperture;
        private float initialFocusDistance;
        private float initialGaussianStart;
        private float initialGaussianEnd;

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
                Debug.LogError("LensDistortion non trouv� dans le Volume profile.");
            }

            if (volume.profile.TryGet(out depthOfField))
            {
                // Stocker les valeurs initiales selon le mode actif
                if (depthOfField.mode.value == DepthOfFieldMode.Bokeh)
                {
                    initialAperture = depthOfField.aperture.value;
                    initialFocusDistance = depthOfField.focusDistance.value;
                }
                else if (depthOfField.mode.value == DepthOfFieldMode.Gaussian)
                {
                    initialGaussianStart = depthOfField.gaussianStart.value;
                    initialGaussianEnd = depthOfField.gaussianEnd.value;
                }
            }
            else
            {
                Debug.LogWarning("DepthOfField non trouv� dans le Volume profile (le blur ne sera pas appliqu�).");
            }

            // S'assurer que le particle system est � l'arr�t au d�part
            StopParticles(forceClear: true);
        }

        private void Update()
        {
            bool nowDashing = dashSystem != null && dashSystem.isDashing;

            // Front montant: d�marrage des effets
            if (nowDashing && !wasDashing)
            {
                if (currentRoutine != null)
                    StopCoroutine(currentRoutine);

                PlayParticles();
                currentRoutine = StartCoroutine(PlayDashEffect());
            }

            // Front descendant: arr�t des particules
            if (!nowDashing && wasDashing)
            {
                StopParticles(forceClear: false);
            }

            wasDashing = nowDashing;
        }

        private System.Collections.IEnumerator PlayDashEffect()
        {
            float elapsed = 0f;

            // Lerp depuis la valeur "dash" vers les valeurs neutres sur la dur�e
            while (elapsed < dashEffectDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / dashEffectDuration);

                if (lensDistortion != null)
                    lensDistortion.intensity.value = Mathf.Lerp(dashEffectLensIntensity, 0f, t);

                if (depthOfField != null)
                {
                    if (depthOfField.mode.value == DepthOfFieldMode.Bokeh)
                    {
                        // Plus petit f-stop = plus de flou
                        depthOfField.aperture.value = Mathf.Lerp(dashBokehAperture, initialAperture, t);
                        depthOfField.focusDistance.value = Mathf.Lerp(dashBokehFocusDistance, initialFocusDistance, t);
                    }
                    else if (depthOfField.mode.value == DepthOfFieldMode.Gaussian)
                    {
                        depthOfField.gaussianStart.value = Mathf.Lerp(dashGaussianStart, initialGaussianStart, t);
                        depthOfField.gaussianEnd.value = Mathf.Lerp(dashGaussianEnd, initialGaussianEnd, t);
                    }
                }

                yield return null;
            }

            // R�initialisation stricte
            if (lensDistortion != null)
                lensDistortion.intensity.value = 0f;

            if (depthOfField != null)
            {
                if (depthOfField.mode.value == DepthOfFieldMode.Bokeh)
                {
                    depthOfField.aperture.value = initialAperture;
                    depthOfField.focusDistance.value = initialFocusDistance;
                }
                else if (depthOfField.mode.value == DepthOfFieldMode.Gaussian)
                {
                    depthOfField.gaussianStart.value = initialGaussianStart;
                    depthOfField.gaussianEnd.value = initialGaussianEnd;
                }
            }

            currentRoutine = null;
        }

        private void OnDisable()
        {
            // S�curit�: remettre les valeurs d�origine si le script est d�sactiv�
            if (lensDistortion != null)
                lensDistortion.intensity.value = 0f;

            if (depthOfField != null)
            {
                if (depthOfField.mode.value == DepthOfFieldMode.Bokeh)
                {
                    depthOfField.aperture.value = initialAperture;
                    depthOfField.focusDistance.value = initialFocusDistance;
                }
                else if (depthOfField.mode.value == DepthOfFieldMode.Gaussian)
                {
                    depthOfField.gaussianStart.value = initialGaussianStart;
                    depthOfField.gaussianEnd.value = initialGaussianEnd;
                }
            }

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