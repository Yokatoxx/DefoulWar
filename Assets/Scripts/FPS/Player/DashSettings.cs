using UnityEngine;

namespace FPS
{
    [CreateAssetMenu(fileName = "DashSettings", menuName = "FPS/Dash Settings")]
    public class DashSettings : ScriptableObject
    {
        [Header("Ciblage")]
        public LayerMask enemyMask = ~0;
        public LayerMask obstacleMask = ~0;
        [Min(0f)] public float maxAimAngle = 30f;
        [Min(0.5f)] public float distanceDash = 25f;

        [Header("Dash")]
        [Min(1)] public int countDash = 3;
        [Min(0f)] public float cooldown = 1.5f;
        [Min(0f)] public float dashDamage = 9999f;
        [Min(0.01f)] public float dashTravelTime = 0.08f;
        [Min(0f)] public float capsuleRadius = 0.4f;
        [Min(0f)] public float stopOffset = 1f;
        [Tooltip("Durée pendant laquelle le joueur ne peut pas tirer après un dash")]
        [Min(0f)] public float postDashNoFireDuration = 0.1f;

        [Header("Slow-Mo")]
        public SlowMoSettings slowMo = new SlowMoSettings();

        [Header("Bounce Joueur")]
        public BounceSettings groundBounce = new BounceSettings();
        public BounceSettings airBounce = new BounceSettings();

        [Header("HitStop")]
        public HitStopSettings hitStop = new HitStopSettings();

        [Header("Knockback Ennemi")]
        public KnockbackSettings knockback = new KnockbackSettings();

        [Header("ScreenShake Impact")]
        public ScreenShakeSettings screenShake = new ScreenShakeSettings();
    }

    [System.Serializable]
    public class SlowMoSettings
    {
        [Tooltip("Active le slow-motion après un dash réussi")]
        public bool enabled = true;
        
        [Range(0.01f, 1f)]
        [Tooltip("Échelle du temps pendant le slow-mo (0.2 = 5x plus lent)")]
        public float scale = 0.2f;
        
        [Min(0.01f)]
        [Tooltip("Durée du slow-mo en secondes réelles")]
        public float duration = 0.75f;
        
        [Min(0f)]
        [Tooltip("Délai avant l'activation du slow-mo")]
        public float timeToStart = 0.8f;
        
        [Min(0f)]
        [Tooltip("Durée du ramp-in (transition vers slow-mo)")]
        public float rampInTime = 0.2f;
        
        [Min(0f)]
        [Tooltip("Durée du ramp-out (transition vers temps normal)")]
        public float rampOutTime = 0.15f;
        
        [Tooltip("Courbe de transition pour le slow-mo")]
        public AnimationCurve curve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    }

    [System.Serializable]
    public class BounceSettings
    {
        [Header("Direction")]
        [Tooltip("Utilise l'inverse de la direction du dash comme base")]
        public bool useDashDirectionAsBounce = false;
        
        [Tooltip("Direction statique (si useDashDirectionAsBounce est false)")]
        public Vector3 direction = new Vector3(0f, 0.85f, -0.35f);
        public bool directionIsLocal = true;
        
        [Range(0f, 1f)]
        [Tooltip("Composante verticale ajoutée au bounce dynamique")]
        public float verticalComponent = 0.5f;

        [Header("Force")]
        [Min(0f)]
        public float force = 18f;

        [Header("Courbe")]
        [Tooltip("Application de la force sur la durée (X: temps 0-1, Y: multiplicateur)")]
        public AnimationCurve forceOverTime = AnimationCurve.Linear(0f, 1f, 1f, 0f);
        
        [Min(0f)]
        [Tooltip("Durée du rebond (0 = impulsion instantanée)")]
        public float duration = 0.25f;
    }

    [System.Serializable]
    public class HitStopSettings
    {
        [Tooltip("Active le hitstop à l'impact")]
        public bool enabled = false;
        
        [Min(0f)]
        [Tooltip("Durée du hitstop en temps réel")]
        public float duration = 0.05f;
        
        [Tooltip("Time.timeScale = 0 pendant le hitstop (sinon 0.01)")]
        public bool freezeTime = true;
    }

    [System.Serializable]
    public class KnockbackSettings
    {
        [Tooltip("Active le knockback sur l'ennemi touché")]
        public bool enabled = true;
        
        [Min(0f)]
        [Tooltip("Force de repousse appliquée à l'ennemi")]
        public float force = 15f;
        
        [Min(0.1f)]
        [Tooltip("Durée avant que l'ennemi reprenne son comportement")]
        public float duration = 0.5f;
        
        [Tooltip("Le knockback affecte aussi l'axe Y")]
        public bool affectsYAxis = false;
    }

    [System.Serializable]
    public class ScreenShakeSettings
    {
        [Tooltip("Active le screenshake à l'impact du dash")]
        public bool enabled = true;
        
        [Min(0.01f)]
        [Tooltip("Durée du screenshake en secondes")]
        public float duration = 0.15f;
        
        [Min(0f)]
        [Tooltip("Intensité du déplacement de la caméra")]
        public float positionMagnitude = 0.1f;
        
        [Min(0f)]
        [Tooltip("Intensité de la rotation de la caméra (en degrés)")]
        public float rotationMagnitude = 2f;
    }
}
