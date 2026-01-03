using System.Collections;
using UnityEngine;

namespace FPS
{
    /// <summary>
    /// Gère le hitstop (pause brève) lors d'un impact de dash.
    /// Extrait de DashCible pour simplifier la logique.
    /// </summary>
    public class DashHitStop : MonoBehaviour
    {
        private bool isActive;
        private float savedTimeScale = 1f;
        private float savedFixedDeltaTime = 0.02f;
        
        public bool IsActive => isActive;
        
        /// <summary>
        /// Applique un hitstop en temps réel.
        /// </summary>
        /// <param name="duration">Durée en temps réel (non affecté par timeScale)</param>
        /// <param name="freezeTime">Si true, timeScale = 0, sinon 0.01</param>
        /// <param name="onComplete">Callback appelé à la fin du hitstop</param>
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
            
            // Sauvegarder l'état actuel du temps
            savedTimeScale = (Time.timeScale <= 0.01f) ? 1f : Time.timeScale;
            savedFixedDeltaTime = Time.fixedDeltaTime;
            
            // Appliquer le freeze
            Time.timeScale = freezeTime ? 0f : 0.01f;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
            
            // Attendre en temps réel
            yield return new WaitForSecondsRealtime(duration);
            
            // Restaurer le temps
            Restore();
            
            onComplete?.Invoke();
        }
        
        /// <summary>
        /// Restaure le temps normal.
        /// </summary>
        public void Restore()
        {
            if (!isActive) return;
            
            Time.timeScale = savedTimeScale;
            Time.fixedDeltaTime = 0.02f * savedTimeScale;
            isActive = false;
        }
        
        /// <summary>
        /// Restaure vers un timeScale spécifique (utilisé quand slow-mo est actif).
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
