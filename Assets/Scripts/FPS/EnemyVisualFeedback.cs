using UnityEngine;

namespace FPS
{
    public class EnemyVisualFeedback : MonoBehaviour
    {
        [Header("Hit Feedback")]
        [SerializeField] private Color hitColor = Color.red;
        [SerializeField] private float hitFlashDuration = 0.1f;
        
        [Header("Armor Visual")]
        [SerializeField] private Material armorMaterial;
        [SerializeField] private GameObject helmetPrefab;
        [SerializeField] private GameObject vestPrefab;
        
        private Renderer[] renderers;
        private Material[] originalMaterials;
        private Color[] originalColors;
        private bool isFlashing;
        
        private void Awake()
        {
            renderers = GetComponentsInChildren<Renderer>();
            originalMaterials = new Material[renderers.Length];
            originalColors = new Color[renderers.Length];
            
            for (int i = 0; i < renderers.Length; i++)
            {
                originalMaterials[i] = renderers[i].material;
                originalColors[i] = renderers[i].material.color;
            }

            EnemyHealth health = GetComponent<EnemyHealth>();
            if (health != null)
            {
                health.OnDeath.AddListener(OnDeath);
            }
        }

        public void ShowHitFeedback()
        {
            if (!isFlashing)
            {
                StartCoroutine(HitFlashCoroutine());
            }
        }
        
        private System.Collections.IEnumerator HitFlashCoroutine()
        {
            isFlashing = true;
            
            foreach (Renderer r in renderers)
            {
                if (r != null)
                {
                    r.material.color = hitColor;
                }
            }
            
            yield return new WaitForSeconds(hitFlashDuration);
            
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].material.color = originalColors[i];
                }
            }
            
            isFlashing = false;
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
