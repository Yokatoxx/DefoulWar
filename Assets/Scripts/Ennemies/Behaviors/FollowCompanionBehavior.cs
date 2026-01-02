using UnityEngine;
using UnityEngine.AI;
using Ennemies.Settings;

namespace Ennemies.Behaviors
{
    // Impl�mentation bas�e sur la logique des behaviors existants (Chaser/Distance/ZonePatrol)
    public class FollowCompanionBehavior : IEnemyBehavior
    {
        private NavMeshAgent agent;
        private Transform owner;
        private EnemyBehaviorSettings settings;

        private Transform companionTarget;
        private float nextSearchTime;
        private const float SEARCH_INTERVAL = 1.0f;

        private bool isFollowing;
        private bool canAttack;

        public void Initialize(NavMeshAgent agent, Transform player, EnemyBehaviorSettings settings, Transform owner)
        {
            this.agent = agent;
            this.owner = owner;
            this.settings = settings;

            isFollowing = false;
            canAttack = false;
            companionTarget = null;

            if (agent != null)
            {
                agent.isStopped = false;
                agent.speed = settings.patrolSpeed; // vitesse d�escorte
            }

            nextSearchTime = Time.time + Random.Range(0f, SEARCH_INTERVAL);
        }

        public void Execute()
        {
            if (agent == null || owner == null) return;

            // Acquisition si pas de cible
            if (companionTarget == null)
            {
                isFollowing = false;

                if (Time.time >= nextSearchTime)
                {
                    nextSearchTime = Time.time + SEARCH_INTERVAL;
                    companionTarget = FindClosestEligibleCompanion();
                    isFollowing = companionTarget != null;

                    agent.isStopped = !isFollowing;
                    if (isFollowing)
                        agent.speed = Mathf.Max(agent.speed, settings.patrolSpeed);
                }
                return;
            }

            // Perte si cible d�truite/d�sactiv�e
            if (!companionTarget.gameObject.activeInHierarchy)
            {
                ClearCompanion();
                return;
            }

            // LOS optionnelle selon settings
            bool hasLOS = !settings.requireLineOfSight || CheckLineOfSight(companionTarget);
            float d = Vector3.Distance(owner.position, companionTarget.position);

            // Si pas de LOS et tr�s loin, abandon
            if (!hasLOS && d > settings.detectionRange * 1.5f)
            {
                ClearCompanion();
                return;
            }

            float keep = Mathf.Max(0.25f, settings.keepDistance);
            if (d > keep)
            {
                agent.isStopped = false;

                // viser un point autour du compagnon pour �viter le chevauchement
                Vector3 dir = (owner.position - companionTarget.position).normalized;
                if (dir.sqrMagnitude < 0.001f) dir = Random.insideUnitSphere;
                dir.y = 0f;

                Vector3 targetPos = companionTarget.position + dir * (keep * 0.8f);
                if (NavMesh.SamplePosition(targetPos, out NavMeshHit hit, 1.0f, NavMesh.AllAreas))
                    agent.SetDestination(hit.position);
                else
                    agent.SetDestination(companionTarget.position);

                RotateTowards(companionTarget.position);
            }
            else
            {
                agent.isStopped = true;
                RotateTowards(companionTarget.position);
            }

            canAttack = false;
        }

        private Transform FindClosestEligibleCompanion()
        {
            float maxRange = Mathf.Max(0.01f, settings.detectionRange);
            float bestDist = float.PositiveInfinity;
            Transform best = null;

            // On se base sur les ennemis configur�s via EnemyBehaviour
            var enemies = Object.FindObjectsByType<Ennemies.EnemyBehaviour>(FindObjectsSortMode.None);
            foreach (var eb in enemies)
            {
                if (eb == null) continue;
                if (eb.transform == owner) continue;

                // Ne pas suivre ceux qui sont eux-m�mes CompanionFollower
                if (eb.Settings != null && eb.Settings.behaviorType == EnemyBehaviorType.CompanionFollower)
                    continue;

                float dist = Vector3.Distance(owner.position, eb.transform.position);
                if (dist > maxRange) continue;

                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = eb.transform;
                }
            }

            return best;
        }

        private void ClearCompanion()
        {
            companionTarget = null;
            isFollowing = false;
            agent.isStopped = true;
        }

        private bool CheckLineOfSight(Transform target)
        {
            Vector3 eye = owner.position + Vector3.up * settings.eyeHeight;
            Vector3 tgt = target.position + Vector3.up * 1f;
            Vector3 dir = tgt - eye;
            float dist = dir.magnitude;

            if (Physics.Raycast(eye, dir.normalized, out RaycastHit hit, dist, settings.obstacleLayer))
            {
                return hit.transform == target || hit.transform.IsChildOf(target);
            }
            return true;
        }

        private void RotateTowards(Vector3 pos)
        {
            Vector3 dir = (pos - owner.position).normalized;
            dir.y = 0f;
            if (dir != Vector3.zero)
            {
                Quaternion look = Quaternion.LookRotation(dir);
                owner.rotation = Quaternion.Slerp(owner.rotation, look, Time.deltaTime * settings.rotationSpeed);
            }
        }

        public bool CanAttack() => canAttack;
        public bool IsChasing() => false;
        public bool IsPatrolling() => !isFollowing && companionTarget == null;
        public void OnDamageTaken() { }
        public void DrawGizmos()
        {
            if (owner == null || settings == null) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(owner.position, settings.detectionRange);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(owner.position, Mathf.Max(0.25f, settings.keepDistance));
        }
    }
}
