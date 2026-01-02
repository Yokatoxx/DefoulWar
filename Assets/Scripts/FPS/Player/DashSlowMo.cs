using UnityEngine;

namespace FPS
{
    /// <summary>
    /// Gère le slow-motion pendant les combos de dash.
    /// Extrait de DashCible pour simplifier la logique.
    /// </summary>
    public class DashSlowMo : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private float slowMoScale = 0.2f;
        [SerializeField] private float slowMoDuration = 0.75f;
        
        private float slowMoEndUnscaled;
        private float previousTimeScale = 1f;
        private bool isActive;
        
        public bool IsActive => isActive;
        public float SlowMoScale => slowMoScale;
        public float SlowMoDuration => slowMoDuration;
        
        /// <summary>
        /// Configure les paramètres depuis un DashDefinition.
        /// </summary>
        public void Configure(float scale, float duration)
        {
            slowMoScale = Mathf.Clamp(scale, 0.01f, 1f);
            slowMoDuration = Mathf.Max(0.01f, duration);
        }
        
        private void Update()
        {
            // Vérifier si le slow-mo a expiré
            if (isActive && Time.unscaledTime >= slowMoEndUnscaled)
            {
                Clear();
            }
        }
        
        /// <summary>
        /// Active ou rafraîchit le slow-mo.
        /// </summary>
        public void ApplyOrRefresh()
        {
            slowMoEndUnscaled = Time.unscaledTime + slowMoDuration;
            
            if (!isActive)
            {
                // Sauvegarder le timeScale actuel (ignorer les valeurs trop basses)
                previousTimeScale = (Time.timeScale <= 0.01f) ? 1f : Time.timeScale;
                Time.timeScale = slowMoScale;
                Time.fixedDeltaTime = 0.02f * Time.timeScale;
                isActive = true;
            }
        }
        
        /// <summary>
        /// Arrête le slow-mo et restaure le temps normal.
        /// </summary>
        public void Clear()
        {
            if (!isActive) return;
            
            float targetTimeScale = (previousTimeScale <= 0.01f) ? 1f : previousTimeScale;
            Time.timeScale = targetTimeScale;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            isActive = false;
        }
        
        private void OnDisable()
        {
            if (isActive) Clear();
        }
        
        private void OnDestroy()
        {
            if (isActive) Clear();
        }
    }
}
