using UnityEngine;
using FPS;
using EPOOutline;

namespace Utils
{
    /// <summary>
    /// Wrapper pour gérer l'outline d'un ennemi ciblé.
    /// Utilise Easy Performant Outline (EPOOutline.Outlinable).
    /// Active/désactive le composant Outlinable pour afficher/cacher l'outline.
    /// </summary>
    public class TargetOutline : MonoBehaviour
    {
        [Header("Default Outline Settings")]
        [SerializeField] private Color defaultColor = Color.yellow;
        [SerializeField] [Range(0f, 1f)] private float defaultDilateShift = 1f;
        
        private Outlinable outlinable;
        private EnemyHealth enemyHealth;
        private bool isShowing;

        private void Awake()
        {
            outlinable = GetComponent<Outlinable>();
            if (outlinable == null)
                outlinable = GetComponentInChildren<Outlinable>();

            enemyHealth = GetComponent<EnemyHealth>();
            if (enemyHealth == null)
                enemyHealth = GetComponentInParent<EnemyHealth>();

            if (enemyHealth != null)
            {
                enemyHealth.OnDeath.AddListener(OnEnemyDeath);
            }

            // Désactiver le composant Outlinable par défaut
            if (outlinable != null)
            {
                outlinable.enabled = false;
            }
        }

        /// <summary>
        /// Affiche l'outline avec la couleur et l'épaisseur spécifiées.
        /// </summary>
        public void Show(Color color, float width)
        {
            if (outlinable == null)
                return;

            // Configurer les paramètres
            outlinable.OutlineParameters.Color = color;
            outlinable.OutlineParameters.DilateShift = Mathf.Clamp01(width / 10f);
            outlinable.OutlineParameters.Enabled = true;
            
            // Activer le composant pour afficher l'outline
            outlinable.enabled = true;
            isShowing = true;
        }

        /// <summary>
        /// Cache l'outline.
        /// </summary>
        public void Hide()
        {
            if (outlinable == null)
                return;

            // Désactiver le composant pour cacher l'outline
            outlinable.enabled = false;
            isShowing = false;
        }

        public bool IsShowing => isShowing;

        private void OnEnemyDeath()
        {
            Hide();
        }

        private void OnDisable()
        {
            Hide();
        }

        private void OnDestroy()
        {
            if (enemyHealth != null)
            {
                enemyHealth.OnDeath.RemoveListener(OnEnemyDeath);
            }
        }
    }
}
