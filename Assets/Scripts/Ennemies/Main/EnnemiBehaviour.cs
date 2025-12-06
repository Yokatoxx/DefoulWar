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
    public class EnnemiBehaviour : MonoBehaviour
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
        
        // Animator pour les animations futures
        // private Animator animator;

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
            
            // animator = GetComponent<Animator>();

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
                Debug.LogWarning($"[EnnemiBehaviour] No settings assigned on {gameObject.name}!");
                return;
            }

            // Initialiser le handler d'attaque
            attackHandler.Initialize(settings, player);

            // Créer le comportement approprié
            currentBehavior = CreateBehavior(settings.behaviorType);
            currentBehavior.Initialize(agent, player, settings, transform);

            // Configuration spéciale pour ZonePatrol
            if (currentBehavior is ZonePatrolBehavior zonePatrol && waypointPath != null)
            {
                waypointPath.RefreshWaypoints(); // S'assurer que les waypoints sont chargés
                zonePatrol.SetWaypointPath(waypointPath);
            }

            // Appliquer la vitesse initiale
            agent.speed = settings.patrolSpeed;
        }

        private IEnemyBehavior CreateBehavior(EnemyBehaviorType type)
        {
            switch (type)
            {
                case EnemyBehaviorType.Distance:
                    return new DistanceBehavior();
                case EnemyBehaviorType.Chaser:
                    return new ChaserBehavior();
                case EnemyBehaviorType.ZonePatrol:
                    return new ZonePatrolBehavior();
                default:
                    Debug.LogWarning($"[EnnemiBehaviour] Unknown behavior type: {type}. Using Chaser.");
                    return new ChaserBehavior();
            }
        }

        private void Update()
        {
            // Ne rien faire si mort
            if (health != null && health.IsDead)
            {
                agent.isStopped = true;
                return;
            }

            if (currentBehavior == null || player == null) return;

            // Exécuter le comportement
            currentBehavior.Execute();

            // Gérer les attaques
            if (currentBehavior.CanAttack())
            {
                attackHandler.TryAttack();
            }

            // Triggers d'animation (commentés pour le moment)
            UpdateAnimationTriggers();
        }

        private void UpdateAnimationTriggers()
        {
            bool isChasing = currentBehavior.IsChasing();
            bool isPatrolling = currentBehavior.IsPatrolling();

            // Trigger quand on commence à poursuivre
            if (isChasing && !wasChasing)
            {
                // animator?.SetTrigger("OnChase");
                // animator?.SetBool("IsChasing", true);
            }
            else if (!isChasing && wasChasing)
            {
                // animator?.SetBool("IsChasing", false);
            }

            // Trigger quand on commence à patrouiller
            if (isPatrolling && !wasPatrolling)
            {
                // animator?.SetTrigger("OnPatrol");
                // animator?.SetBool("IsPatrolling", true);
            }
            else if (!isPatrolling && wasPatrolling)
            {
                // animator?.SetBool("IsPatrolling", false);
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
                // Dessiner les gizmos en mode éditeur (avant play)
                DrawEditorGizmos();
            }
        }

        private void DrawEditorGizmos()
        {
            // Zone de détection
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, settings.detectionRange);

            // Portée d'attaque
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, settings.attackRange);

            // Spécifique au type
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
