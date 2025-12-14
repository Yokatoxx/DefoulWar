using UnityEngine;

namespace FPS
{
    [CreateAssetMenu(fileName = "DashDefinition", menuName = "FPS/Dash Definition")]
    public class DashDefinition : ScriptableObject
    {
        [Header("Ciblage")]
        public LayerMask enemyMask = ~0;
        public LayerMask obstacleMask = ~0;
        [Min(0f)] public float maxAimAngle = 30f;

        [Header("Dash Ciblé Settings")]
        [Min(1)] public int countDash = 3;
        [Min(0.01f)] public float slowMoTime = 0.75f;
        [Range(0.01f, 1f)] public float slowMoScale = 0.2f;
        [Min(0.5f)] public float distanceDash = 25f;
        [Min(0f)] public float cooldown = 1.5f;
        [Min(0f)] public float dashDamage = 9999f;

        [Header("Déplacement")]
        [Min(0.01f)] public float dashTravelTime = 0.08f;
        [Min(0f)] public float capsuleRadius = 0.4f;
        [Min(0f)] public float stopOffset = 1f;

        [Header("HitStop")]
        [Tooltip("Durée du hitstop en temps réel (0 = désactivé)")]
        [Min(0f)] public float hitStopUnscaledDuration = 0f;
        [Tooltip("Si true, Time.timeScale = 0 pendant le hitstop; sinon = 0.01")]
        public bool hitStopFreezeTime = true;
        
        [Header("Knockback Ennemi")]
        [Tooltip("Force de repousse appliquée à l'ennemi lors du dash")]
        [Min(0f)] public float knockbackForce = 15f;
        
        [Tooltip("Durée du knockback (temps avant que l'ennemi reprenne son comportement IA)")]
        [Min(0.1f)] public float knockbackDuration = 0.5f;
        
        [Tooltip("Si true, le knockback affecte aussi l'axe Y (propulse légèrement en l'air)")]
        public bool knockbackAffectsYAxis = false;

        [Header("Gameplay")]
        [Tooltip("Durée pendant laquelle le joueur ne peut pas tirer après un dash (en secondes, temps réel/unscaled)")]
        [Min(0f)] public float postDashNoFireDuration = 0.1f;
    }
}