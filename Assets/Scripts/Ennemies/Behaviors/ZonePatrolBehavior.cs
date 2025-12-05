using UnityEngine;
using UnityEngine.AI;
using Ennemies.Settings;

namespace Ennemies.Behaviors
{
    /// <summary>
    /// Comportement d'ennemi qui patrouille sur des waypoints.
    /// Poursuit le joueur s'il entre dans la zone, retourne patrouiller s'il sort.
    /// </summary>
    public class ZonePatrolBehavior : IEnemyBehavior
    {
        private NavMeshAgent agent;
        private Transform player;
        private Transform owner;
        private EnemyBehaviorSettings settings;

        private Vector3 spawnPosition;
        private WaypointPath waypointPath;
        private int currentWaypointIndex;
        private float waitTimer;
        private bool isWaiting;
        private bool hasReachedCurrentWaypoint;

        private bool isChasing;
        private bool isPatrolling;
        private bool canAttack;
        private bool isAlerted; // Alerte quand touché par le joueur
        private float alertTimer;

        private const float ARRIVAL_DISTANCE = 1.5f;
        private const float ALERT_DURATION = 5f; // Durée de l'alerte après avoir été touché

        public void Initialize(NavMeshAgent agent, Transform player, EnemyBehaviorSettings settings, Transform owner)
        {
            this.agent = agent;
            this.player = player;
            this.settings = settings;
            this.owner = owner;
            this.spawnPosition = owner.position;
            this.isPatrolling = true;
            this.isChasing = false;
            this.currentWaypointIndex = 0;
            this.hasReachedCurrentWaypoint = false;
            this.isWaiting = false;
            this.isAlerted = false;
            this.alertTimer = 0f;
        }

        /// <summary>
        /// Définit le chemin de waypoints pour la patrouille.
        /// </summary>
        public void SetWaypointPath(WaypointPath path)
        {
            this.waypointPath = path;
            if (path != null && path.WaypointCount > 0)
            {
                // Commencer au waypoint le plus proche
                currentWaypointIndex = path.GetClosestWaypointIndex(owner.position);
                isWaiting = false;
                hasReachedCurrentWaypoint = false;
            }
        }

        public void Execute()
        {
            if (agent == null || player == null) return;

            // Gérer le timer d'alerte
            if (isAlerted)
            {
                alertTimer -= Time.deltaTime;
                if (alertTimer <= 0f)
                {
                    isAlerted = false;
                }
            }

            float distanceToPlayer = Vector3.Distance(owner.position, player.position);
            float distancePlayerToSpawn = Vector3.Distance(player.position, spawnPosition);

            bool playerInZone = distancePlayerToSpawn <= settings.patrolRadius;
            bool playerDetected = distanceToPlayer <= settings.detectionRange;
            bool hasLineOfSight = !settings.requireLineOfSight || CheckLineOfSight();

            // Si alerté (touché par le joueur), poursuivre le joueur sans limite de distance
            // Sinon, poursuivre seulement si le joueur est dans la zone ET détecté ET visible
            if (isAlerted || (playerInZone && playerDetected && hasLineOfSight))
            {
                isChasing = true;
                isPatrolling = false;
                agent.speed = settings.chaseSpeed;

                if (distanceToPlayer <= settings.attackRange)
                {
                    agent.isStopped = true;
                    canAttack = true;
                    RotateTowardsPlayer();
                }
                else
                {
                    agent.isStopped = false;
                    agent.SetDestination(player.position);
                    canAttack = false;
                }
            }
            else
            {
                isChasing = false;
                isPatrolling = true;
                canAttack = false;
                agent.speed = settings.patrolSpeed;

                ExecutePatrol();
            }
        }

        private void ExecutePatrol()
        {
            if (waypointPath == null || waypointPath.WaypointCount == 0)
            {
                if (Vector3.Distance(owner.position, spawnPosition) > 0.5f)
                {
                    agent.isStopped = false;
                    agent.SetDestination(spawnPosition);
                }
                else
                {
                    agent.isStopped = true;
                }
                return;
            }

            // Phase d'attente au waypoint
            if (isWaiting)
            {
                agent.isStopped = true;
                waitTimer -= Time.deltaTime;
                
                if (waitTimer <= 0f)
                {
                    isWaiting = false;
                    MoveToNextWaypoint();
                }
                return;
            }

            // Phase de déplacement vers le waypoint actuel
            Transform targetWaypoint = waypointPath.GetWaypoint(currentWaypointIndex);
            if (targetWaypoint == null) return;

            float distanceToWaypoint = Vector3.Distance(owner.position, targetWaypoint.position);

            if (distanceToWaypoint <= ARRIVAL_DISTANCE && !hasReachedCurrentWaypoint)
            {
                hasReachedCurrentWaypoint = true;
                
                if (settings.waypointWaitTime > 0f)
                {
                    isWaiting = true;
                    waitTimer = settings.waypointWaitTime;
                    agent.isStopped = true;
                }
                else
                {
                    MoveToNextWaypoint();
                }
            }
            else if (distanceToWaypoint > ARRIVAL_DISTANCE)
            {
                agent.isStopped = false;
                agent.SetDestination(targetWaypoint.position);
            }
        }

        private void MoveToNextWaypoint()
        {
            currentWaypointIndex = waypointPath.GetNextWaypointIndex(currentWaypointIndex);
            hasReachedCurrentWaypoint = false;
            
            Transform nextWaypoint = waypointPath.GetWaypoint(currentWaypointIndex);
            if (nextWaypoint != null)
            {
                agent.isStopped = false;
                agent.SetDestination(nextWaypoint.position);
            }
        }

        private void RotateTowardsPlayer()
        {
            Vector3 direction = (player.position - owner.position).normalized;
            direction.y = 0;

            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                owner.rotation = Quaternion.Slerp(owner.rotation, lookRotation, Time.deltaTime * settings.rotationSpeed);
            }
        }

        private bool CheckLineOfSight()
        {
            Vector3 eyePosition = owner.position + Vector3.up * settings.eyeHeight;
            Vector3 targetPosition = player.position + Vector3.up * 1f;
            Vector3 direction = targetPosition - eyePosition;
            float distance = direction.magnitude;

            if (Physics.Raycast(eyePosition, direction.normalized, out RaycastHit hit, distance, settings.obstacleLayer))
            {
                return hit.transform == player || hit.transform.IsChildOf(player);
            }
            
            return true;
        }

        public bool CanAttack() => canAttack;
        public bool IsChasing() => isChasing;
        public bool IsPatrolling() => isPatrolling;

        public void OnDamageTaken()
        {
            // Quand l'ennemi est touché, il devient alerté et poursuit le joueur
            isAlerted = true;
            alertTimer = ALERT_DURATION;
        }

        public void DrawGizmos()
        {
            if (settings == null) return;

            Vector3 zoneCenter = Application.isPlaying ? spawnPosition : (owner != null ? owner.position : Vector3.zero);
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(zoneCenter, settings.patrolRadius);

            if (owner != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(owner.position, settings.detectionRange);

                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(owner.position, settings.attackRange);
            }

            if (Application.isPlaying)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(spawnPosition, 0.3f);
                
                if (waypointPath != null && waypointPath.WaypointCount > 0)
                {
                    Transform currentWP = waypointPath.GetWaypoint(currentWaypointIndex);
                    if (currentWP != null && owner != null)
                    {
                        Gizmos.color = Color.green;
                        Gizmos.DrawLine(owner.position, currentWP.position);
                    }
                }
            }
        }
    }
}

