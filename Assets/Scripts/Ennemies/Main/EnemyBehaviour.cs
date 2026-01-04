using UnityEngine;
using UnityEngine.AI;
using FPS;
using Ennemies.Settings;
using Ennemies.Behaviors;

namespace Ennemies
{
    /// <summary>
    /// Contrôleur principal du comportement d'un ennemi.
    /// Utilise un ScriptableObject pour configurer le type de comportement.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(EnemyHealth))]
    public class EnemyBehaviour : MonoBehaviour
    {
        [Header("Behavior Configuration")]
        [Tooltip("Configuration du comportement de l'ennemi")]
        [SerializeField] private EnemyBehaviorSettings settings;

        [Header("Patrol Settings (pour ZonePatrol)")]
        [Tooltip("Chemin de waypoints pour la patrouille")]
        [SerializeField] private WaypointPath waypointPath;

        [Header("Attack Settings")]
        [Tooltip("Point d'origine des tirs")]
        [SerializeField] private Transform shootPoint;

        [Header("Debug")]
        [SerializeField] private bool showDebugGizmos = true;

        // Components
        private NavMeshAgent agent;
        private EnemyHealth health;
        private EnemyAttackHandler attackHandler;

        // Behavior
        private IEnemyBehavior currentBehavior;
        private Transform player;

        // État précédent pour les triggers d'animation
        private bool wasChasing;
        private bool wasPatrolling;

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            health = GetComponent<EnemyHealth>();
            attackHandler = GetComponent<EnemyAttackHandler>();

            // Trouver le joueur
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }

            // Créer le handler d'attaque s'il n'existe pas
            if (attackHandler == null)
            {
                attackHandler = gameObject.AddComponent<EnemyAttackHandler>();
            }

            if (shootPoint != null)
            {
                attackHandler.SetShootPoint(shootPoint);
            }

            // Écouter les événements de dégâts
            if (health != null)
            {
                health.OnDamageTaken.AddListener(OnDamageTaken);
            }
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.OnDamageTaken.RemoveListener(OnDamageTaken);
            }
        }

        private void OnDamageTaken(float damage, string zone)
        {
            // Notifier le comportement que l'ennemi a été touché
            currentBehavior?.OnDamageTaken();
        }

        private void Start()
        {
            InitializeBehavior();
        }

        private void InitializeBehavior()
        {
            if (settings == null)
            {
                Debug.LogWarning($"[EnemyBehaviour] No settings assigned on {gameObject.name}!");
                return;
            }

            // Initialiser le handler d'attaque
            attackHandler.Initialize(settings, player);

            // Créer le comportement approprié
            currentBehavior = CreateBehavior(settings.behaviorType);
            currentBehavior.Initialize(agent, player, settings, transform);

            // Si le comportement supporte un shield, lier le composant s'il existe
            var shieldComp = GetComponentInChildren<Ennemies.Effect.EnemyShield>();
            if (shieldComp != null && currentBehavior is Ennemies.Behaviors.ChaserBehavior chaser)
            {
                chaser.SetShield(shieldComp);
            }

            // Configuration spéciale pour ZonePatrol
            if (currentBehavior is ZonePatrolBehavior zonePatrol && waypointPath != null)
            {
                waypointPath.RefreshWaypoints();
                zonePatrol.SetWaypointPath(waypointPath);
            }

            // Appliquer la vitesse initiale
            agent.speed = settings.patrolSpeed;
        }

        private IEnemyBehavior CreateBehavior(EnemyBehaviorType type)
        {
            switch (type)
            {
                case EnemyBehaviorType.Distance: return new DistanceBehavior();
                case EnemyBehaviorType.Chaser: return new ChaserBehavior();
                case EnemyBehaviorType.ZonePatrol: return new ZonePatrolBehavior();
                case EnemyBehaviorType.CompanionFollower: return new FollowCompanionBehavior();
                case EnemyBehaviorType.GroundSlam: return new GroundSlamBehavior();
                default:
                    Debug.LogWarning($"[EnnemiBehaviour] Unknown behavior type: {type}. Using Chaser.");
                    return new ChaserBehavior();
            }
        }

        private void Update()
        {
            if (health != null && health.IsDead)
            {
                agent.isStopped = true;
                return;
            }

            if (currentBehavior == null || player == null) return;

            currentBehavior.Execute();

            if (currentBehavior.CanAttack())
            {
                attackHandler.TryAttack();
            }

            UpdateAnimationTriggers();
        }

        private void UpdateAnimationTriggers()
        {
            bool isChasing = currentBehavior.IsChasing();
            bool isPatrolling = currentBehavior.IsPatrolling();

            // Note: Animations commentées pour implémentation future
            if (isChasing && !wasChasing)
            {
                // animator?.SetTrigger("OnChase");
            }

            if (isPatrolling && !wasPatrolling)
            {
                // animator?.SetTrigger("OnPatrol");
            }

            wasChasing = isChasing;
            wasPatrolling = isPatrolling;
        }

        /// <summary>
        /// Change dynamiquement les settings de comportement.
        /// </summary>
        public void SetSettings(EnemyBehaviorSettings newSettings)
        {
            settings = newSettings;
            InitializeBehavior();
        }

        /// <summary>
        /// Définit le chemin de waypoints.
        /// </summary>
        public void SetWaypointPath(WaypointPath path)
        {
            waypointPath = path;
            if (currentBehavior is ZonePatrolBehavior zonePatrol)
            {
                zonePatrol.SetWaypointPath(path);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!showDebugGizmos) return;

            if (currentBehavior != null)
            {
                currentBehavior.DrawGizmos();
            }
            else if (settings != null)
            {
                DrawEditorGizmos();
            }
        }

        private void DrawEditorGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, settings.detectionRange);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, settings.attackRange);

            switch (settings.behaviorType)
            {
                case EnemyBehaviorType.Distance:
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireSphere(transform.position, settings.keepDistance);
                    break;
                case EnemyBehaviorType.ZonePatrol:
                    Gizmos.color = Color.blue;
                    Gizmos.DrawWireSphere(transform.position, settings.patrolRadius);
                    break;
            }
        }

        // Propriétés publiques
        public EnemyBehaviorSettings Settings => settings;
        public bool IsChasing => currentBehavior?.IsChasing() ?? false;
        public bool IsPatrolling => currentBehavior?.IsPatrolling() ?? false;
        public bool CanAttack => currentBehavior?.CanAttack() ?? false;
    }
}
