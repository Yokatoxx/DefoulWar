﻿using UnityEngine;
using UnityEngine.AI;
using Ennemies.Settings;
using Ennemies.Effect;

namespace Ennemies.Behaviors
{
    /// <summary>
    /// Comportement d'ennemi qui poursuit le joueur et declenche une attaque au sol.
    /// Utilise le système de vision avec FOV et mémoire de position.
    /// </summary>
    public class GroundSlamBehavior : BaseEnemyBehavior
    {
        private float nextSlamTime;
        private bool canAttack;

        public override void Initialize(NavMeshAgent agent, Transform player, EnemyBehaviorSettings settings, Transform owner)
        {
            base.Initialize(agent, player, settings, owner);
            nextSlamTime = 0f;
            canAttack = false;
            detectionState = DetectionState.Idle;

            if (agent != null)
                agent.speed = settings.patrolSpeed;
        }

        protected override void ExecuteBehavior()
        {
            if (agent == null || player == null || owner == null || settings == null) return;

            Vector3 ownerXZ = new Vector3(owner.position.x, 0f, owner.position.z);
            Vector3 playerXZ = new Vector3(player.position.x, 0f, player.position.z);
            float distXZ = Vector3.Distance(ownerXZ, playerXZ);

            bool canSeePlayer = IsPlayerInFieldOfView();

            switch (detectionState)
            {
                case DetectionState.Idle:
                    HandleIdleState(canSeePlayer);
                    break;

                case DetectionState.Chasing:
                    HandleChasingState(canSeePlayer, distXZ);
                    break;

                case DetectionState.Investigating:
                    HandleInvestigatingState(canSeePlayer);
                    break;

                case DetectionState.Lost:
                    HandleLostState(canSeePlayer);
                    break;
            }
        }

        private void HandleIdleState(bool canSeePlayer)
        {
            canAttack = false;
            agent.isStopped = true;

            if (canSeePlayer)
            {
                if (TryStartChaseWithTurn())
                {
                    detectionState = DetectionState.Chasing;
                    return;
                }
                StartChasing();
            }
        }

        private void HandleChasingState(bool canSeePlayer, float distXZ)
        {
            if (canSeePlayer)
            {
                UpdateLastKnownPosition();
                AlertNearbyEnemies();

                float slamRange = Mathf.Max(0.01f, settings.slamTriggerRadius);

                if (distXZ <= slamRange && Time.time >= nextSlamTime)
                {
                    agent.isStopped = true;
                    RotateTowardsPlayerSmooth();
                    TriggerSlam();
                    nextSlamTime = Time.time + Mathf.Max(0.01f, settings.attackCooldown);
                    canAttack = true;
                }
                else
                {
                    agent.isStopped = false;
                    agent.speed = settings.chaseSpeed;
                    Vector3 destination = CalculateTrajectoryDestination(player.position);
                    agent.SetDestination(destination);
                    canAttack = false;
                }
            }
            else
            {
                // Perte de vue
                detectionState = DetectionState.Investigating;
                investigationTimer = settings.investigationTime;
                agent.SetDestination(lastKnownPlayerPosition);
                agent.isStopped = false;
                canAttack = false;
                ResetTrajectory();
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
            agent.isStopped = true;
            agent.velocity = Vector3.zero;

            if (canSeePlayer)
            {
                StartChasing();
            }
            else
            {
                detectionState = DetectionState.Idle;
            }
        }

        private void StartChasing()
        {
            detectionState = DetectionState.Chasing;
            agent.speed = settings.chaseSpeed;
            agent.isStopped = false;
            UpdateLastKnownPosition();
            ResetTrajectory();
        }

        private void TriggerSlam()
        {
            if (settings.slamZonePrefab == null)
            {
                Debug.LogWarning("[GroundSlamBehavior] slamZonePrefab manquant pour " + owner?.name);
                return;
            }

            Vector3 spawnPos = GetSlamSpawnPosition();
            Quaternion rot = Quaternion.identity;

            GameObject zone = Object.Instantiate(settings.slamZonePrefab, spawnPos, rot);

            var sphere = zone.GetComponent<SphereCollider>();
            if (sphere != null)
            {
                sphere.isTrigger = true;
                sphere.radius = Mathf.Max(0.01f, settings.slamTriggerRadius);
            }
            var box = zone.GetComponent<BoxCollider>();
            if (box != null) box.isTrigger = true;
            var capsule = zone.GetComponent<CapsuleCollider>();
            if (capsule != null)
            {
                capsule.isTrigger = true;
                capsule.radius = Mathf.Max(0.01f, settings.slamTriggerRadius);
            }

            var dmgZone = zone.GetComponent<SlamDamageZone>();
            if (dmgZone != null)
            {
                dmgZone.Init(
                    damagePerSecond: settings.attackDamage / Mathf.Max(0.1f, settings.slamLifetime),
                    lifetime: settings.slamLifetime
                );
            }
        }

        private Vector3 GetSlamSpawnPosition()
        {
            Vector3 from = owner.position + Vector3.up * 1f;
            if (Physics.Raycast(from, Vector3.down, out RaycastHit hit, 5f, ~0, QueryTriggerInteraction.Ignore))
                return hit.point + Vector3.up * settings.slamYOffset;

            return owner.position + Vector3.up * settings.slamYOffset;
        }

        public override bool CanAttack() => canAttack;
        public override bool IsChasing() => detectionState == DetectionState.Chasing;
        public override bool IsPatrolling() => false;

        public override void OnDamageTaken()
        {
            if (player != null && detectionState != DetectionState.Chasing)
            {
                UpdateLastKnownPosition();
                StartChasing();
            }
        }

        public override void DrawGizmos()
        {
            if (owner == null || settings == null) return;
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(owner.position, settings.detectionRange);
            
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(owner.position, Mathf.Max(0.01f, settings.slamTriggerRadius));

            // Cône de vision
            float halfAngle = settings.viewAngle / 2f;
            Vector3 leftDir = Quaternion.Euler(0, -halfAngle, 0) * owner.forward;
            Vector3 rightDir = Quaternion.Euler(0, halfAngle, 0) * owner.forward;
            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawRay(owner.position + Vector3.up * settings.eyeHeight, leftDir * settings.detectionRange);
            Gizmos.DrawRay(owner.position + Vector3.up * settings.eyeHeight, rightDir * settings.detectionRange);

            // Dernière position connue
            if (detectionState == DetectionState.Investigating)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(lastKnownPlayerPosition, 0.5f);
            }
        }
    }
}
