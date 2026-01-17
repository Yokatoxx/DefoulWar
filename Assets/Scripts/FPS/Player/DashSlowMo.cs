using UnityEngine;
using System.Collections;

namespace FPS
{
    /// <summary>
    /// Gère le slow-motion pendant les combos de dash, avec délai et ramp-in fluide via courbe.
    /// </summary>
    public class DashSlowMo : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private float slowMoScale = 0.2f;
        [SerializeField] private float slowMoDuration = 0.75f;
        [SerializeField] private float timeToStart = 0.8f;

        [Header("Courbe de transition")]
        [Tooltip("Courbe d’animation utilisée pour lisser l’entrée du slow-mo (0 -> 1).")]
        [SerializeField] private AnimationCurve slowMoCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        [Tooltip("Durée (en secondes réelles) du ramp-in vers le timeScale de slow-mo.")]
        [SerializeField] private float rampInTime = 0.2f;

        private float slowMoEndUnscaled;
        private float previousTimeScale = 1f;
        private bool isActive;

        // Gestion du délai et du ramp-in
        private Coroutine delayedApplyRoutine;
        private float rampStartUnscaled;
        private float startTimeScale; // timeScale au moment du début de l’entrée
        private bool rampInDone;

        public bool IsActive => isActive;
        public float SlowMoScale => slowMoScale;
        public float SlowMoDuration => slowMoDuration;

        /// Configure depuis un DashDefinition (optionnel)
        public void Configure(float scale, float duration)
        {
            slowMoScale = Mathf.Clamp(scale, 0.01f, 1f);
            slowMoDuration = Mathf.Max(0.01f, duration);
        }

        private void Update()
        {
            // Terminaison du slow-mo à la fin de la fenêtre
            if (isActive && Time.unscaledTime >= slowMoEndUnscaled)
            {
                Clear();
                return;
            }

            // Ramp-in fluide du timeScale pendant la phase d’entrée
            if (isActive && !rampInDone)
            {
                float rt = Mathf.Max(0.01f, rampInTime);
                float t = Mathf.Clamp01((Time.unscaledTime - rampStartUnscaled) / rt);

                // Évaluer la courbe pour obtenir un facteur lissé
                float k = slowMoCurve != null ? Mathf.Clamp01(slowMoCurve.Evaluate(t)) : t;

                // Interpoler entre le timeScale de départ et le slowMoScale
                float target = Mathf.Lerp(startTimeScale, slowMoScale, k);

                // Appliquer
                Time.timeScale = target;
                Time.fixedDeltaTime = 0.02f * Time.timeScale;

                if (t >= 1f)
                {
                    // Fin du ramp-in
                    rampInDone = true;
                    Time.timeScale = slowMoScale;
                    Time.fixedDeltaTime = 0.02f * Time.timeScale;
                }
            }
        }

        /// Demande l’application/rafraîchissement du slow-mo (avec délai + ramp-in).
        public void ApplyOrRefresh()
        {
            if (isActive)
            {
                // Rafraîchir la durée uniquement (ne pas relancer ramp-in si déjà actif)
                slowMoEndUnscaled = Time.unscaledTime + slowMoDuration;
                return;
            }

            // Lancer une application différée
            if (delayedApplyRoutine != null)
            {
                StopCoroutine(delayedApplyRoutine);
                delayedApplyRoutine = null;
            }
            delayedApplyRoutine = StartCoroutine(DelayedApplySlowMo());
        }

        private IEnumerator DelayedApplySlowMo()
        {
            float delay = Mathf.Max(0f, timeToStart);
            if (delay > 0f)
                yield return new WaitForSecondsRealtime(delay);

            // Début du slow-mo: préparer ramp-in
            slowMoEndUnscaled = Time.unscaledTime + slowMoDuration;

            // Sauvegarder le timeScale actuel (ignorer valeurs trop basses)
            previousTimeScale = (Time.timeScale <= 0.01f) ? 1f : Time.timeScale;

            // Initialiser ramp-in
            startTimeScale = previousTimeScale;
            rampStartUnscaled = Time.unscaledTime;
            rampInDone = (rampInTime <= 0.0001f);

            // Appliquer immédiatement une première valeur
            if (rampInDone)
            {
                Time.timeScale = slowMoScale;
            }
            else
            {
                // Départ à startTimeScale; Update s’occupera d’interpoler
                Time.timeScale = startTimeScale;
            }
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            isActive = true;

            delayedApplyRoutine = null;
        }

        /// Arrête le slow-mo et restaure le temps normal (instantané).
        public void Clear()
        {
            // Annuler une application différée en cours
            if (delayedApplyRoutine != null)
            {
                StopCoroutine(delayedApplyRoutine);
                delayedApplyRoutine = null;
            }

            if (!isActive) return;

            // Restauration instantanée (si tu veux un ramp-out, je peux l’ajouter)
            float targetTimeScale = (previousTimeScale <= 0.01f) ? 1f : previousTimeScale;
            Time.timeScale = targetTimeScale;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            isActive = false;
            rampInDone = false;
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
