using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Ennemies.Settings;

namespace Ennemies.Behaviors
{
    /// <summary>
    /// Comportement d'ennemi qui suit un autre ennemi (compagnon) s'il est disponible.
    /// </summary>
    public class FollowCompanionBehavior : BaseEnemyBehavior
    {
        private Transform companionTarget;
        private float nextSearchTime;
        private const float SEARCH_INTERVAL = 1.0f;

        private bool isFollowing;
        private bool canAttack;

        // Comptage global des followers par compagnon
        private static readonly Dictionary<Transform, int> FollowersPerCompanion = new Dictionary<Transform, int>();
        private static int GetFollowersCount(Transform t) => (t != null && FollowersPerCompanion.TryGetValue(t, out var c)) ? c : 0;
        private static void IncFollower(Transform t)
        {
            if (t == null) return;
            FollowersPerCompanion[t] = GetFollowersCount(t) + 1;
        }
        private static void DecFollower(Transform t)
        {
            if (t == null) return;
            var c = GetFollowersCount(t) - 1;
            if (c <= 0) FollowersPerCompanion.Remove(t);
            else FollowersPerCompanion[t] = c;
        }

        // Ce behavior ne poursuit pas le joueur, donc pas de turn-before-move
        protected override bool RequiresTurnBeforeMove => false;

        public override void Initialize(NavMeshAgent agent, Transform player, EnemyBehaviorSettings settings, Transform owner)
        {
            base.Initialize(agent, player, settings, owner);

            isFollowing = false;
            canAttack = false;
            companionTarget = null;

            if (agent != null)
            {
                agent.isStopped = false;
                agent.speed = settings.patrolSpeed;
            }

            nextSearchTime = Time.time + Random.Range(0f, SEARCH_INTERVAL);
        }

