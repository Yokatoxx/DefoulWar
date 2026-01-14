using UnityEngine;
using UnityEngine.AI;
using Ennemies.Settings;

namespace Ennemies.Behaviors
{
    /// <summary>
    /// Comportement d'ennemi qui maintient une distance fixe avec le joueur.
    /// Recule si le joueur est trop proche, avance s'il est trop loin.
    /// </summary>
    public class DistanceBehavior : BaseEnemyBehavior
    {
        private bool isChasing;
        private bool canAttack;

        public override void Initialize(NavMeshAgent agent, Transform player, EnemyBehaviorSettings settings, Transform owner)
        {
            base.Initialize(agent, player, settings, owner);
            this.isChasing = false;
        }

        protected override void ExecuteBehavior()
        {
            if (agent == null || player == null) return;

            float distanceToPlayer = Vector3.Distance(owner.position, player.position);
            bool hasLineOfSight = !settings.requireLineOfSight || CheckLineOfSight();

            // Détection du joueur
            if (distanceToPlayer <= settings.detectionRange && hasLineOfSight)
            {
                // Si on vient de détecter le joueur, vérifier si on doit tourner d'abord
                if (!isChasing)
                {
                    if (TryStartChaseWithTurn())
                    {
                        isChasing = true;
                        return; // On tourne d'abord
                    }
                }
                
                isChasing = true;
                agent.speed = settings.chaseSpeed;

                float minDistance = settings.keepDistance - settings.distanceTolerance;
                float maxDistance = settings.keepDistance + settings.distanceTolerance;

                // Toujours viser le joueur quand il est détecté
                RotateTowardsPlayerSmooth();

                if (distanceToPlayer < minDistance)
                {
                    // Trop proche, reculer tout en visant
                    Vector3 directionAway = (owner.position - player.position).normalized;
                    Vector3 targetPosition = owner.position + directionAway * settings.chaseSpeed;
                    
                    if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, settings.chaseSpeed, NavMesh.AllAreas))
                    {
                        agent.SetDestination(hit.position);
                    }
                    agent.isStopped = false;
                    
                    // Peut quand même attaquer si dans la portée d'attaque
                    canAttack = distanceToPlayer <= settings.attackRange && hasLineOfSight;
                }
                else if (distanceToPlayer > maxDistance)
                {
                    // Trop loin, avancer vers le joueur
                    Vector3 directionToPlayer = (player.position - owner.position).normalized;
                    Vector3 targetPosition = player.position - directionToPlayer * settings.keepDistance;
                    
                    if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, settings.keepDistance, NavMesh.AllAreas))
                    {
                        agent.SetDestination(hit.position);
                    }
                    agent.isStopped = false;
                    
                    // Peut attaquer si dans la portée d'attaque
                    canAttack = distanceToPlayer <= settings.attackRange && hasLineOfSight;
                }
                else
                {
                    // Distance correcte, rester en place
                    agent.isStopped = true;
                    
                    // Peut attaquer si dans la portée d'attaque
                    canAttack = distanceToPlayer <= settings.attackRange && hasLineOfSight;
                }
            }
            else
            {
                // Joueur hors de portée de détection ou pas de ligne de vue
                isChasing = false;
                canAttack = false;
                agent.isStopped = true;
            }
        }

        public override bool CanAttack() => canAttack;
        public override bool IsChasing() => isChasing;
        public override bool IsPatrolling() => false;

        public override void OnDamageTaken() { } // Pas d'effet spécial pour ce comportement

        public override void DrawGizmos()
        {
            if (owner == null) return;

            // Zone de détection
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(owner.position, settings.detectionRange);

            // Distance à maintenir
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(owner.position, settings.keepDistance);

            // Portée d'attaque
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(owner.position, settings.attackRange);

            // Zone de tolérance
            Gizmos.color = new Color(0, 1, 0, 0.3f);
            Gizmos.DrawWireSphere(owner.position, settings.keepDistance - settings.distanceTolerance);
            Gizmos.DrawWireSphere(owner.position, settings.keepDistance + settings.distanceTolerance);
        }
    }
}
