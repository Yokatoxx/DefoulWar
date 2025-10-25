using UnityEngine;
using System.Collections;

namespace Proto3GD.FPS
{
    /// <summary>
    /// Gère les effets de screenshake pour la caméra
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        [Header("Shake Settings")]
        [Tooltip("Multiplicateur global de l'intensité du shake")]
        [SerializeField] private float globalIntensityMultiplier = 1f;
        
        [Tooltip("Durée maximale du shake (limite de sécurité)")]
        [SerializeField] private float maxShakeDuration = 2f;
        
        [Header("Position Shake")]
        [Tooltip("Intensité maximale du déplacement")]
        [SerializeField] private float maxPositionMagnitude = 0.5f;
        
        [Header("Rotation Shake")]
        [Tooltip("Intensité maximale de la rotation")]
        [SerializeField] private float maxRotationMagnitude = 5f;
        
        [Header("Damping")]
        [Tooltip("Courbe d'atténuation du shake (0-1 sur la durée)")]
        [SerializeField] private AnimationCurve dampingCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        
        [Header("Debug")]
        [Tooltip("Afficher les informations de debug dans la console")]
        [SerializeField] private bool showDebugInfo = false;
        
        private static CameraShake instance;
        public static CameraShake Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<CameraShake>();
                }
                return instance;
            }
        }

        private Transform cameraTransform;
        private Vector3 originalPosition;
        private Quaternion originalRotation;
        private bool isShaking;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }

            cameraTransform = transform;
            originalPosition = cameraTransform.localPosition;
            originalRotation = cameraTransform.localRotation;
        }

        /// <summary>
        /// Déclenche un screenshake
        /// </summary>
        /// <param name="duration">Durée du shake en secondes</param>
        /// <param name="magnitude">Intensité du shake</param>
        public void Shake(float duration, float magnitude)
        {
            if (!isShaking)
            {
                StartCoroutine(ShakeCoroutine(duration, magnitude));
            }
        }

        /// <summary>
        /// Déclenche un screenshake avec rotation
        /// </summary>
        /// <param name="duration">Durée du shake en secondes</param>
        /// <param name="positionMagnitude">Intensité du déplacement</param>
        /// <param name="rotationMagnitude">Intensité de la rotation</param>
        public void ShakeWithRotation(float duration, float positionMagnitude, float rotationMagnitude)
        {
            if (!isShaking)
            {
                StartCoroutine(ShakeWithRotationCoroutine(duration, positionMagnitude, rotationMagnitude));
            }
        }

        private IEnumerator ShakeCoroutine(float duration, float magnitude)
        {
            isShaking = true;
            float elapsed = 0f;
            originalPosition = cameraTransform.localPosition;
            
            // Appliquer les limites configurables
            duration = Mathf.Min(duration, maxShakeDuration);
            magnitude = Mathf.Min(magnitude * globalIntensityMultiplier, maxPositionMagnitude);
            
            if (showDebugInfo)
            {
                Debug.Log($"CameraShake: Shake démarré - Durée: {duration}s, Magnitude: {magnitude}");
            }

            while (elapsed < duration)
            {
                float percentComplete = elapsed / duration;
                float damper = dampingCurve.Evaluate(percentComplete);

                // Génère un déplacement aléatoire
                float offsetX = Random.Range(-1f, 1f) * magnitude * damper;
                float offsetY = Random.Range(-1f, 1f) * magnitude * damper;

                cameraTransform.localPosition = originalPosition + new Vector3(offsetX, offsetY, 0);

                elapsed += Time.deltaTime;
                yield return null;
            }

            cameraTransform.localPosition = originalPosition;
            isShaking = false;
            
            if (showDebugInfo)
            {
                Debug.Log("CameraShake: Shake terminé");
            }
        }

        private IEnumerator ShakeWithRotationCoroutine(float duration, float positionMagnitude, float rotationMagnitude)
        {
            isShaking = true;
            float elapsed = 0f;
            originalPosition = cameraTransform.localPosition;
            originalRotation = cameraTransform.localRotation;
            
            // Appliquer les limites configurables
            duration = Mathf.Min(duration, maxShakeDuration);
            positionMagnitude = Mathf.Min(positionMagnitude * globalIntensityMultiplier, maxPositionMagnitude);
            rotationMagnitude = Mathf.Min(rotationMagnitude * globalIntensityMultiplier, maxRotationMagnitude);
            
            if (showDebugInfo)
            {
                Debug.Log($"CameraShake: Shake avec rotation démarré - Durée: {duration}s, Position: {positionMagnitude}, Rotation: {rotationMagnitude}°");
            }

            while (elapsed < duration)
            {
                float percentComplete = elapsed / duration;
                float damper = dampingCurve.Evaluate(percentComplete);

                // Génère un déplacement aléatoire
                float offsetX = Random.Range(-1f, 1f) * positionMagnitude * damper;
                float offsetY = Random.Range(-1f, 1f) * positionMagnitude * damper;

                // Génère une rotation aléatoire
                float rotationZ = Random.Range(-1f, 1f) * rotationMagnitude * damper;

                cameraTransform.localPosition = originalPosition + new Vector3(offsetX, offsetY, 0);
                cameraTransform.localRotation = originalRotation * Quaternion.Euler(0, 0, rotationZ);

                elapsed += Time.deltaTime;
                yield return null;
            }

            cameraTransform.localPosition = originalPosition;
            cameraTransform.localRotation = originalRotation;
            isShaking = false;
            
            if (showDebugInfo)
            {
                Debug.Log("CameraShake: Shake avec rotation terminé");
            }
        }

        /// <summary>
        /// Arrête immédiatement le screenshake
        /// </summary>
        public void StopShake()
        {
            StopAllCoroutines();
            cameraTransform.localPosition = originalPosition;
            cameraTransform.localRotation = originalRotation;
            isShaking = false;
        }
    }
}
