using UnityEngine;
using UnityEngine.AI;
using Ennemies.Settings;

namespace Ennemies.Behaviors
{
    /// <summary>
    /// Comportement d'ennemi qui patrouille sur des waypoints.
    /// Poursuit le joueur s'il entre dans la zone, retourne patrouiller s'il sort.
    /// Utilise le système de vision avec FOV.
    /// </summary>
    public class ZonePatrolBehavior : BaseEnemyBehavior
    {
        private Vector3 spawnPosition;
        private WaypointPath waypointPath;
        private int currentWaypointIndex;
        private float waitTimer;
        private bool isWaiting;
        private bool hasReachedCurrentWaypoint;

        private bool canAttack;

        private const float ARRIVAL_DISTANCE = 1.5f;

        public override void Initialize(NavMeshAgent agent, Transform player, EnemyBehaviorSettings settings, Transform owner)
        {
            base.Initialize(agent, player, settings, owner);
            this.spawnPosition = owner.position;
            this.currentWaypointIndex = 0;
            this.hasReachedCurrentWaypoint = false;
            this.isWaiting = false;
            detectionState = DetectionState.Idle;
        }

        /// <summary>
        /// Définit le chemin de waypoints pour la patrouille.
        /// </summary>
        public void SetWaypointPath(WaypointPath path)
        {
            this.waypointPath = path;
            if (path != null && path.WaypointCount > 0)
            {
                currentWaypointIndex = path.GetClosestWaypointIndex(owner.position);
                isWaiting = false;
                hasReachedCurrentWaypoint = false;
            }
        }

        protected override void ExecuteBehavior()
        {
            if (agent == null || player == null) return;

            float distanceToPlayer = Vector3.Distance(owner.position, player.position);
            float distancePlayerToSpawn = Vector3.Distance(player.position, spawnPosition);
            bool playerInZone = distancePlayerToSpawn <= settings.patrolRadius;
            bool canSeePlayer = IsPlayerInFieldOfView();

            switch (detectionState)
            {
                case DetectionState.Idle:
                    HandleIdleState(canSeePlayer, playerInZone);
                    break;

                case DetectionState.Chasing:
                    HandleChasingState(canSeePlayer, distanceToPlayer, playerInZone);
                    break;

                case DetectionState.Investigating:
                    HandleInvestigatingState(canSeePlayer);
                    break;

                case DetectionState.Lost:
                    HandleLostState(canSeePlayer);
                    break;
            }
        }

        private void HandleIdleState(bool canSeePlayer, bool playerInZone)
        {
            canAttack = false;

            // Patrouiller normalement
            ExecutePatrol();

            // Détection du joueur dans la zone
            if (canSeePlayer && playerInZone)
            {
                if (TryStartChaseWithTurn())
                {
                    detectionState = DetectionState.Chasing;
                    return;
                }
                StartChasing();
            }
        }

        private void HandleChasingState(bool canSeePlayer, float distanceToPlayer, bool playerInZone)
        {
            if (canSeePlayer)
            {
                UpdateLastKnownPosition();
                AlertNearbyEnemies();

                agent.speed = settings.chaseSpeed;

                if (distanceToPlayer <= settings.attackRange)
                {
                    agent.isStopped = true;
                    canAttack = true;
                    RotateTowardsPlayerSmooth();
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
                // Perte de vue - investigation
                detectionState = DetectionState.Investigating;
                investigationTimer = settings.investigationTime;
                agent.SetDestination(lastKnownPlayerPosition);
                agent.isStopped = false;
                canAttack = false;
            }
        }

        private void HandleInvestigatingState(bool canSeePlayer)
        {
            canAttack = false;

            if (canSeePlayer)
            {
                StartChasing();
                return;
            }

            if (!HasReachedLastKnownPosition())
            {
                agent.SetDestination(lastKnownPlayerPosition);
                agent.isStopped = false;
            }
            else
            {
                agent.isStopped = true;
                if (UpdateInvestigationTimer())
                {
                    detectionState = DetectionState.Lost;
                    ResetAlertState();
                }
            }
        }

        private void HandleLostState(bool canSeePlayer)
        {
            canAttack = false;

            if (canSeePlayer)
            {
                StartChasing();
            }
            else
            {
                // Retour à la patrouille
                detectionState = DetectionState.Idle;
                agent.speed = settings.patrolSpeed;
            }
        }

        private void StartChasing()
        {
            detectionState = DetectionState.Chasing;
            agent.speed = settings.chaseSpeed;
            agent.isStopped = false;
            UpdateLastKnownPosition();
        }

        private void ExecutePatrol()
        {
            agent.speed = settings.patrolSpeed;

            if (waypointPath == null || waypointPath.WaypointCount == 0)
            {
                // Pas de waypoints, rester au spawn
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

            // Phase d'attente
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

            // Déplacement vers le waypoint
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

        public override bool CanAttack() => canAttack;
        public override bool IsChasing() => detectionState == DetectionState.Chasing;
        public override bool IsPatrolling() => detectionState == DetectionState.Idle;

        public override void OnDamageTaken()
        {
            // Touché = alerte immédiate et poursuite
            if (player != null && detectionState != DetectionState.Chasing)
            {
                UpdateLastKnownPosition();
                StartChasing();
            }
        }

        public override void DrawGizmos()
        {
            if (settings == null) return;

            Vector3 zoneCenter = Application.isPlaying ? spawnPosition : (owner != null ? owner.position : Vector3.zero);
            
            // Zone de patrouille
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(zoneCenter, settings.patrolRadius);

            if (owner != null)
            {
                // Détection
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(owner.position, settings.detectionRange);

                // Attaque
                Gizmos.color = Color.red;
                Gizmos.DrawWireSphere(owner.position, settings.attackRange);

                // Cône de vision
                float halfAngle = settings.viewAngle / 2f;
                Vector3 leftDir = Quaternion.Euler(0, -halfAngle, 0) * owner.forward;
                Vector3 rightDir = Quaternion.Euler(0, halfAngle, 0) * owner.forward;
                Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
                Gizmos.DrawRay(owner.position + Vector3.up * settings.eyeHeight, leftDir * settings.detectionRange);
                Gizmos.DrawRay(owner.position + Vector3.up * settings.eyeHeight, rightDir * settings.detectionRange);
            }

            if (Application.isPlaying)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawSphere(spawnPosition, 0.3f);
                
                // Dernière position connue
                if (detectionState == DetectionState.Investigating)
                {
                    Gizmos.color = Color.magenta;
                    Gizmos.DrawWireSphere(lastKnownPlayerPosition, 0.5f);
                    Gizmos.DrawLine(owner.position, lastKnownPlayerPosition);
                }

                // Waypoint actuel
                if (waypointPath != null && waypointPath.WaypointCount > 0 && detectionState == DetectionState.Idle)
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
