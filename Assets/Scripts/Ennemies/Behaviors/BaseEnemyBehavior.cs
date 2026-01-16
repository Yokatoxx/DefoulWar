using UnityEngine;
using UnityEngine.AI;
using Ennemies.Settings;

namespace Ennemies.Behaviors
{
    /// <summary>
    /// Classe abstraite de base pour tous les comportements d'ennemis.
    /// Gère automatiquement la rotation vers le joueur avant de commencer le déplacement.
    /// </summary>
    public abstract class BaseEnemyBehavior : IEnemyBehavior
    {
        protected NavMeshAgent agent;
        protected Transform player;
        protected Transform owner;
        protected EnemyBehaviorSettings settings;

        // État de rotation initiale
        private bool isTurningTowardsPlayer;
        private bool wasUpdateRotationEnabled;
        
        /// <summary>
        /// Angle en degrés en dessous duquel on considère que l'ennemi fait face au joueur.
        /// </summary>
        protected virtual float TurnAngleThreshold => 15f;
        
        /// <summary>
        /// Si true, l'ennemi doit tourner vers le joueur avant de se déplacer lors de la détection initiale.
        /// Les behaviors peuvent override cette propriété pour désactiver ce comportement.
        /// </summary>
        protected virtual bool RequiresTurnBeforeMove => true;

        public virtual void Initialize(NavMeshAgent agent, Transform player, EnemyBehaviorSettings settings, Transform owner)
        {
            this.agent = agent;
            this.player = player;
            this.settings = settings;
            this.owner = owner;
            
            isTurningTowardsPlayer = false;
            wasUpdateRotationEnabled = agent != null && agent.updateRotation;

            // Appliquer les paramètres de réactivité du NavMeshAgent
            if (agent != null)
            {
                agent.acceleration = settings.acceleration;
                agent.angularSpeed = settings.angularSpeed;
                agent.autoBraking = settings.autoBraking;
            }
        }

        public void Execute()
        {
            if (agent == null || player == null || owner == null) return;

            // Phase de rotation initiale en cours
            if (isTurningTowardsPlayer)
            {
                TurnTowardsPlayer();
                
                float angleToPlayer = GetAngleToPlayer();
                if (angleToPlayer <= TurnAngleThreshold)
                {
                    // Rotation terminée, on peut passer au comportement normal
                    EndTurningPhase();
                }
                else
                {
                    // Toujours en train de tourner, on ne fait rien d'autre
                    return;
                }
            }
            
            // Exécuter la logique de comportement spécifique
            ExecuteBehavior();
        }

        /// <summary>
        /// Appelé par les behaviors quand ils commencent à poursuivre le joueur.
        /// Déclenche la phase de rotation si nécessaire.
        /// Retourne true si une phase de rotation a été démarrée (le behavior ne doit pas bouger).
        /// </summary>
        protected bool TryStartChaseWithTurn()
        {
            if (!RequiresTurnBeforeMove) return false;
            if (isTurningTowardsPlayer) return true; // Déjà en train de tourner
            
            float angleToPlayer = GetAngleToPlayer();
            if (angleToPlayer > TurnAngleThreshold)
            {
                StartTurningPhase();
                return true; // Ne pas bouger, on tourne d'abord
            }
            
            return false; // Pas besoin de tourner, le behavior peut bouger
        }

        /// <summary>
        /// Démarre la phase de rotation avec arrêt immédiat.
        /// </summary>
        private void StartTurningPhase()
        {
            isTurningTowardsPlayer = true;
            
            // Sauvegarder l'état de rotation du NavMeshAgent
            wasUpdateRotationEnabled = agent.updateRotation;
            
            // Désactiver la rotation automatique du NavMeshAgent pendant qu'on tourne manuellement
            agent.updateRotation = false;
            
            // Arrêt immédiat - pas d'effet "voiture qui freine"
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.ResetPath();
        }

        /// <summary>
        /// Termine la phase de rotation et restaure l'état normal.
        /// </summary>
        private void EndTurningPhase()
        {
            isTurningTowardsPlayer = false;
            
            // Restaurer la rotation automatique du NavMeshAgent
            agent.updateRotation = wasUpdateRotationEnabled;
            
            // S'assurer que l'agent peut bouger
            agent.isStopped = false;
        }

        /// <summary>
        /// Retourne l'angle en degrés entre la direction actuelle et la direction vers le joueur.
        /// </summary>
        protected float GetAngleToPlayer()
        {
            Vector3 directionToPlayer = (player.position - owner.position).normalized;
            directionToPlayer.y = 0;
            
            Vector3 forward = owner.forward;
            forward.y = 0;
            
            if (directionToPlayer.sqrMagnitude < 0.001f || forward.sqrMagnitude < 0.001f)
                return 0f;
            
            return Vector3.Angle(forward.normalized, directionToPlayer.normalized);
        }

        /// <summary>
        /// Tourne progressivement l'ennemi vers le joueur.
        /// </summary>
        protected void TurnTowardsPlayer()
        {
            Vector3 direction = (player.position - owner.position).normalized;
            direction.y = 0;

            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                // Utiliser une vitesse de rotation plus élevée pour la phase initiale
                float turnSpeed = settings.rotationSpeed * 2f;
                owner.rotation = Quaternion.Slerp(owner.rotation, lookRotation, Time.deltaTime * turnSpeed);
            }
        }

        /// <summary>
        /// Retourne true si l'ennemi est en phase de rotation initiale.
        /// </summary>
        protected bool IsTurning => isTurningTowardsPlayer;

        /// <summary>
        /// Logique de comportement spécifique à implémenter par les classes dérivées.
        /// </summary>
        protected abstract void ExecuteBehavior();

        // Implémentations de l'interface
        public abstract bool CanAttack();
        public abstract bool IsChasing();
        public abstract bool IsPatrolling();
        public abstract void OnDamageTaken();
        public abstract void DrawGizmos();

        /// <summary>
        /// Méthode utilitaire pour la rotation continue vers le joueur (utilisée pendant le combat).
        /// </summary>
        protected void RotateTowardsPlayerSmooth()
        {
            Vector3 direction = (player.position - owner.position).normalized;
            direction.y = 0;

            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                owner.rotation = Quaternion.Slerp(owner.rotation, lookRotation, Time.deltaTime * settings.rotationSpeed);
            }
        }

        /// <summary>
        /// Vérifie la ligne de vue vers le joueur.
        /// </summary>
        protected bool CheckLineOfSight()
        {
            Vector3 eyePosition = owner.position + Vector3.up * settings.eyeHeight;
            Vector3 targetPosition = player.position + Vector3.up * 1f;
            Vector3 direction = targetPosition - eyePosition;
            float distance = direction.magnitude;

            if (Physics.Raycast(eyePosition, direction.normalized, out RaycastHit hit, distance, settings.obstacleLayer))
            {
                return hit.transform == player || hit.transform.IsChildOf(player);
            }
            
            return true;
        }
    }
}
