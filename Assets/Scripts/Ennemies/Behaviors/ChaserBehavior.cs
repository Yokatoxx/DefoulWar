using UnityEngine;
using UnityEngine.AI;
using Ennemies.Settings;

namespace Ennemies.Behaviors
{
    /// <summary>
    /// Comportement d'ennemi qui poursuit le joueur en permanence.
    /// Perd l'aggro si le joueur sort de 1.5x la distance de détection.
    /// </summary>
    public class ChaserBehavior : IEnemyBehavior
    {
        private NavMeshAgent agent;
        private Transform player;
        private Transform owner;
        private EnemyBehaviorSettings settings;

        private bool isChasing;
        private bool canAttack;

        private const float AGGRO_LOSS_MULTIPLIER = 1.5f;

        public void Initialize(NavMeshAgent agent, Transform player, EnemyBehaviorSettings settings, Transform owner)
        {
            this.agent = agent;
            this.player = player;
            this.settings = settings;
            this.owner = owner;
            this.isChasing = false;
        }

        public void Execute()
        {
            if (agent == null || player == null) return;

            float distanceToPlayer = Vector3.Distance(owner.position, player.position);
            float aggroLossDistance = settings.detectionRange * AGGRO_LOSS_MULTIPLIER;

            bool hasLineOfSight = !settings.requireLineOfSight || CheckLineOfSight();

            // Gestion de l'état de poursuite
            if (!isChasing && distanceToPlayer <= settings.detectionRange && hasLineOfSight)
            {
                // Détection du joueur, commencer la poursuite
                isChasing = true;
                agent.speed = settings.chaseSpeed;
            }
            else if (isChasing && (distanceToPlayer > aggroLossDistance || !hasLineOfSight))
            {
                // Joueur trop loin ou plus visible, perdre l'aggro
                isChasing = false;
                agent.isStopped = true;
            }

            if (isChasing)
            {
                // En poursuite
                if (distanceToPlayer <= settings.attackRange)
                {
                    // À portée d'attaque, s'arrêter et attaquer
                    agent.isStopped = true;
                    canAttack = true;
                    RotateTowardsPlayer();
                }
                else
                {
                    // Continuer la poursuite
                    agent.isStopped = false;
                    agent.SetDestination(player.position);
                    canAttack = false;
                }
            }
            else
            {
                canAttack = false;
            }
        }

        private bool CheckLineOfSight()
        {
            Vector3 eyePosition = owner.position + Vector3.up * settings.eyeHeight;
            Vector3 targetPosition = player.position + Vector3.up * 1f; // Viser le centre du joueur
            Vector3 direction = targetPosition - eyePosition;
            float distance = direction.magnitude;

            if (Physics.Raycast(eyePosition, direction.normalized, out RaycastHit hit, distance, settings.obstacleLayer))
            {
                // On a touché quelque chose, vérifier si c'est le joueur
                return hit.transform == player || hit.transform.IsChildOf(player);
            }
            
            // Rien n'a bloqué le raycast, on a une ligne de vue
            return true;
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

        public bool CanAttack() => canAttack;
        public bool IsChasing() => isChasing;
        public bool IsPatrolling() => false;

        public void OnDamageTaken() { } // Pas d'effet spécial pour ce comportement

        public void DrawGizmos()
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
    }
}

