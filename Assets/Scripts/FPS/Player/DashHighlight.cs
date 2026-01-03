using UnityEngine;
using Utils;

namespace FPS
{
    /// <summary>
    /// Gère le highlight/outline des cibles visées pour le dash.
    /// Extrait de DashCible pour simplifier la logique.
    /// </summary>
    public class DashHighlight : MonoBehaviour
    {
        [Header("Outline Settings")]
        [SerializeField] private Color outlineColor = Color.yellow;
        [Range(0f, 10f)]
        [SerializeField] private float outlineWidth = 2f;
        [SerializeField] private bool showDuringSlowMo = true;
        
        private TargetOutline currentOutline;
        private EnemyHealth currentTarget;
        
        public Color OutlineColor => outlineColor;
        public float OutlineWidth => outlineWidth;
        public bool ShowDuringSlowMo => showDuringSlowMo;
        public EnemyHealth CurrentTarget => currentTarget;
        
        /// <summary>
        /// Met à jour l'outline en fonction de la cible visée et des conditions d'affichage.
        /// </summary>
        /// <param name="aimedTarget">L'ennemi actuellement visé</param>
        /// <param name="canShow">Si l'outline peut être affiché (cooldown ready ou slow-mo)</param>
        public void UpdateHighlight(EnemyHealth aimedTarget, bool canShow)
        {
            // Si la cible a changé
            if (aimedTarget != currentTarget)
            {
                // Cacher l'outline de l'ancienne cible
                HideCurrentOutline();
                
                currentTarget = aimedTarget;
                
                // Afficher l'outline sur la nouvelle cible si conditions remplies
                if (aimedTarget != null && canShow)
                {
                    ShowOutlineOn(aimedTarget);
                }
            }
            else if (aimedTarget != null)
            {
                // Même cible, vérifier si l'état d'affichage doit changer
                if (canShow)
                {
                    if (currentOutline == null)
                    {
                        ShowOutlineOn(aimedTarget);
                    }
                    else if (!currentOutline.IsShowing)
                    {
                        currentOutline.Show(outlineColor, outlineWidth);
                    }
                }
                else
                {
                    if (currentOutline != null && currentOutline.IsShowing)
                    {
                        currentOutline.Hide();
                    }
                }
            }
            else
            {
                HideCurrentOutline();
            }
        }
        
        private void ShowOutlineOn(EnemyHealth target)
        {
            currentOutline = target.GetComponent<TargetOutline>();
            if (currentOutline == null)
                currentOutline = target.GetComponentInChildren<TargetOutline>();
            
            if (currentOutline != null)
            {
                currentOutline.Show(outlineColor, outlineWidth);
            }
        }
        
        private void HideCurrentOutline()
        {
            if (currentOutline != null)
            {
                currentOutline.Hide();
                currentOutline = null;
            }
            currentTarget = null;
        }
        
        /// <summary>
        /// Force la désactivation de l'outline actuel.
        /// </summary>
        public void ClearHighlight()
        {
            HideCurrentOutline();
        }
        
        private void OnDisable()
        {
            ClearHighlight();
        }
        
        private void OnDestroy()
        {
            ClearHighlight();
        }
    }
}
