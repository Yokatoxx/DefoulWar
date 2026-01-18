using UnityEngine;
using UnityEngine.AI;
using Ennemies.Settings;

namespace Ennemies.Behaviors
{
    /// <summary>
    /// État de détection de l'ennemi.
    /// </summary>
    public enum DetectionState
    {
        Idle,           // Pas de cible connue
        Chasing,        // Poursuit le joueur (le voit)
        Investigating,  // Va vers la dernière position connue
        Lost            // A perdu la cible après investigation
    }

    /// <summary>
    /// Classe abstraite de base pour tous les comportements d'ennemis.
    /// Gère le système de vision avec angle de vue, mémoire de position et alertes.
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

        // Système de détection avancé
        protected DetectionState detectionState = DetectionState.Idle;
        protected Vector3 lastKnownPlayerPosition;
        protected float investigationTimer;
        private bool hasAlertedOthers;
        
        // Mode arène : les ennemis connaissent toujours la position du joueur
        protected bool isInArenaMode = false;
        
        /// <summary>
        /// Angle en degrés en dessous duquel on considère que l'ennemi fait face au joueur.
        /// </summary>
        protected virtual float TurnAngleThreshold => 15f;
        
        /// <summary>
        /// Si true, l'ennemi doit tourner vers le joueur avant de se déplacer lors de la détection initiale.
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
            detectionState = DetectionState.Idle;
            lastKnownPlayerPosition = Vector3.zero;
            investigationTimer = 0f;
            hasAlertedOthers = false;

            // Appliquer les paramètres de réactivité du NavMeshAgent
            if (agent != null)
            {
                agent.acceleration = settings.acceleration;
                agent.angularSpeed = settings.angularSpeed;
                agent.autoBraking = settings.autoBraking;
            }

