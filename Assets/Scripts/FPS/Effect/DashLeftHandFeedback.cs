using UnityEngine;

namespace FPS.Effect
{
    /// <summary>
    /// Placeholder pour le feedback visuel de la main gauche pendant le dash.
    /// Attach ce script à un GameObject enfant de la caméra (ex: un cube placeholder).
    /// </summary>
    public class DashLeftHandFeedback : MonoBehaviour
    {
        [Header("Références")]
        [Tooltip("Référence au système de dash. Auto-détecté si vide.")]
        [SerializeField] private DashCible dashCible;
        
        [Header("Position")]
        [Tooltip("Position de repos de la main (local space).")]
        [SerializeField] private Vector3 restPosition = new Vector3(-0.4f, -0.3f, 0.5f);
        [Tooltip("Position pendant le dash - poing en avant (local space).")]
        [SerializeField] private Vector3 punchPosition = new Vector3(-0.2f, -0.1f, 0.9f);
        
        [Header("Animation Dash")]
        [Tooltip("Durée du punch vers l'avant.")]
        [SerializeField] private float punchDuration = 0.08f;
        [Tooltip("Durée du retour à la position de repos.")]
        [SerializeField] private float returnDuration = 0.15f;
        [Tooltip("Courbe d'animation pour le punch (optionnel).")]
        [SerializeField] private AnimationCurve punchCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
        
        [Header("Rotation (optionnel)")]
        [Tooltip("Rotation de repos.")]
        [SerializeField] private Vector3 restRotation = Vector3.zero;
        [Tooltip("Rotation pendant le punch.")]
        [SerializeField] private Vector3 punchRotation = new Vector3(-15f, 0f, 10f);
        
        [Header("Effets Dash")]
        [Tooltip("Particle System à jouer pendant le dash.")]
        [SerializeField] private ParticleSystem dashParticles;
        [Tooltip("Trail Renderer à activer pendant le dash.")]
        [SerializeField] private TrailRenderer trailRenderer;
        
        [Header("Effet Impact")]
        [Tooltip("Animation shake à l'impact.")]
        [SerializeField] private bool enableImpactShake = true;
        [Tooltip("Intensité du shake à l'impact.")]
        [SerializeField] private float impactShakeIntensity = 0.1f;
        [Tooltip("Durée du shake à l'impact.")]
        [SerializeField] private float impactShakeDuration = 0.1f;
        [Tooltip("Particle System à jouer à l'impact (sur la main).")]
        [SerializeField] private ParticleSystem impactParticles;
        [Tooltip("Prefab d'effet à spawner à la position de l'ennemi touché.")]
        [SerializeField] private GameObject impactEffectPrefab;
        [Tooltip("Durée de vie de l'effet spawné à l'impact.")]
        [SerializeField] private float impactEffectLifetime = 1f;
        
        private bool wasDashing;
        private float animationTime;
        private bool isAnimating;
        private bool isPunching;
        private Vector3 animStartPos;
        private Vector3 animTargetPos;
        private Quaternion animStartRot;
        private Quaternion animTargetRot;
        
        // Impact shake
        private bool isShaking;
        private float shakeTime;
        private Vector3 shakeOffset;
        
        private void Start()
        {
            if (dashCible == null)
                dashCible = FindObjectOfType<DashCible>();
            
            transform.localPosition = restPosition;
            transform.localRotation = Quaternion.Euler(restRotation);
            
            if (trailRenderer != null)
                trailRenderer.emitting = false;
        }
        
        private void OnEnable()
        {
            DashCible.OnDashImpact += HandleDashImpact;
        }
        
        private void OnDisable()
        {
            DashCible.OnDashImpact -= HandleDashImpact;
            ResetToRest();
        }
        
