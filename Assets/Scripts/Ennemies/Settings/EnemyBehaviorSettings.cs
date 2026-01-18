using UnityEngine;

namespace Ennemies.Settings
{
    /// <summary>
    /// Type de comportement de déplacement de l'ennemi.
    /// </summary>
    public enum EnemyBehaviorType
    {
        /// <summary>Maintient une distance fixe avec le joueur.</summary>
        Distance,
        /// <summary>Poursuit le joueur en permanence.</summary>
        Chaser,
        /// <summary>Poursuit dans sa zone, retourne patrouiller si le joueur sort.</summary>
        ZonePatrol,
        /// <summary>Suit un autre ennemi (compagnon) s’il est disponible.</summary>
        CompanionFollower,
        /// <summary> poursuit le joueur et insatncie une zone d'attaque à ses pieds si il est proche du joueur </summary>
        GroundSlam
    }

    /// <summary>
    /// Type d'attaque de l'ennemi.
    /// </summary>
    public enum AttackType
    {
        /// <summary>Attaque au corps à corps.</summary>
        Melee,
        /// <summary>Attaque à distance (projectile ou hitscan).</summary>
        Ranged
    }

    /// <summary>
    /// ScriptableObject pour configurer le comportement d'un ennemi.
    /// </summary>
    [CreateAssetMenu(fileName = "EnemyBehavior", menuName = "Enemies/Behavior Settings")]
    public class EnemyBehaviorSettings : ScriptableObject
    {
        [Header("Behavior Type")]
        [Tooltip("Type de comportement de déplacement")]
        public EnemyBehaviorType behaviorType = EnemyBehaviorType.Chaser;

        [Header("Movement Settings")]
        [Tooltip("Distance à laquelle l'ennemi détecte le joueur ou ses cibles")]
        [Min(0f)] public float detectionRange = 15f;

        [Tooltip("Nécessite une ligne de vue directe pour détecter la cible")]
        public bool requireLineOfSight = true;

        [Tooltip("Layers considérés comme obstacles pour la ligne de vue")]
        public LayerMask obstacleLayer = ~0;

        [Tooltip("Hauteur des yeux de l'ennemi pour le raycast")]
        [Min(0f)] public float eyeHeight = 1.5f;

        [Tooltip("Vitesse de déplacement en poursuite")]
        [Min(0f)] public float chaseSpeed = 3.5f;

        [Tooltip("Vitesse de déplacement en patrouille / escorte")]
        [Min(0f)] public float patrolSpeed = 2f;

        [Tooltip("Distance à maintenir (Distance et CompanionFollower)")]
        [Min(0f)] public float keepDistance = 8f;

        [Tooltip("Tolérance de distance avant de se repositionner (Distance)")]
        [Min(0f)] public float distanceTolerance = 1f;

        [Header("Zone Patrol Settings")]
        [Tooltip("Rayon de la zone de patrouille (ZonePatrol)")]
        [Min(0f)] public float patrolRadius = 20f;

        [Tooltip("Temps d'attente à chaque waypoint (ZonePatrol)")]
        [Min(0f)] public float waypointWaitTime = 1f;

        [Header("Attack Settings")]
        [Tooltip("Type d'attaque")]
        public AttackType attackType = AttackType.Melee;

        [Tooltip("Dégâts infligés par attaque")]
        [Min(0f)] public float attackDamage = 10f;

        [Tooltip("Temps entre chaque attaque")]
        [Min(0.1f)] public float attackCooldown = 1.5f;

        [Tooltip("Portée d'attaque")]
        [Min(0f)] public float attackRange = 2f;

        [Header("Ranged Attack Settings")]
        [Tooltip("Si true, utilise un raycast instantané. Sinon, tire un projectile.")]
        public bool isHitscan = false;

        [Tooltip("Prefab du projectile (ignoré si hitscan)")]
        public GameObject bulletPrefab;

        [Tooltip("Vitesse du projectile (ignoré si hitscan)")]
        [Min(0f)] public float bulletSpeed = 20f;

        [Tooltip("Durée de vie du projectile en secondes")]
        [Min(0f)] public float bulletLifetime = 5f;

        [Header("Bullet Trail Settings")]
        [Tooltip("Prefab du trail pour les projectiles (optionnel)")]
        public TrailRenderer bulletTrailPrefab;

        [Tooltip("Durée du trail en secondes")]
        [Min(0f)] public float trailDuration = 0.5f;

        [Header("Rotation Settings")]
        [Tooltip("Vitesse de rotation vers la cible")]
        [Min(0f)] public float rotationSpeed = 5f;

        [Header("NavMesh Agent Settings")]
        [Tooltip("Accélération du NavMeshAgent (plus élevé = changements de direction plus rapides)")]
        [Min(1f)] public float acceleration = 25f;

        [Tooltip("Vitesse de rotation du NavMeshAgent en degrés/seconde")]
        [Min(60f)] public float angularSpeed = 360f;

        [Tooltip("Si false, l'agent ne ralentit pas automatiquement avant d'atteindre sa destination")]
        public bool autoBraking = false;

        [Header("Vision Settings")]
        [Tooltip("Angle de vue de l'ennemi en degrés (120° = vision humaine normale)")]
        [Range(30f, 360f)] public float viewAngle = 120f;

        [Tooltip("Temps en secondes que l'ennemi passe à investiguer la dernière position connue")]
        [Min(0f)] public float investigationTime = 3f;

        [Tooltip("Rayon de détection 360° (bruit, dash proche, etc.)")]
        [Min(0f)] public float hearingRange = 8f;

        [Tooltip("Rayon d'alerte pour prévenir les autres ennemis")]
        [Min(0f)] public float alertRadius = 15f;

        [Header("Slam Settings")]
        [Tooltip("Prefab de la zone de dégâts (doit contenir un Collider isTrigger + SlamDamageZone).")]
        public GameObject slamZonePrefab;
        [Tooltip("Durée de vie de la zone slam (secondes).")]
        [Min(0f)] public float slamLifetime = 0.6f;
        [Tooltip("Décalage vertical du spawn par rapport aux pieds.")]
        public float slamYOffset = 0.05f;
        [Tooltip("Rayon/portée de la zone de trigger du slam (si applicable).")]
        [Min(0f)] public float slamTriggerRadius = 10f;

        [Header("Sniper Settings (Distance Behavior)")]
        [Tooltip("Distance à laquelle l'ennemi fuit vers la hauteur au lieu de tirer")]
        [Min(0f)] public float fleeToHighGroundDistance = 8f;

        [Tooltip("Durée de la phase de charge (laser rouge suit le joueur)")]
        [Min(0.1f)] public float sniperChargeDuration = 1.0f;

        [Tooltip("Durée de la phase de verrouillage (laser vert fixe)")]
        [Min(0.1f)] public float sniperLockDuration = 0.5f;

        [Tooltip("Rayon de recherche des points en hauteur")]
        [Min(1f)] public float highGroundSearchRadius = 20f;

        [Header("Dodge Settings")]
        [Tooltip("Active l'esquive pour ce type d'ennemi")]
        public bool canDodge = false;

        [Tooltip("Probabilité d'esquive (0-1), 0.1 = 10%")]
        [Range(0f, 1f)] public float dodgeChance = 0.1f;

        [Tooltip("Force du bond latéral")]
        [Min(1f)] public float dodgeForce = 8f;

        [Tooltip("Durée de l'esquive en secondes")]
        [Min(0.01f)] public float dodgeDuration = 0.3f;

        [Tooltip("Cooldown entre deux esquives")]
        [Min(0.5f)] public float dodgeCooldown = 2f;
    }
}