            // Enregistrer dans le système d'alerte
            EnemyAlertSystem.Instance.RegisterEnemy(this);
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
                    EndTurningPhase();
                }
                else
                {
                    return;
                }
            }
            
            // Exécuter la logique de comportement spécifique
            ExecuteBehavior();
        }

        /// <summary>
        /// Vérifie si le joueur est dans le champ de vision (distance + angle + raycast).
        /// En mode arène, retourne toujours true.
        /// </summary>
        protected bool IsPlayerInFieldOfView()
        {
            if (player == null) return false;
            
            // En mode arène, le joueur est toujours "visible"
            if (isInArenaMode) return true;

            float distance = Vector3.Distance(owner.position, player.position);

            // 1. Vérifier la distance de détection
            if (distance > settings.detectionRange) return false;

            // 2. Vérifier si dans le rayon d'écoute (360°, court rayon)
            if (distance <= settings.hearingRange) 
            {
                // Joueur très proche = détecté (bruit)
                return !settings.requireLineOfSight || CheckLineOfSight();
            }

            // 3. Vérifier l'angle de vue
            float angle = GetAngleToPlayer();
            float halfViewAngle = settings.viewAngle / 2f;
            if (angle > halfViewAngle) return false;

            // 4. Vérifier la ligne de vue (raycast)
            if (settings.requireLineOfSight && !CheckLineOfSight()) return false;

            return true;
        }
        
        /// <summary>
        /// Active ou désactive le mode arène (bypass de la détection).
        /// </summary>
        public void SetArenaMode(bool active)
        {
            isInArenaMode = active;
            
            // Si on active le mode arène, forcer l'état de poursuite et démarrer le mouvement
            if (active && player != null && agent != null)
            {
                detectionState = DetectionState.Chasing;
                lastKnownPlayerPosition = player.position;
                
                // Démarrer immédiatement la poursuite
                agent.isStopped = false;
                agent.speed = settings.chaseSpeed;
                agent.SetDestination(player.position);
            }
        }

        /// <summary>
        /// Met à jour la dernière position connue du joueur si visible.
        /// </summary>
        protected void UpdateLastKnownPosition()
        {
            if (player != null)
            {
                lastKnownPlayerPosition = player.position;
            }
        }

        /// <summary>
        /// Alerte les ennemis proches de la position du joueur.
        /// </summary>
        protected void AlertNearbyEnemies()
        {
            if (hasAlertedOthers) return;
            
            EnemyAlertSystem.Instance.AlertEnemiesInRadius(
                owner.position, 
                lastKnownPlayerPosition, 
                settings.alertRadius
            );
            hasAlertedOthers = true;
        }

        /// <summary>
        /// Reçoit une alerte d'un autre ennemi avec la position du joueur.
        /// </summary>
        public virtual void ReceiveAlert(Vector3 playerPosition)
        {
            // Si on n'a pas de cible, utiliser la position alertée
            if (detectionState == DetectionState.Idle || detectionState == DetectionState.Lost)
            {
                lastKnownPlayerPosition = playerPosition;
                detectionState = DetectionState.Investigating;
                investigationTimer = settings.investigationTime;
            }
        }

        /// <summary>
        /// Vérifie si l'ennemi a atteint la dernière position connue.
        /// </summary>
        protected bool HasReachedLastKnownPosition()
        {
            if (agent == null) return true;
            
            float distance = Vector3.Distance(owner.position, lastKnownPlayerPosition);
            return distance <= agent.stoppingDistance + 0.5f;
        }

        /// <summary>
        /// Met à jour le timer d'investigation.
        /// Retourne true si le temps d'investigation est écoulé.
        /// </summary>
        protected bool UpdateInvestigationTimer()
        {
            if (investigationTimer > 0)
            {
                investigationTimer -= Time.deltaTime;
                return investigationTimer <= 0;
            }
            return true;
        }

        /// <summary>
        /// Réinitialise l'état d'alerte (permet d'alerter à nouveau).
        /// </summary>
        protected void ResetAlertState()
        {
            hasAlertedOthers = false;
        }

        /// <summary>
        /// Appelé par les behaviors quand ils commencent à poursuivre le joueur.
        /// </summary>
        protected bool TryStartChaseWithTurn()
        {
            if (!RequiresTurnBeforeMove) return false;
            if (isTurningTowardsPlayer) return true;
            
            float angleToPlayer = GetAngleToPlayer();
            if (angleToPlayer > TurnAngleThreshold)
            {
                StartTurningPhase();
                return true;
            }
            
            return false;
        }

        private void StartTurningPhase()
        {
            isTurningTowardsPlayer = true;
            wasUpdateRotationEnabled = agent.updateRotation;
            agent.updateRotation = false;
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.ResetPath();
        }

        private void EndTurningPhase()
        {
            isTurningTowardsPlayer = false;
            agent.updateRotation = wasUpdateRotationEnabled;
            agent.isStopped = false;
        }

        protected float GetAngleToPlayer()
        {
            Vector3 directionToPlayer = (player.position - owner.position).normalized;
            // Note: On garde Y pour permettre de voir le joueur en contrebas (sniper en hauteur)
            
            Vector3 forward = owner.forward;
            // Pour la comparaison, on utilise une direction 3D complète
            
            if (directionToPlayer.sqrMagnitude < 0.001f || forward.sqrMagnitude < 0.001f)
                return 0f;
            
            // Calcul de l'angle horizontal seulement (pour le cône de vision latéral)
            Vector3 directionHorizontal = directionToPlayer;
            directionHorizontal.y = 0;
            Vector3 forwardHorizontal = forward;
            forwardHorizontal.y = 0;
            
            if (directionHorizontal.sqrMagnitude < 0.001f || forwardHorizontal.sqrMagnitude < 0.001f)
                return 0f; // Joueur directement au-dessus ou en-dessous = dans le champ
            
            return Vector3.Angle(forwardHorizontal.normalized, directionHorizontal.normalized);
        }

        protected void TurnTowardsPlayer()
        {
            Vector3 direction = (player.position - owner.position).normalized;
            direction.y = 0;

            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                float turnSpeed = settings.rotationSpeed * 2f;
                owner.rotation = Quaternion.Slerp(owner.rotation, lookRotation, Time.deltaTime * turnSpeed);
            }
        }

        protected void TurnTowardsPosition(Vector3 position)
        {
            Vector3 direction = (position - owner.position).normalized;
            direction.y = 0;

            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                owner.rotation = Quaternion.Slerp(owner.rotation, lookRotation, Time.deltaTime * settings.rotationSpeed);
            }
        }

        protected bool IsTurning => isTurningTowardsPlayer;

        protected abstract void ExecuteBehavior();

        // Implémentations de l'interface
        public abstract bool CanAttack();
        public abstract bool IsChasing();
        public abstract bool IsPatrolling();
        public abstract void OnDamageTaken();
        public abstract void DrawGizmos();

        /// <summary>
        /// Retourne l'état de détection actuel.
        /// </summary>
        public DetectionState GetDetectionState() => detectionState;

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

        protected bool CheckLineOfSight()
        {
            Vector3 eyePosition = owner.position + Vector3.up * settings.eyeHeight;
            Vector3 targetPosition = player.position + Vector3.up * 1f;
            Vector3 direction = targetPosition - eyePosition;
            float distance = direction.magnitude;

            // Utiliser RaycastAll pour filtrer les obstacles
            RaycastHit[] hits = Physics.RaycastAll(eyePosition, direction.normalized, distance, settings.obstacleLayer);
            
            // Trier par distance
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            
            foreach (var hit in hits)
            {
                // Ignorer l'ennemi lui-même
                if (hit.transform == owner || hit.transform.IsChildOf(owner))
                    continue;
                
                // Ignorer les obstacles très proches de l'ennemi (bord de plateforme)
                if (hit.distance < 0.5f)
                    continue;
                
                // C'est le joueur = ligne de vue OK
                if (hit.transform == player || hit.transform.IsChildOf(player))
                    return true;
                
                // Obstacle entre l'ennemi et le joueur
                return false;
            }
            
            // Rien touché = ligne de vue claire
            return true;
        }

        /// <summary>
        /// Vérifie la ligne de vue vers une position spécifique.
        /// </summary>
        protected bool CheckLineOfSightToPosition(Vector3 targetPosition)
        {
            Vector3 eyePosition = owner.position + Vector3.up * settings.eyeHeight;
            Vector3 direction = targetPosition - eyePosition;
            float distance = direction.magnitude;

            return !Physics.Raycast(eyePosition, direction.normalized, distance, settings.obstacleLayer);
        }

        /// <summary>
        /// Retourne la position de l'ennemi (pour le système d'alerte).
        /// </summary>
        public Vector3 GetOwnerPosition()
        {
            return owner != null ? owner.position : Vector3.zero;
        }
    }
}
