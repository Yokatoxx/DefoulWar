using UnityEngine;
using System.Collections;

namespace FPS
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
        /// Déclenche un screenshake directionnel (le shake part de la direction de l'impact)
        /// </summary>
        /// <param name="attackerWorldPosition">Position mondiale de l'attaquant</param>
        /// <param name="duration">Durée du shake en secondes</param>
        /// <param name="positionMagnitude">Intensité du déplacement</param>
        /// <param name="rotationMagnitude">Intensité de la rotation (optionnel)</param>
        public void DirectionalShake(Vector3 attackerWorldPosition, float duration, float positionMagnitude, float rotationMagnitude = 0f)
        {
            StopAllCoroutines();
            StartCoroutine(DirectionalShakeCoroutine(attackerWorldPosition, duration, positionMagnitude, rotationMagnitude));
        }

        private IEnumerator DirectionalShakeCoroutine(Vector3 attackerWorldPosition, float duration, float positionMagnitude, float rotationMagnitude)
        {
            isShaking = true;
            float elapsed = 0f;
            originalPosition = cameraTransform.localPosition;
            originalRotation = cameraTransform.localRotation;
            
            duration = Mathf.Min(duration, maxShakeDuration);
            positionMagnitude = Mathf.Min(positionMagnitude * globalIntensityMultiplier, maxPositionMagnitude);
            rotationMagnitude = Mathf.Min(rotationMagnitude * globalIntensityMultiplier, maxRotationMagnitude);
            
            // Calculer la direction de l'impact en espace local caméra
            Vector3 toAttacker = (attackerWorldPosition - cameraTransform.position).normalized;
            Vector3 localDir = cameraTransform.InverseTransformDirection(toAttacker);
            Vector2 impactDir = new Vector2(localDir.x, localDir.y).normalized;
            
            if (showDebugInfo)
            {
                Debug.Log($"CameraShake: Directional shake - Direction: {impactDir}, Durée: {duration}s, Magnitude: {positionMagnitude}");
            }

            while (elapsed < duration)
            {
                float percentComplete = elapsed / duration;
                float damper = dampingCurve.Evaluate(percentComplete);
                
                // Premier quart: fort push dans la direction de l'impact
                // Reste: oscillation décroissante
                float phaseProgress = percentComplete * 4f;
                float directionalWeight;
                float randomWeight;
                
                if (phaseProgress < 1f)
                {
                    directionalWeight = 1f - phaseProgress;
                    randomWeight = phaseProgress * 0.5f;
                }
                else
                {
                    directionalWeight = 0f;
                    randomWeight = 1f;
                }
                
                // Offset directionnel (vers l'impact)
                Vector2 directionalOffset = impactDir * positionMagnitude * directionalWeight * damper;
                
                // Offset aléatoire (oscillation)
                float randomX = Random.Range(-1f, 1f) * positionMagnitude * randomWeight * damper;
                float randomY = Random.Range(-1f, 1f) * positionMagnitude * randomWeight * damper;
                
                Vector3 totalOffset = new Vector3(directionalOffset.x + randomX, directionalOffset.y + randomY, 0);
                cameraTransform.localPosition = originalPosition + totalOffset;
                
                // Rotation directionnelle (penche vers l'impact)
                if (rotationMagnitude > 0f)
                {
                    float tiltAngle = -impactDir.x * rotationMagnitude * damper;
                    float randomRoll = Random.Range(-1f, 1f) * rotationMagnitude * 0.3f * randomWeight * damper;
                    cameraTransform.localRotation = originalRotation * Quaternion.Euler(0, 0, tiltAngle + randomRoll);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            cameraTransform.localPosition = originalPosition;
            cameraTransform.localRotation = originalRotation;
            isShaking = false;
            
            if (showDebugInfo)
            {
                Debug.Log("CameraShake: Directional shake terminé");
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