        private void Update()
        {
            if (dashCible == null) return;
            
            bool nowDashing = dashCible.isDashing;
            
            if (nowDashing && !wasDashing)
            {
                StartPunchAnimation();
                PlayDashEffects();
            }
            
            if (!nowDashing && wasDashing)
            {
                StartReturnAnimation();
                StopDashEffects();
            }
            
            wasDashing = nowDashing;
            
            UpdateAnimation();
            UpdateShake();
        }
        
        private void HandleDashImpact(Vector3 impactPosition)
        {
            // Shake sur la main
            if (enableImpactShake)
            {
                isShaking = true;
                shakeTime = 0f;
            }
            
            // Particules sur la main
            if (impactParticles != null && !impactParticles.isPlaying)
                impactParticles.Play();
            
            // Effet spawné à la position de l'ennemi
            if (impactEffectPrefab != null)
            {
                var fx = Instantiate(impactEffectPrefab, impactPosition, Quaternion.identity);
                Destroy(fx, impactEffectLifetime);
            }
        }
        
        private void StartPunchAnimation()
        {
            isAnimating = true;
            isPunching = true;
            animationTime = 0f;
            animStartPos = GetBasePosition();
            animTargetPos = punchPosition;
            animStartRot = transform.localRotation;
            animTargetRot = Quaternion.Euler(punchRotation);
        }
        
        private void StartReturnAnimation()
        {
            isAnimating = true;
            isPunching = false;
            animationTime = 0f;
            animStartPos = GetBasePosition();
            animTargetPos = restPosition;
            animStartRot = transform.localRotation;
            animTargetRot = Quaternion.Euler(restRotation);
        }
        
        private Vector3 GetBasePosition()
        {
            // Retourne la position sans le shake
            return transform.localPosition - shakeOffset;
        }
        
        private void UpdateAnimation()
        {
            if (!isAnimating) return;
            
            float duration = isPunching ? punchDuration : returnDuration;
            animationTime += Time.unscaledDeltaTime;
            
            float t = Mathf.Clamp01(animationTime / duration);
            float curvedT = punchCurve.Evaluate(t);
            
            Vector3 basePos = Vector3.Lerp(animStartPos, animTargetPos, curvedT);
            transform.localPosition = basePos + shakeOffset;
            transform.localRotation = Quaternion.Slerp(animStartRot, animTargetRot, curvedT);
            
            if (t >= 1f)
            {
                isAnimating = false;
            }
        }
        
        private void UpdateShake()
        {
            if (!isShaking)
            {
                shakeOffset = Vector3.zero;
                return;
            }
            
            shakeTime += Time.unscaledDeltaTime;
            float t = shakeTime / impactShakeDuration;
            
            if (t >= 1f)
            {
                isShaking = false;
                shakeOffset = Vector3.zero;
                return;
            }
            
            // Shake qui diminue avec le temps
            float intensity = impactShakeIntensity * (1f - t);
            shakeOffset = new Vector3(
                Random.Range(-intensity, intensity),
                Random.Range(-intensity, intensity),
                Random.Range(-intensity * 0.5f, intensity * 0.5f)
            );
            
            // Appliquer le shake à la position actuelle
            if (!isAnimating)
            {
                Vector3 targetPos = wasDashing ? punchPosition : restPosition;
                transform.localPosition = targetPos + shakeOffset;
            }
        }
        
        private void PlayDashEffects()
        {
            if (dashParticles != null && !dashParticles.isPlaying)
                dashParticles.Play();
            
            if (trailRenderer != null)
                trailRenderer.emitting = true;
        }
        
        private void StopDashEffects()
        {
            if (dashParticles != null && dashParticles.isPlaying)
                dashParticles.Stop();
            
            if (trailRenderer != null)
                trailRenderer.emitting = false;
        }
        
        public void ResetToRest()
        {
            isAnimating = false;
            isShaking = false;
            shakeOffset = Vector3.zero;
            transform.localPosition = restPosition;
            transform.localRotation = Quaternion.Euler(restRotation);
            StopDashEffects();
        }
    }
}
