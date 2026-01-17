using UnityEngine;
using UnityEngine.Events;
using System.Collections;
using FPS;

namespace Ennemies.Effect
{
    /// <summary>
    /// Animation procédurale d'attaque pour le shield : lève le bouclier puis le tape contre le sol.
    /// Attacher ce script au GameObject du shield (le mesh/cube du bouclier).
    /// </summary>
    public class ShieldAttackAnimation : MonoBehaviour
    {
        [Header("Animation Settings")]
        [Tooltip("Durée de la phase de lever du shield (secondes)")]
        [SerializeField] private float raiseTime = 0.3f;

        [Tooltip("Durée de la phase de frappe vers le sol (secondes)")]
        [SerializeField] private float slamTime = 0.15f;

        [Tooltip("Durée de pause après le slam avant de revenir (secondes)")]
        [SerializeField] private float holdTime = 0.1f;

        [Tooltip("Durée de retour à la position initiale (secondes)")]
        [SerializeField] private float returnTime = 0.25f;

        [Header("Movement Settings")]
        [Tooltip("Hauteur à laquelle le shield monte (unités)")]
        [SerializeField] private float raiseHeight = 0.8f;

        [Tooltip("Angle de rotation du shield lors de la montée (degrés)")]
        [SerializeField] private float raiseAngle = -45f;

        [Tooltip("Angle de rotation du shield lors du slam (degrés)")]
        [SerializeField] private float slamAngle = 30f;

        [Header("VFX/SFX (optionnel)")]
        [Tooltip("Prefab VFX à instancier lors de l'impact au sol")]
        [SerializeField] private GameObject impactVFXPrefab;

        [Tooltip("Offset de position pour le spawn du VFX (relatif au shield)")]
        [SerializeField] private Vector3 vfxSpawnOffset = Vector3.down * 0.5f;

        [Tooltip("Durée de vie du VFX avant destruction (0 = ne pas détruire)")]
        [SerializeField] private float vfxLifetime = 2f;

        [Tooltip("Son à jouer lors de l'impact")]
        [SerializeField] private AudioClip impactSound;

        [Tooltip("Volume du son d'impact")]
        [Range(0f, 1f)]
        [SerializeField] private float impactVolume = 0.7f;

        [Header("Screen Shake (optionnel)")]
        [Tooltip("Activer le screen shake lors de l'impact")]
        [SerializeField] private bool useScreenShake = true;

        [Tooltip("Distance maximale pour que le joueur ressente le shake")]
        [SerializeField] private float shakeMaxDistance = 8f;

        [Tooltip("Durée du screen shake")]
        [SerializeField] private float shakeDuration = 0.15f;

        [Tooltip("Intensité du déplacement de la caméra")]
        [SerializeField] private float shakePositionMagnitude = 0.05f;

        [Tooltip("Intensité de la rotation de la caméra")]
        [SerializeField] private float shakeRotationMagnitude = 1f;

        [Header("Events")]
        [Tooltip("Événement déclenché au moment de l'impact du slam")]
        public UnityEvent OnImpactHit;

        private Vector3 originalLocalPosition;
        private Quaternion originalLocalRotation;
        private bool isAnimating = false;
        private AudioSource audioSource;

        private void Awake()
        {
            originalLocalPosition = transform.localPosition;
            originalLocalRotation = transform.localRotation;

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null && impactSound != null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.spatialBlend = 1f;
                audioSource.playOnAwake = false;
            }
        }

        /// <summary>
        /// Lance l'animation d'attaque du shield.
        /// </summary>
        public void PlayAttackAnimation()
        {
            if (isAnimating) return;
            StartCoroutine(AttackAnimationCoroutine());
        }

        /// <summary>
        /// Vérifie si le shield est en train d'animer.
        /// </summary>
        public bool IsAnimating => isAnimating;

        private IEnumerator AttackAnimationCoroutine()
        {
            isAnimating = true;

            // Phase 1 : Lever le shield
            yield return StartCoroutine(RaisePhase());

            // Phase 2 : Frapper vers le sol
            yield return StartCoroutine(SlamPhase());

            // Phase 3 : Pause après l'impact
            yield return new WaitForSeconds(holdTime);

            // Phase 4 : Retour à la position initiale
            yield return StartCoroutine(ReturnPhase());

            isAnimating = false;
        }

