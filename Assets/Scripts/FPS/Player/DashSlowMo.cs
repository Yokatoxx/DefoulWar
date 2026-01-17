using UnityEngine;
using System.Collections;

namespace FPS
{
    /// <summary>
    /// Gère le slow-motion pendant les combos de dash, avec ramp-in et ramp-out fluides.
    /// Utilise SlowMoSettings pour la configuration via DashSettings.
    /// </summary>
    public class DashSlowMo : MonoBehaviour
    {
        private SlowMoSettings config;
        
        private float slowMoEndUnscaled;
        private float previousTimeScale = 1f;
        private float originalFixedDeltaTime = 0.02f;
        private bool isActive;

        private Coroutine delayedApplyRoutine;
        private Coroutine rampOutRoutine;
        private float rampStartUnscaled;
        private float startTimeScale;
        private bool rampInDone;

        public bool IsActive => isActive;
        public float SlowMoScale => config?.scale ?? 0.2f;
        public float SlowMoDuration => config?.duration ?? 0.75f;
        public bool IsEnabled => config?.enabled ?? true;

        private void Awake()
        {
            originalFixedDeltaTime = Time.fixedDeltaTime;
        }

        /// <summary>
        /// Configure le module avec les paramètres SlowMo depuis DashSettings.
        /// </summary>
        public void Configure(SlowMoSettings settings)
        {
            config = settings;
        }

        private void Update()
        {
            if (!IsEnabled) return;
            
            // Fin du slow-mo
            if (isActive && Time.unscaledTime >= slowMoEndUnscaled)
            {
                StartRampOut();
                return;
            }

            // Ramp-in fluide
            if (isActive && !rampInDone)
            {
                float rampIn = config?.rampInTime ?? 0.2f;
                float rt = Mathf.Max(0.01f, rampIn);
                float t = Mathf.Clamp01((Time.unscaledTime - rampStartUnscaled) / rt);

                AnimationCurve curve = config?.curve;
                float k = curve != null ? Mathf.Clamp01(curve.Evaluate(t)) : t;
                float target = Mathf.Lerp(startTimeScale, SlowMoScale, k);

                Time.timeScale = target;
                Time.fixedDeltaTime = originalFixedDeltaTime * Time.timeScale;

                if (t >= 1f)
                {
                    rampInDone = true;
                    Time.timeScale = SlowMoScale;
                    Time.fixedDeltaTime = originalFixedDeltaTime * Time.timeScale;
                }
            }
        }

        public void ApplyOrRefresh()
        {
            if (!IsEnabled) return;
            
            if (rampOutRoutine != null)
            {
                StopCoroutine(rampOutRoutine);
                rampOutRoutine = null;
            }
            
            if (isActive)
            {
                slowMoEndUnscaled = Time.unscaledTime + SlowMoDuration;
                return;
            }

            if (delayedApplyRoutine != null)
            {
                StopCoroutine(delayedApplyRoutine);
                delayedApplyRoutine = null;
            }
            delayedApplyRoutine = StartCoroutine(DelayedApplySlowMo());
        }

        private IEnumerator DelayedApplySlowMo()
        {
            float delay = Mathf.Max(0f, config?.timeToStart ?? 0.8f);
            if (delay > 0f)
                yield return new WaitForSecondsRealtime(delay);

            slowMoEndUnscaled = Time.unscaledTime + SlowMoDuration;
            previousTimeScale = (Time.timeScale <= 0.01f) ? 1f : Time.timeScale;

            startTimeScale = previousTimeScale;
            rampStartUnscaled = Time.unscaledTime;
            
            float rampIn = config?.rampInTime ?? 0.2f;
            rampInDone = (rampIn <= 0.0001f);

            if (rampInDone)
            {
                Time.timeScale = SlowMoScale;
            }
            else
            {
                Time.timeScale = startTimeScale;
            }
            Time.fixedDeltaTime = originalFixedDeltaTime * Time.timeScale;
            isActive = true;

            delayedApplyRoutine = null;
        }

        private void StartRampOut()
        {
            if (rampOutRoutine != null) return;
            
            isActive = false;
            rampInDone = false;
            
            float rampOut = config?.rampOutTime ?? 0.15f;
            if (rampOut <= 0.0001f)
            {
                RestoreTimeScale();
                return;
            }
            
            rampOutRoutine = StartCoroutine(RampOutCoroutine());
        }

        private IEnumerator RampOutCoroutine()
        {
            float startScale = Time.timeScale;
            float targetScale = (previousTimeScale <= 0.01f) ? 1f : previousTimeScale;
            float elapsed = 0f;
            float duration = Mathf.Max(0.01f, config?.rampOutTime ?? 0.15f);

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                AnimationCurve curve = config?.curve;
                float k = curve != null ? Mathf.Clamp01(curve.Evaluate(t)) : t;
                
                Time.timeScale = Mathf.Lerp(startScale, targetScale, k);
                Time.fixedDeltaTime = originalFixedDeltaTime * Time.timeScale;

                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }

            RestoreTimeScale();
            rampOutRoutine = null;
        }

        private void RestoreTimeScale()
        {
            float targetTimeScale = (previousTimeScale <= 0.01f) ? 1f : previousTimeScale;
            Time.timeScale = targetTimeScale;
            Time.fixedDeltaTime = originalFixedDeltaTime * Time.timeScale;
        }

        public void Clear()
        {
            if (delayedApplyRoutine != null)
            {
                StopCoroutine(delayedApplyRoutine);
                delayedApplyRoutine = null;
            }

            if (rampOutRoutine != null)
            {
                StopCoroutine(rampOutRoutine);
                rampOutRoutine = null;
            }

            if (!isActive && rampOutRoutine == null) return;

            isActive = false;
            rampInDone = false;
            RestoreTimeScale();
        }

        private void OnDisable()
        {
            Clear();
        }

        private void OnDestroy()
        {
            Clear();
        }
    }
}
