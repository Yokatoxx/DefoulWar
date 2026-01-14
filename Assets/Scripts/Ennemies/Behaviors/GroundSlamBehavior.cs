using UnityEngine;
using UnityEngine.AI;
using Ennemies.Settings;
using Ennemies.Effect;

namespace Ennemies.Behaviors
{
    public class GroundSlamBehavior : BaseEnemyBehavior
    {
        private float nextSlamTime;
        private bool canAttack;
        private bool isChasing;

        public override void Initialize(NavMeshAgent agent, Transform player, EnemyBehaviorSettings settings, Transform owner)
        {
            base.Initialize(agent, player, settings, owner);

            nextSlamTime = 0f;
            canAttack = false;
            isChasing = false;

            if (agent != null)
                agent.speed = settings.patrolSpeed;
        }

        protected override bool IsCurrentlyChasing() => isChasing;

        protected override void ExecuteBehavior()
        {
            if (agent == null || player == null || owner == null || settings == null) return;

            Vector3 ownerXZ = new Vector3(owner.position.x, 0f, owner.position.z);
            Vector3 playerXZ = new Vector3(player.position.x, 0f, player.position.z);
            float distXZ = Vector3.Distance(ownerXZ, playerXZ);

            bool detected = distXZ <= settings.detectionRange && (!settings.requireLineOfSight || CheckLineOfSight());

            if (!detected)
            {
                isChasing = false;
                agent.isStopped = true;
                canAttack = false;
                return;
            }

            isChasing = true;

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
                agent.SetDestination(player.position);
                canAttack = false;
            }
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
        public override bool IsChasing() => isChasing;
        public override bool IsPatrolling() => false;

        public override void OnDamageTaken() { }

        public override void DrawGizmos()
        {
            if (owner == null || settings == null) return;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(owner.position, settings.detectionRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(owner.position, Mathf.Max(0.01f, settings.slamTriggerRadius));
        }
    }
}
