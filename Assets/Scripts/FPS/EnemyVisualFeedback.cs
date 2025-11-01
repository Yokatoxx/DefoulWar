using UnityEngine;

namespace FPS
{
    /// <summary>
    /// Gère les effets visuels de l'ennemi (hit feedback, mort, etc.).
    /// </summary>
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

            // S'abonner à l'événement de dégâts
            EnemyHealth health = GetComponent<EnemyHealth>();
            if (health != null)
            {
                health.OnDeath.AddListener(OnDeath);
            }
        }

        /// <summary>
        /// Affiche un flash quand l'ennemi est touché.
        /// </summary>
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
            
            // Changer la couleur
            foreach (Renderer r in renderers)
            {
                if (r != null)
                {
                    r.material.color = hitColor;
                }
            }
            
            yield return new WaitForSeconds(hitFlashDuration);
            
            // Restaurer la couleur
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].material.color = originalColors[i];
                }
            }
            
            isFlashing = false;
        }
        
        /// <summary>
        /// Affiche visuellement l'armure sur une zone.
        /// </summary>
        public void ShowArmorOnZone(string zoneName)
        {
            switch (zoneName.ToLower())
            {
                case "head":
                    if (helmetPrefab != null)
                    {
                        Transform head = transform.Find("Head");
                        if (head != null)
                        {
                            GameObject helmet = Instantiate(helmetPrefab, head);
                            helmet.transform.localPosition = Vector3.zero;
                            helmet.transform.localRotation = Quaternion.identity;
                        }
                    }
                    break;

                case "body":
                case "chest":
                    if (vestPrefab != null)
                    {
                        Transform body = transform.Find("Body");
                        if (body != null)
                        {
                            GameObject vest = Instantiate(vestPrefab, body);
                            vest.transform.localPosition = Vector3.zero;
                            vest.transform.localRotation = Quaternion.identity;
                        }
                    }
                    break;
            }
        }

        private void OnDeath()
        {
            // Animation de mort (optionnel)
            StartCoroutine(DeathAnimation());
        }
        
        private System.Collections.IEnumerator DeathAnimation()
        {
            // Faire tomber l'ennemi
            float elapsed = 0f;
            float duration = 0.5f;
            Vector3 startScale = transform.localScale;
            
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                
                // Réduire la taille
                transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t);
                
                yield return null;
            }
        }
    }
}
