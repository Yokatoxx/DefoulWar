using UnityEngine;
using UnityEngine.AI;
using Ennemies.Settings;
using Ennemies.Effect;

namespace Ennemies.Behaviors
{
    public class GroundSlamBehavior : IEnemyBehavior
    {
        private NavMeshAgent agent;
        private Transform player;
        private Transform owner;
        private EnemyBehaviorSettings settings;

        private float nextSlamTime;
        private bool canAttack;

        public void Initialize(NavMeshAgent agent, Transform player, EnemyBehaviorSettings settings, Transform owner)
        {
            this.agent = agent;
            this.player = player;
            this.settings = settings;
            this.owner = owner;

            nextSlamTime = 0f;
            canAttack = false;

            if (agent != null)
                agent.speed = settings.patrolSpeed;
        }

        public void Execute()
        {
            if (agent == null || player == null || owner == null || settings == null) return;

            // Distance horizontale (XZ) pour éviter l'influence de la hauteur
            Vector3 ownerXZ = new Vector3(owner.position.x, 0f, owner.position.z);
            Vector3 playerXZ = new Vector3(player.position.x, 0f, player.position.z);
            float distXZ = Vector3.Distance(ownerXZ, playerXZ);

            bool detected = distXZ <= settings.detectionRange && (!settings.requireLineOfSight || CheckLineOfSight());

            if (!detected)
            {
                agent.isStopped = true;
                canAttack = false;
                return;
            }

            // Utiliser une portée spécifique au slam
            float slamRange = Mathf.Max(0.01f, settings.slamTriggerRadius);

            // Si à portée slam et cooldown OK -> slam
            if (distXZ <= slamRange && Time.time >= nextSlamTime)
            {
                agent.isStopped = true;
                RotateTowards(player.position);
                TriggerSlam();
                nextSlamTime = Time.time + Mathf.Max(0.01f, settings.attackCooldown);
                canAttack = true;
            }
            else
            {
                // Approche jusqu'à entrer dans la portée slam
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
                Debug.LogWarning($"[GroundSlamBehavior] slamZonePrefab manquant pour {owner?.name}.");
                return;
            }

            Vector3 spawnPos = GetSlamSpawnPosition();
            Quaternion rot = Quaternion.identity;

            GameObject zone = Object.Instantiate(settings.slamZonePrefab, spawnPos, rot);

            // Appliquer le rayon si un collider existe
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
            // Placer sur le sol sous l'ennemi
            Vector3 from = owner.position + Vector3.up * 1f;
            if (Physics.Raycast(from, Vector3.down, out RaycastHit hit, 5f, ~0, QueryTriggerInteraction.Ignore))
                return hit.point + Vector3.up * settings.slamYOffset;

            return owner.position + Vector3.up * settings.slamYOffset;
        }

        private bool CheckLineOfSight()
        {
            Vector3 eye = owner.position + Vector3.up * settings.eyeHeight;
            Vector3 tgt = player.position + Vector3.up * 1f;
            Vector3 dir = tgt - eye;
            float dist = dir.magnitude;
            if (Physics.Raycast(eye, dir.normalized, out RaycastHit hit, dist, settings.obstacleLayer))
                return hit.transform == player || hit.transform.IsChildOf(player);
            return true;
        }

        private void RotateTowards(Vector3 worldPos)
        {
            Vector3 direction = (worldPos - owner.position).normalized;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
            {
                var look = Quaternion.LookRotation(direction);
                owner.rotation = Quaternion.Slerp(owner.rotation, look, Time.deltaTime * settings.rotationSpeed);
            }
        }

        public bool CanAttack() => canAttack;
        public bool IsChasing() => false;
        public bool IsPatrolling() => false;

        public void OnDamageTaken() { }

        public void DrawGizmos()
        {
            if (owner == null || settings == null) return;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(owner.position, settings.detectionRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(owner.position, Mathf.Max(0.01f, settings.slamTriggerRadius)); // gizmo de portée slam
        }
    }
}