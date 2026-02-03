using UnityEngine;

namespace FPS
{
    public class EnemyVisualFeedback : MonoBehaviour
    {
        [Header("Hit Feedback")]
        [SerializeField] private Color hitColor = Color.red;
        [SerializeField] private float hitFlashDuration = 0.1f;
        
        [Header("Dash Impact Feedback")]
        [Tooltip("Couleur du flash quand touché par un dash.")]
        [SerializeField] private Color dashHitColor = new Color(1f, 0.5f, 0f); // Orange
        [Tooltip("Durée du flash dash (plus long que hit normal).")]
        [SerializeField] private float dashFlashDuration = 0.15f;
        [Tooltip("Intensité du scale punch à l'impact du dash.")]
        [SerializeField] private float dashScalePunchIntensity = 0.2f;
        [Tooltip("Durée du scale punch.")]
        [SerializeField] private float dashScalePunchDuration = 0.1f;
        [Tooltip("Particle System à jouer à l'impact du dash.")]
        [SerializeField] private ParticleSystem dashImpactParticles;
        
        [Header("Armor Visual")]
        [SerializeField] private Material armorMaterial;
        [SerializeField] private GameObject helmetPrefab;
        [SerializeField] private GameObject vestPrefab;
        
        private Renderer[] renderers;
        private Material[] originalMaterials;
        private Color[] originalColors;
        private bool isFlashing;
        private Vector3 originalScale;
        private Coroutine scaleRoutine;
        private EnemyHealth myHealth;
        
        private void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>();
            originalMaterials = new Material[renderers.Length];
            originalColors = new Color[renderers.Length];
            originalScale = transform.localScale;
            
            for (int i = 0; i < renderers.Length; i++)
            {
                originalMaterials[i] = renderers[i].material;
                originalColors[i] = renderers[i].material.color;
            }

            myHealth = GetComponent<EnemyHealth>();
            if (myHealth != null)
            {
                myHealth.OnDeath.AddListener(OnDeath);
                myHealth.OnDamageTaken.AddListener(OnDamageReceived);
            }
        }

        private void OnEnable()
        {
            DashCible.OnDashImpact += HandleDashImpact;
        }

        private void OnDisable()
        {
            DashCible.OnDashImpact -= HandleDashImpact;
        }

        private void HandleDashImpact(Vector3 impactPosition)
        {
            // Vérifier si c'est MOI qui ai été touché (position proche)
            float distToImpact = Vector3.Distance(transform.position, impactPosition);
            Debug.Log($"[EnemyVisualFeedback] {gameObject.name}: Impact reçu, distance: {distToImpact:F2}");
            
            if (distToImpact < 2f) // Seuil de proximité
            {
                Debug.Log($"[EnemyVisualFeedback] {gameObject.name}: Déclenchement du feedback dash!");
                ShowDashImpactFeedback();
            }
        }

        private void OnDamageReceived(float damage, string zoneName)
        {
            // Le feedback dash est géré par HandleDashImpact, ici on fait juste le flash normal
            if (myHealth != null && myHealth.LastHitType != DamageType.Dash)
            {
                ShowHitFeedback();
            }
        }

        public void ShowHitFeedback()
        {
            if (!isFlashing)
            {
                StartCoroutine(HitFlashCoroutine(hitColor, hitFlashDuration));
            }
        }

        public void ShowDashImpactFeedback()
        {
            // Flash couleur différente pour le dash
            if (!isFlashing)
            {
                StartCoroutine(HitFlashCoroutine(dashHitColor, dashFlashDuration));
            }
            
            // Scale punch - effet de "squash" à l'impact
            if (scaleRoutine != null)
                StopCoroutine(scaleRoutine);
            scaleRoutine = StartCoroutine(ScalePunchCoroutine());
            
            // Particules d'impact
            if (dashImpactParticles != null && !dashImpactParticles.isPlaying)
                dashImpactParticles.Play();
        }
        
        private System.Collections.IEnumerator HitFlashCoroutine(Color flashColor, float duration)
        {
            isFlashing = true;
            
            foreach (Renderer r in renderers)
            {
                if (r != null)
                {
                    r.material.color = flashColor;
                }
            }
            
            yield return new WaitForSecondsRealtime(duration);
            
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].material.color = originalColors[i];
                }
            }
            
            isFlashing = false;
        }

        private System.Collections.IEnumerator ScalePunchCoroutine()
        {
            float elapsed = 0f;
            
            // Squash: écraser sur l'axe Y, étirer sur X/Z
            Vector3 squashScale = new Vector3(
                originalScale.x * (1f + dashScalePunchIntensity),
                originalScale.y * (1f - dashScalePunchIntensity),
                originalScale.z * (1f + dashScalePunchIntensity)
            );
            
            // Phase 1: Squash rapide
            float squashDuration = dashScalePunchDuration * 0.3f;
            while (elapsed < squashDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / squashDuration;
                transform.localScale = Vector3.Lerp(originalScale, squashScale, t);
                yield return null;
            }
            
            // Phase 2: Retour avec overshoot
            elapsed = 0f;
            float returnDuration = dashScalePunchDuration * 0.7f;
            while (elapsed < returnDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / returnDuration;
                float curvedT = 1f - Mathf.Pow(1f - t, 3f);
                transform.localScale = Vector3.Lerp(squashScale, originalScale, curvedT);
                yield return null;
            }
            
            transform.localScale = originalScale;
            scaleRoutine = null;
        }
        
        private void OnDeath()
        {
            StartCoroutine(DeathAnimation());
        }
        
        private System.Collections.IEnumerator DeathAnimation()
        {
            float elapsed = 0f;
            float duration = 0.5f;
            Vector3 startScale = transform.localScale;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                
                yield return null;
            }
        }
    }
}
