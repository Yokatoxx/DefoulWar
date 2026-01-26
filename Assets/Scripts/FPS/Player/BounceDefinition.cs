using UnityEngine;

namespace FPS
{
    [CreateAssetMenu(fileName = "BounceDefinition", menuName = "FPS/Bounce Definition")]
    public class BounceDefinition : ScriptableObject
    {
        [Header("Direction")]
        [Tooltip("Si true, utilise l'inverse de la direction du dash comme base du bounce")]
        public bool useDashDirectionAsBounce = false;
        
        [Tooltip("Direction statique du bounce (utilisée si useDashDirectionAsBounce est false)")]
        public Vector3 direction = new Vector3(0f, 0.85f, -0.35f);
        public bool directionIsLocal = true;
        
        [Tooltip("Composante verticale ajoutée au bounce dynamique (utilisée si useDashDirectionAsBounce est true)")]
        [Range(0f, 1f)] public float verticalComponent = 0.5f;

        [Header("Force")]
        [Min(0f)] public float force = 18f;

        [Header("Courbe de rebond")]
        [Tooltip("Courbe d'application de la force sur la durée (X: temps normalisé 0-1, Y: multiplicateur de force)")]
        public AnimationCurve forceOverTime = AnimationCurve.Linear(0f, 1f, 1f, 0f);
        [Tooltip("Durée du rebond en secondes (0 = impulsion instantanée)")]
        [Min(0f)] public float duration = 0.25f;
    }
}