        private IEnumerator RaisePhase()
        {
            float elapsed = 0f;
            Vector3 startPos = originalLocalPosition;
            Quaternion startRot = originalLocalRotation;

            Vector3 targetPos = originalLocalPosition + Vector3.up * raiseHeight;
            Quaternion targetRot = originalLocalRotation * Quaternion.Euler(raiseAngle, 0f, 0f);

            while (elapsed < raiseTime)
            {
                elapsed += Time.deltaTime;
                float t = EaseOutQuad(elapsed / raiseTime);

                transform.localPosition = Vector3.Lerp(startPos, targetPos, t);
                transform.localRotation = Quaternion.Slerp(startRot, targetRot, t);

                yield return null;
            }

            transform.localPosition = targetPos;
            transform.localRotation = targetRot;
        }

        private IEnumerator SlamPhase()
        {
            float elapsed = 0f;
            Vector3 startPos = transform.localPosition;
            Quaternion startRot = transform.localRotation;

            // Position de slam : légèrement plus bas que l'original pour l'effet d'impact
            Vector3 targetPos = originalLocalPosition + Vector3.down * 0.1f;
            Quaternion targetRot = originalLocalRotation * Quaternion.Euler(slamAngle, 0f, 0f);

            bool impactTriggered = false;
            const float impactTriggerPoint = 0.85f; // Déclencher à 85% du mouvement

            while (elapsed < slamTime)
            {
                elapsed += Time.deltaTime;
                float t = EaseInQuad(elapsed / slamTime);

                transform.localPosition = Vector3.Lerp(startPos, targetPos, t);
                transform.localRotation = Quaternion.Slerp(startRot, targetRot, t);

                // Déclencher l'impact quand le shield est presque au sol
                if (!impactTriggered && t >= impactTriggerPoint)
                {
                    impactTriggered = true;
                    OnImpact();
                }

                yield return null;
            }

            transform.localPosition = targetPos;
            transform.localRotation = targetRot;

            // Fallback si l'impact n'a pas été déclenché
            if (!impactTriggered)
            {
                OnImpact();
            }
        }

        private IEnumerator ReturnPhase()
        {
            float elapsed = 0f;
            Vector3 startPos = transform.localPosition;
            Quaternion startRot = transform.localRotation;

            while (elapsed < returnTime)
            {
                elapsed += Time.deltaTime;
                float t = EaseOutQuad(elapsed / returnTime);

                transform.localPosition = Vector3.Lerp(startPos, originalLocalPosition, t);
                transform.localRotation = Quaternion.Slerp(startRot, originalLocalRotation, t);

                yield return null;
            }

            transform.localPosition = originalLocalPosition;
            transform.localRotation = originalLocalRotation;
        }

        private void OnImpact()
        {
            // Déclencher l'événement pour infliger les dégâts
            OnImpactHit?.Invoke();

            // Instancier le VFX prefab
            if (impactVFXPrefab != null)
            {
                Vector3 spawnPos = transform.position + vfxSpawnOffset;
                GameObject vfx = Instantiate(impactVFXPrefab, spawnPos, Quaternion.identity);
                
                if (vfxLifetime > 0f)
                {
                    Destroy(vfx, vfxLifetime);
                }
            }

            if (impactSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(impactSound, impactVolume);
            }

            // Screen shake si le joueur est proche
            TriggerScreenShake();
        }

        private void TriggerScreenShake()
        {
            if (!useScreenShake) return;
            if (CameraShake.Instance == null) return;

            // Trouver le joueur
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null) return;

            float distance = Vector3.Distance(transform.position, playerObj.transform.position);
            
            if (distance <= shakeMaxDistance)
            {
                // Atténuer le shake en fonction de la distance (plus proche = plus fort)
                float distanceFactor = 1f - (distance / shakeMaxDistance);
                distanceFactor = Mathf.Clamp01(distanceFactor);

                float finalPosMag = shakePositionMagnitude * distanceFactor;
                float finalRotMag = shakeRotationMagnitude * distanceFactor;

                CameraShake.Instance.ShakeWithRotation(shakeDuration, finalPosMag, finalRotMag);
            }
        }

        // Easing pour la montée (ralentit à la fin)
        private float EaseOutQuad(float t)
        {
            return 1f - (1f - t) * (1f - t);
        }

        // Easing pour le slam (accélère pour plus d'impact)
        private float EaseInQuad(float t)
        {
            return t * t;
        }

        /// <summary>
        /// Reset immédiat à la position d'origine (utile si l'ennemi meurt pendant l'animation)
        /// </summary>
        public void ResetImmediate()
        {
            StopAllCoroutines();
            transform.localPosition = originalLocalPosition;
            transform.localRotation = originalLocalRotation;
            isAnimating = false;
        }
    }
}
