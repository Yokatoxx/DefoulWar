using UnityEngine;
using UnityEngine.AI;
using Ennemies.Settings;
using Ennemies.Effect;

namespace Ennemies.Behaviors
{
    /// <summary>
    /// Comportement d'ennemi qui poursuit le joueur en permanence.
    /// Perd l'aggro si le joueur sort de 1.5x la distance de détection.
    /// </summary>
    public class ChaserBehavior : BaseEnemyBehavior
    {
        private EnemyShield shield;

        private bool isChasing;
        private bool canAttack;

        private const float AGGRO_LOSS_MULTIPLIER = 1.5f;

        public override void Initialize(NavMeshAgent agent, Transform player, EnemyBehaviorSettings settings, Transform owner)
        {
            base.Initialize(agent, player, settings, owner);
            this.isChasing = false;
        }

        protected override void ExecuteBehavior()
        {
            if (agent == null || player == null) return;

            float distanceToPlayer = Vector3.Distance(owner.position, player.position);
            float aggroLossDistance = settings.detectionRange * AGGRO_LOSS_MULTIPLIER;

            bool hasLineOfSight = !settings.requireLineOfSight || CheckLineOfSight();

            // Gestion de l'état de poursuite
            if (!isChasing && distanceToPlayer <= settings.detectionRange && hasLineOfSight)
            {
                // Détection du joueur - vérifier si on doit tourner d'abord
                if (TryStartChaseWithTurn())
                {
                    // On tourne d'abord, ne pas bouger
                    isChasing = true; // Marquer comme chasing pour que la prochaine frame continue
                    return;
                }
                
                // Pas besoin de tourner, commencer la poursuite
                isChasing = true;
                agent.speed = settings.chaseSpeed;
            }
            else if (isChasing && (distanceToPlayer > aggroLossDistance || !hasLineOfSight))
            {
                // Joueur trop loin ou plus visible, perdre l'aggro
                isChasing = false;
                agent.isStopped = true;
                // Reset immédiat de la vélocité pour éviter l'effet d'inertie
                agent.velocity = Vector3.zero;
            }

            if (isChasing)
            {
                // En poursuite
                if (distanceToPlayer <= settings.attackRange)
                {
                    // À portée d'attaque, s'arrêter et attaquer
                    agent.isStopped = true;
                    canAttack = true;
                    RotateTowardsPlayerSmooth();
                }
                else
                {
                    // Continuer la poursuite
                    agent.isStopped = false;
                    agent.speed = settings.chaseSpeed;
                    agent.SetDestination(player.position);
                    canAttack = false;
                }
            }
            else
            {
                canAttack = false;
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

            // Zone de perte d'aggro
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f); // Orange transparent
            Gizmos.DrawWireSphere(owner.position, settings.detectionRange * AGGRO_LOSS_MULTIPLIER);

            // Portée d'attaque
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(owner.position, settings.attackRange);
        }

        public void SetShield(Ennemies.Effect.EnemyShield shield)
        {
            this.shield = shield;
        }

        public void SetShieldActive(bool active)
        {
            if (shield != null)
                shield.ShieldActive = active;
        }

        public EnemyShield GetShield() => shield;
    }
}