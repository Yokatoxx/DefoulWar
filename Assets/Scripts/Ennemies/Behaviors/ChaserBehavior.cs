using UnityEngine;
using UnityEngine.AI;
using Ennemies.Settings;
using Ennemies.Effect;

namespace Ennemies.Behaviors
{
    /// <summary>
    /// Comportement d'ennemi qui poursuit le joueur.
    /// Utilise le système de vision avec FOV et mémoire de position.
    /// </summary>
    public class ChaserBehavior : BaseEnemyBehavior
    {
        private EnemyShield shield;
        private bool canAttack;

        private const float AGGRO_LOSS_MULTIPLIER = 1.5f;

        public override void Initialize(NavMeshAgent agent, Transform player, EnemyBehaviorSettings settings, Transform owner)
        {
            base.Initialize(agent, player, settings, owner);
            detectionState = DetectionState.Idle;
        }

        protected override void ExecuteBehavior()
        {
            if (agent == null || player == null) return;

            float distanceToPlayer = Vector3.Distance(owner.position, player.position);
            bool canSeePlayer = IsPlayerInFieldOfView();

            switch (detectionState)
            {
                case DetectionState.Idle:
                    HandleIdleState(canSeePlayer);
                    break;

                case DetectionState.Chasing:
                    HandleChasingState(canSeePlayer, distanceToPlayer);
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
            
            if (canSeePlayer)
            {
                // Joueur détecté - vérifier si on doit tourner d'abord
                if (TryStartChaseWithTurn())
                {
                    detectionState = DetectionState.Chasing;
                    return;
                }
                
                StartChasing();
            }
        }

        private void HandleChasingState(bool canSeePlayer, float distanceToPlayer)
        {
            if (canSeePlayer)
            {
                // On voit toujours le joueur
                UpdateLastKnownPosition();
                AlertNearbyEnemies();

                if (distanceToPlayer <= settings.attackRange)
                {
                    // À portée d'attaque
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
                // Perte de vue - passer en investigation
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
                // Joueur retrouvé !
                StartChasing();
                return;
            }

            // Aller vers la dernière position connue
            if (!HasReachedLastKnownPosition())
            {
                agent.SetDestination(lastKnownPlayerPosition);
                agent.isStopped = false;
            }
            else
            {
                // Arrivé à la dernière position, regarder autour
                agent.isStopped = true;
                
                if (UpdateInvestigationTimer())
                {
                    // Temps écoulé, perdre la cible
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
                // Revenir à Idle après un court délai
                detectionState = DetectionState.Idle;
            }
        }

        private void StartChasing()
        {
            detectionState = DetectionState.Chasing;
            agent.speed = settings.chaseSpeed;
            agent.isStopped = false;
            UpdateLastKnownPosition();
        }

        public override bool CanAttack() => canAttack;
        public override bool IsChasing() => detectionState == DetectionState.Chasing;
        public override bool IsPatrolling() => false;

        public override void OnDamageTaken()
        {
            // Quand touché, on sait où est le joueur
            if (player != null && detectionState != DetectionState.Chasing)
            {
                UpdateLastKnownPosition();
                StartChasing();
            }
        }

        public override void DrawGizmos()
        {
            if (owner == null || settings == null) return;

            // Zone de détection
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(owner.position, settings.detectionRange);

            // Zone d'écoute (360°)
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(owner.position, settings.hearingRange);

            // Portée d'attaque
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(owner.position, settings.attackRange);

            // Dessiner le cône de vision
            DrawFieldOfViewGizmo();

            // Dernière position connue
            if (detectionState == DetectionState.Investigating)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawWireSphere(lastKnownPlayerPosition, 0.5f);
                Gizmos.DrawLine(owner.position, lastKnownPlayerPosition);
            }
        }

        private void DrawFieldOfViewGizmo()
        {
            float halfAngle = settings.viewAngle / 2f;
            Vector3 leftDir = Quaternion.Euler(0, -halfAngle, 0) * owner.forward;
            Vector3 rightDir = Quaternion.Euler(0, halfAngle, 0) * owner.forward;

            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawRay(owner.position + Vector3.up * settings.eyeHeight, leftDir * settings.detectionRange);
            Gizmos.DrawRay(owner.position + Vector3.up * settings.eyeHeight, rightDir * settings.detectionRange);
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