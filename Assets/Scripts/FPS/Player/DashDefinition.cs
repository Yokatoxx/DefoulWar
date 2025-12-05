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
        
    }
}