        protected override void ExecuteBehavior()
        {
            if (agent == null || owner == null) return;

            if (companionTarget == null)
            {
                isFollowing = false;

                if (Time.time >= nextSearchTime)
                {
                    nextSearchTime = Time.time + SEARCH_INTERVAL;

                    // Recherche qui évite les compagnons déjà suivis, si possible
                    companionTarget = FindBestCompanionAvoidingStack();

                    if (companionTarget != null)
                    {
                        // Vérifier la limite
                        if (settings.maxFollowersPerCompanion > 0 &&
                            GetFollowersCount(companionTarget) >= settings.maxFollowersPerCompanion)
                        {
                            companionTarget = null; // sur-capacité
                        }
                    }

                    if (companionTarget != null)
                    {
                        IncFollower(companionTarget);
                        isFollowing = true;
                        agent.isStopped = false;
                        agent.speed = Mathf.Max(agent.speed, settings.patrolSpeed);
                    }
                    else
                    {
                        agent.isStopped = true;
                    }
                }
                return;
            }

            // Perte si cible detruite/desactivee
            if (!companionTarget.gameObject.activeInHierarchy)
            {
                ClearCompanion();
                return;
            }

            // LOS optionnelle selon settings
            bool hasLOS = !settings.requireLineOfSight || CheckLineOfSightTo(companionTarget);
            float d = Vector3.Distance(owner.position, companionTarget.position);

            // Si pas de LOS et tres loin, abandon
            if (!hasLOS && d > settings.detectionRange * 1.5f)
            {
                ClearCompanion();
                return;
            }

            float keep = Mathf.Max(0.25f, settings.keepDistance);
            if (d > keep)
            {
                agent.isStopped = false;

                // viser un point autour du compagnon pour eviter le chevauchement
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

// Remplacez FindBestCompanionAvoidingStack() par une version qui évite des tags si possible
private Transform FindBestCompanionAvoidingStack()
{
    float maxRange = Mathf.Max(0.01f, settings.detectionRange);

    // Helper: test si un eb a un tag à éviter
    bool IsAvoided(Ennemies.EnemyBehaviour eb)
    {
        if (settings.avoidedCompanionTags == null || settings.avoidedCompanionTags.Length == 0) return false;
        foreach (var tag in settings.avoidedCompanionTags)
        {
            if (!string.IsNullOrEmpty(tag) && eb.gameObject.CompareTag(tag))
                return true;
        }
        return false;
    }

    // 1) Essais par tags préférés, en minimisant les followers et en excluant les 'évités' si possible
    if (settings.preferredCompanionTags != null && settings.preferredCompanionTags.Length > 0)
    {
        foreach (var tag in settings.preferredCompanionTags)
        {
            var best = FindClosestByPredicateMinFollowers(maxRange, eb =>
            {
                if (eb == null || eb.transform == owner) return false;
                if (eb.Settings != null && eb.Settings.behaviorType == EnemyBehaviorType.CompanionFollower) return false;
                if (!eb.gameObject.CompareTag(tag)) return false;
                // Éviter les tags si d’autres existent: le filtre d’abord les non-évités
                if (IsAvoided(eb)) return false;
                return true;
            });
            if (best != null) return best;
        }

        // Fallback: si autorisé, prendre n’importe quel éligible non-évité
        if (!settings.restrictToPreferredTagsOnly)
        {
            var anyNonAvoided = FindClosestByPredicateMinFollowers(maxRange, eb =>
            {
                if (eb == null || eb.transform == owner) return false;
                if (eb.Settings != null && eb.Settings.behaviorType == EnemyBehaviorType.CompanionFollower) return false;
                if (IsAvoided(eb)) return false;
                return true;
            });
            if (anyNonAvoided != null) return anyNonAvoided;

            // Dernier recours: accepter un 'évité' uniquement si strictement rien d’autre
            if (!settings.avoidTagsStrict)
            {
                var anyAvoided = FindClosestByPredicateMinFollowers(maxRange, eb =>
                {
                    if (eb == null || eb.transform == owner) return false;
                    if (eb.Settings != null && eb.Settings.behaviorType == EnemyBehaviorType.CompanionFollower) return false;
                    return IsAvoided(eb);
                });
                if (anyAvoided != null) return anyAvoided;
            }
        }
        return null;
    }

    // 2) Pas de tags préférés: d’abord non-évités
    var bestNonAvoided = FindClosestByPredicateMinFollowers(maxRange, eb =>
    {
        if (eb == null || eb.transform == owner) return false;
        if (eb.Settings != null && eb.Settings.behaviorType == EnemyBehaviorType.CompanionFollower) return false;
        if (IsAvoided(eb)) return false;
        return true;
    });
    if (bestNonAvoided != null) return bestNonAvoided;

    // 3) Dernier recours: accepter un 'évité' seulement si rien d’autre
    if (!settings.avoidTagsStrict)
    {
        var bestAvoided = FindClosestByPredicateMinFollowers(maxRange, eb =>
        {
            if (eb == null || eb.transform == owner) return false;
            if (eb.Settings != null && eb.Settings.behaviorType == EnemyBehaviorType.CompanionFollower) return false;
            return IsAvoided(eb);
        });
        if (bestAvoided != null) return bestAvoided;
    }

    return null;
}

        private Transform FindClosestByPredicateMinFollowers(float maxRange, System.Func<Ennemies.EnemyBehaviour, bool> predicate)
        {
            float bestDist = float.PositiveInfinity;
            int bestFollowers = int.MaxValue;
            Transform best = null;

            var enemies = Object.FindObjectsByType<Ennemies.EnemyBehaviour>(FindObjectsSortMode.None);
            foreach (var eb in enemies)
            {
                if (!predicate(eb)) continue;

                // Respecter la limite si définie
                int count = GetFollowersCount(eb.transform);
                if (settings.maxFollowersPerCompanion > 0 && count >= settings.maxFollowersPerCompanion)
                    continue;

                float dist = Vector3.Distance(owner.position, eb.transform.position);
                if (dist > maxRange) continue;

                // Prioriser le moins de followers, puis la distance
                if (count < bestFollowers || (count == bestFollowers && dist < bestDist))
                {
                    bestFollowers = count;
                    bestDist = dist;
                    best = eb.transform;
                }
            }
            return best;
        }

        private void ClearCompanion()
        {
            if (companionTarget != null)
                DecFollower(companionTarget);

            companionTarget = null;
            isFollowing = false;
            agent.isStopped = true;
        }

        private bool CheckLineOfSightTo(Transform target)
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

        public override bool CanAttack() => canAttack;
        public override bool IsChasing() => false;
        public override bool IsPatrolling() => !isFollowing && companionTarget == null;
        public override void OnDamageTaken() { }
        
        public override void ReceiveAlert(Vector3 playerPosition)
        {
            // Ce comportement ne suit pas le joueur, ignorer les alertes
        }
        
        public override void DrawGizmos()
        {
            if (owner == null || settings == null) return;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(owner.position, settings.detectionRange);
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(owner.position, Mathf.Max(0.25f, settings.keepDistance));
        }

        public Transform GetCompanionTarget() => companionTarget;
    }
}
