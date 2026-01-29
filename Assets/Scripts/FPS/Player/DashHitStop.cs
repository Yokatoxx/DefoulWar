using System.Collections;
using UnityEngine;

namespace FPS
{
    /// <summary>
    /// Gère le hitstop (pause brève) lors d'un impact de dash.
    /// Utilise HitStopSettings pour la configuration via DashSettings.
    /// </summary>
    public class DashHitStop : MonoBehaviour
    {
        private HitStopSettings config;
        private DashSlowMo dashSlowMo;
        
        private bool isActive;
        private float savedTimeScale = 1f;
        private float savedFixedDeltaTime = 0.02f;
        
        public bool IsActive => isActive;
        public bool IsEnabled => config?.enabled ?? false;
        
        private void Awake()
        {
            dashSlowMo = GetComponent<DashSlowMo>();
        }
        
        /// <summary>
        /// Configure le module avec les paramètres HitStop depuis DashSettings.
        /// </summary>
        public void Configure(HitStopSettings settings)
        {
            config = settings;
        }
        
        /// <summary>
        /// Applique un hitstop en temps réel.
        /// </summary>
        public Coroutine Apply(System.Action onComplete = null)
        {
            if (!IsEnabled || config == null || config.duration <= 0f)
            {
                onComplete?.Invoke();
                return null;
            }
            
            return StartCoroutine(HitStopCoroutine(config.duration, config.freezeTime, onComplete));
        }
        
        /// <summary>
        /// Applique un hitstop avec paramètres personnalisés (override).
        /// </summary>
        public Coroutine Apply(float duration, bool freezeTime, System.Action onComplete = null)
        {
            if (duration <= 0f)
            {
                onComplete?.Invoke();
                return null;
            }
            
            return StartCoroutine(HitStopCoroutine(duration, freezeTime, onComplete));
        }
        
        private IEnumerator HitStopCoroutine(float duration, bool freezeTime, System.Action onComplete)
        {
            isActive = true;
            
            savedTimeScale = (Time.timeScale <= 0.01f) ? 1f : Time.timeScale;
            savedFixedDeltaTime = Time.fixedDeltaTime;
            
            Time.timeScale = freezeTime ? 0f : 0.01f;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            
            yield return new WaitForSecondsRealtime(duration);
            
            Restore();
            onComplete?.Invoke();
        }
        
        /// <summary>
        /// Restaure le temps - coordonne avec SlowMo si actif.
        /// </summary>
        public void Restore()
        {
            if (!isActive) return;
            
            // Si SlowMo est actif, restaurer vers son échelle pour éviter les conflits
            if (dashSlowMo != null && dashSlowMo.IsActive)
            {
                Time.timeScale = dashSlowMo.SlowMoScale;
                Time.fixedDeltaTime = 0.02f * dashSlowMo.SlowMoScale;
            }
            else
            {
                // Restaurer à l'échelle normale (1.0) pour éviter les stales
                Time.timeScale = 1f;
                Time.fixedDeltaTime = 0.02f;
            }
            isActive = false;
        }
        
        /// <summary>
        /// Restaure vers un timeScale spécifique.
        /// </summary>
        public void RestoreTo(float targetTimeScale)
        {
            if (!isActive) return;
            
            Time.timeScale = targetTimeScale;
            Time.fixedDeltaTime = 0.02f * targetTimeScale;
            isActive = false;
        }
        
        private void OnDisable()
        {
            if (isActive) Restore();
        }
        
        private void OnDestroy()
        {
            if (isActive) Restore();
        }
    }
}
