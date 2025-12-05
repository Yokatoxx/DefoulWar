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
        ZonePatrol
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
        [Tooltip("Distance à laquelle l'ennemi détecte le joueur")]
        [Min(0f)] public float detectionRange = 15f;

        [Tooltip("Nécessite une ligne de vue directe pour détecter le joueur")]
        public bool requireLineOfSight = true;

        [Tooltip("Layers considérés comme obstacles pour la ligne de vue")]
        public LayerMask obstacleLayer = ~0;

        [Tooltip("Hauteur des yeux de l'ennemi pour le raycast")]
        [Min(0f)] public float eyeHeight = 1.5f;

        [Tooltip("Vitesse de déplacement en poursuite")]
        [Min(0f)] public float chaseSpeed = 3.5f;

        [Tooltip("Vitesse de déplacement en patrouille")]
        [Min(0f)] public float patrolSpeed = 2f;

        [Tooltip("Distance à maintenir avec le joueur (pour le type Distance)")]
        [Min(0f)] public float keepDistance = 8f;

        [Tooltip("Tolérance de distance avant de se repositionner")]
        [Min(0f)] public float distanceTolerance = 1f;

        [Header("Zone Patrol Settings")]
        [Tooltip("Rayon de la zone de patrouille (pour ZonePatrol)")]
        [Min(0f)] public float patrolRadius = 20f;

        [Tooltip("Temps d'attente à chaque waypoint")]
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
        [Tooltip("Vitesse de rotation vers le joueur")]
        [Min(0f)] public float rotationSpeed = 5f;
    }
}

