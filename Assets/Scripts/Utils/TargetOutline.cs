using UnityEngine;
using FPS;

namespace Utils
{
    /// <summary>
    /// Wrapper pour gérer l'outline d'un ennemi ciblé.
    /// Expose Show() et Hide() pour activer/désactiver l'outline proprement.
    /// S'abonne à EnemyHealth.OnDeath pour cacher l'outline automatiquement.
    /// </summary>
    public class TargetOutline : MonoBehaviour
    {
        private Outline outline;
        private EnemyHealth enemyHealth;
        private bool isShowing;

        private void Awake()
        {
            // Chercher l'Outline sur cet objet ou ses enfants
            outline = GetComponent<Outline>();
            if (outline == null)
                outline = GetComponentInChildren<Outline>();

            // S'abonner à la mort de l'ennemi pour cacher l'outline
            enemyHealth = GetComponent<EnemyHealth>();
            if (enemyHealth == null)
                enemyHealth = GetComponentInParent<EnemyHealth>();

            if (enemyHealth != null)
            {
                enemyHealth.OnDeath.AddListener(OnEnemyDeath);
            }

            // Désactiver l'outline par défaut au démarrage
            if (outline != null)
            {
                outline.enabled = false;
            }
        }

        /// <summary>
        /// Affiche l'outline avec la couleur et l'épaisseur spécifiées.
        /// </summary>
        public void Show(Color color, float width)
        {
            if (outline == null)
                return;

            outline.OutlineColor = color;
            outline.OutlineWidth = width;
            outline.enabled = true;
            isShowing = true;
        }

        /// <summary>
        /// Cache l'outline.
        /// </summary>
        public void Hide()
        {
            if (outline == null)
                return;

            outline.enabled = false;
            isShowing = false;
        }

        /// <summary>
        /// Retourne true si l'outline est actuellement visible.
        /// </summary>
        public bool IsShowing => isShowing;

        private void OnEnemyDeath()
        {
            Hide();
        }

        private void OnDisable()
        {
            // Cacher l'outline si l'objet est désactivé (pooling)
            Hide();
        }

        private void OnDestroy()
        {
            // Se désabonner de l'événement
            if (enemyHealth != null)
            {
                enemyHealth.OnDeath.RemoveListener(OnEnemyDeath);
            }
        }
    }
}

