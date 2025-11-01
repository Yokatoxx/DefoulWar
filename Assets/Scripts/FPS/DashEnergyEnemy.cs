using UnityEngine;
using UnityEngine.AI;

namespace FPS
{
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(EnemyHealth))]
    public class DashEnergyEnemy : MonoBehaviour
    {
        [Header("Flee Settings")]
        [Tooltip("Distance à partir de laquelle l'ennemi commence à fuir")]
        [SerializeField] private float fleeDetectionRange = 20f;
        
        [Tooltip("Vitesse de fuite de l'ennemi")]
        [SerializeField] private float fleeSpeed = 6f;
        
        [Tooltip("Distance minimale à maintenir avec le joueur")]
        [SerializeField] private float minDistanceFromPlayer = 15f;
        
        [Tooltip("Vitesse de rotation vers la direction de fuite")]
        [SerializeField] private float rotationSpeed = 8f;
        
        [Header("Movement Behavior")]
        [Tooltip("Distance maximale de fuite par déplacement")]
        [SerializeField] private float maxFleeDistance = 10f;
        
        [Tooltip("Temps d'attente avant de recalculer la direction de fuite")]
        [SerializeField] private float fleeRecalculateInterval = 0.5f;
        
        [Tooltip("Vitesse de patrouille quand le joueur est loin")]
        [SerializeField] private float idleSpeed = 2f;
        
        [Header("Patrol Settings")]
        [Tooltip("Activer la patrouille aléatoire quand le joueur est loin")]
        [SerializeField] private bool enableRandomPatrol = true;
        
        [Tooltip("Rayon de la zone de patrouille aléatoire")]
        [SerializeField] private float patrolRadius = 10f;
        
        [Tooltip("Temps d'attente entre chaque point de patrouille")]
        [SerializeField] private float patrolWaitTime = 2f;
        
        [Header("Dash Energy")]
        [Tooltip("Quantité d'énergie de dash donnée quand tué (entre 0 et 1, 1 = charge complète)")]
        [SerializeField] private float dashEnergyAmount = 1f;
        
        public float DashEnergyAmount => dashEnergyAmount;
        
        [Header("References")]
        [SerializeField] private LayerMask obstacleLayer;
        
        private NavMeshAgent agent;
        private EnemyHealth enemyHealth;
        private Transform player;
        private float lastFleeCalculation;
        private float patrolWaitTimer;
        private bool isWaitingAtPatrolPoint;
        
        public enum FleeState
        {
            Idle,
            Fleeing,
            Patrol
        }
        
        private FleeState currentState = FleeState.Idle;
        
        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            enemyHealth = GetComponent<EnemyHealth>();
            
            // Trouver le joueur
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
            
            agent.speed = idleSpeed;
        }
        
        private void Update()
        {
            if (enemyHealth != null && enemyHealth.IsDead) return;
            if (player == null) return;
            
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            
            // Machine à états pour le comportement de fuite
            switch (currentState)
            {
                case FleeState.Idle:
                    if (distanceToPlayer <= fleeDetectionRange)
                    {
                        currentState = FleeState.Fleeing;
                        agent.speed = fleeSpeed;
                        isWaitingAtPatrolPoint = false;
                    }
                    else if (enableRandomPatrol && !isWaitingAtPatrolPoint)
                    {
                        currentState = FleeState.Patrol;
                    }
                    break;
                    
                case FleeState.Fleeing:
                    HandleFleeing(distanceToPlayer);
                    
                    if (distanceToPlayer > fleeDetectionRange * 1.5f)
                    {
                        currentState = FleeState.Idle;
                        agent.speed = idleSpeed;
                    }
                    break;
                    
                case FleeState.Patrol:
                    HandlePatrol(distanceToPlayer);
                    
                    if (distanceToPlayer <= fleeDetectionRange)
                    {
                        currentState = FleeState.Fleeing;
                        agent.speed = fleeSpeed;
                        isWaitingAtPatrolPoint = false;
                    }
                    break;
            }
        }
        
        private void HandleFleeing(float distanceToPlayer)
        {
            // Recalculer la direction de fuite à intervalles réguliers
            if (Time.time - lastFleeCalculation >= fleeRecalculateInterval)
            {
                CalculateFleeDestination();
                lastFleeCalculation = Time.time;
            }
            
            // Rotation vers la direction de fuite
            if (agent.hasPath && agent.velocity.sqrMagnitude > 0.1f)
            {
                Vector3 fleeDirection = agent.velocity.normalized;
                Quaternion lookRotation = Quaternion.LookRotation(new Vector3(fleeDirection.x, 0, fleeDirection.z));
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * rotationSpeed);
            }
        }
        
        private void CalculateFleeDestination()
        {
            if (player == null) return;
            
            // Direction opposée au joueur
            Vector3 directionAwayFromPlayer = (transform.position - player.position).normalized;
            
            // Calculer une position de fuite
            Vector3 fleePosition = transform.position + directionAwayFromPlayer * maxFleeDistance;
            
            // Vérifier si la position est valide sur le NavMesh
            NavMeshHit hit;
            if (NavMesh.SamplePosition(fleePosition, out hit, maxFleeDistance, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
            }
            else
            {
                // Si pas de position valide, essayer une position aléatoire loin du joueur
                for (int i = 0; i < 8; i++)
                {
                    Vector3 randomDirection = Random.insideUnitSphere * maxFleeDistance;
                    randomDirection.y = 0;
                    Vector3 randomFleePos = transform.position + directionAwayFromPlayer * maxFleeDistance * 0.5f + randomDirection;
                    
                    if (NavMesh.SamplePosition(randomFleePos, out hit, maxFleeDistance, NavMesh.AllAreas))
                    {
                        float distToPlayer = Vector3.Distance(hit.position, player.position);
                        if (distToPlayer > Vector3.Distance(transform.position, player.position))
                        {
                            agent.SetDestination(hit.position);
                            break;
                        }
                    }
                }
            }
        }
        
        private void HandlePatrol(float distanceToPlayer)
        {
            if (isWaitingAtPatrolPoint)
            {
                patrolWaitTimer += Time.deltaTime;
                if (patrolWaitTimer >= patrolWaitTime)
                {
                    isWaitingAtPatrolPoint = false;
                    patrolWaitTimer = 0f;
                }
                return;
            }
            
            // Si on a atteint la destination ou qu'on n'a pas de destination
            if (!agent.hasPath || agent.remainingDistance <= agent.stoppingDistance)
            {
                SetRandomPatrolDestination();
                isWaitingAtPatrolPoint = true;
            }
        }
        
        private void SetRandomPatrolDestination()
        {
            Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
            randomDirection += transform.position;
            randomDirection.y = transform.position.y;
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                agent.speed = idleSpeed;
            }
        }
        

        private void OnDrawGizmosSelected()
        {
            // Visualiser la portée de détection
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, fleeDetectionRange);
            
            // Visualiser la distance minimale
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, minDistanceFromPlayer);
            
            // Visualiser le rayon de patrouille
            if (enableRandomPatrol)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(transform.position, patrolRadius);
            }
            
            // Visualiser la direction de fuite
            if (Application.isPlaying && player != null)
            {
                Vector3 directionAwayFromPlayer = (transform.position - player.position).normalized;
                Gizmos.color = Color.green;
                Gizmos.DrawRay(transform.position, directionAwayFromPlayer * 5f);
            }
        }
    }
}
