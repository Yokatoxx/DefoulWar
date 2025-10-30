using UnityEngine;

namespace HordeSystem
{
    /// <summary>
    /// État où l'ennemi fait partie d'une horde active.
    /// L'ennemi suit le COMPORTEMENT COLLECTIF de la horde, pas ses propres décisions.
    /// </summary>
    public class InHordeState : BaseEnemyState
    {
        private Transform player;
        private const float AttackRange = 2f;
        private const float RepositionInterval = 1f;
        
        private float repositionTimer;
        private Vector3 assignedPosition;
        private bool hasAlertedHorde;
        
        public InHordeState(NormalEnemyAI enemy) : base(enemy) { }
        
        public override void OnEnter()
        {
            repositionTimer = 0f;
            hasAlertedHorde = false;
            
            // Trouver le joueur
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
            
            // Vitesse normale au début
            if (enemy.Agent != null)
            {
                enemy.Agent.speed = enemy.MoveSpeed;
            }
            
            Debug.Log($"[{enemy.name}] État: InHorde - Suit le comportement collectif de la horde {enemy.CurrentHorde?.HordeId}");
        }
        
        public override void OnUpdate()
        {
            // Vérifier si la horde existe encore
            if (enemy.CurrentHorde == null || enemy.IsAlone)
            {
                Debug.Log($"[{enemy.name}] Horde dissoute, retour à SearchingHorde");
                enemy.ChangeState(new SearchingHordeState(enemy));
                return;
            }
            
            // SUIVRE LE COMPORTEMENT COLLECTIF DE LA HORDE
            ExecuteHordeBehavior();
            
            // Mise à jour périodique de la position
            repositionTimer += Time.deltaTime;
            if (repositionTimer >= RepositionInterval)
            {
                repositionTimer = 0f;
                UpdateFormationPosition();
            }
            
            // Détection locale du joueur pour alerte
            if (player != null && !hasAlertedHorde)
            {
                float distanceToPlayer = Vector3.Distance(enemy.transform.position, player.position);
                
                if (distanceToPlayer <= enemy.DetectionRange)
                {
                    // ALERTE TOUTE LA HORDE !
                    enemy.AlertHorde(player);
                    hasAlertedHorde = true;
                }
            }
        }
        
        public override void OnExit()
        {
            // Réinitialiser la vitesse
            if (enemy.Agent != null)
            {
                enemy.Agent.speed = enemy.MoveSpeed;
            }
        }
        
        /// <summary>
        /// Exécute le comportement collectif assigné par la horde.
        /// </summary>
        private void ExecuteHordeBehavior()
        {
            if (enemy.CurrentHorde == null) return;
            
            switch (enemy.CurrentHorde.CurrentBehavior)
            {
                case HordeBehavior.Patrol:
                    BehaviorPatrol();
                    break;
                    
                case HordeBehavior.Chase:
                    BehaviorChase();
                    break;
                    
                case HordeBehavior.Attack:
                    BehaviorAttack();
                    break;
                    
                case HordeBehavior.Surround:
                    BehaviorSurround();
                    break;
                    
                case HordeBehavior.Retreat:
                    BehaviorRetreat();
                    break;
            }
        }
        
        /// <summary>
        /// Comportement : Patrouille dispersée autour du point de ralliement.
        /// </summary>
        private void BehaviorPatrol()
        {
            // Vitesse normale
            if (enemy.Agent != null && Mathf.Abs(enemy.Agent.speed - enemy.MoveSpeed) > 0.1f)
            {
                enemy.Agent.speed = enemy.MoveSpeed;
            }
            
            // Se déplacer vers la position de formation
            MoveToAssignedPosition();
        }
        
        /// <summary>
        /// Comportement : Poursuite coordonnée du joueur.
        /// </summary>
        private void BehaviorChase()
        {
            // Vitesse de poursuite
            if (enemy.Agent != null && Mathf.Abs(enemy.Agent.speed - enemy.ChaseSpeed) > 0.1f)
            {
                enemy.Agent.speed = enemy.ChaseSpeed;
            }
            
            // Tous se dirigent vers la cible collective en formation
            MoveToAssignedPosition();
        }
        
        /// <summary>
        /// Comportement : Attaque groupée synchronisée.
        /// </summary>
        private void BehaviorAttack()
        {
            // Vitesse de poursuite
            if (enemy.Agent != null)
            {
                enemy.Agent.speed = enemy.ChaseSpeed;
            }
            
            // Si proche du joueur, attaquer
            if (player != null)
            {
                float distanceToPlayer = Vector3.Distance(enemy.transform.position, player.position);
                
                if (distanceToPlayer <= AttackRange)
                {
                    // Arrêter et attaquer
                    if (enemy.Agent != null)
                    {
                        enemy.Agent.isStopped = true;
                    }
                    
                    LookAtTarget(player.position);
                    enemy.TryAttack();
                }
                else
                {
                    // Se rapprocher en formation
                    if (enemy.Agent != null)
                    {
                        enemy.Agent.isStopped = false;
                    }
                    MoveToAssignedPosition();
                }
            }
        }
        
        /// <summary>
        /// Comportement : Encerclement coordonné de la cible.
        /// </summary>
        private void BehaviorSurround()
        {
            // Vitesse de poursuite
            if (enemy.Agent != null)
            {
                enemy.Agent.speed = enemy.ChaseSpeed;
            }
            
            // Chaque ennemi prend sa position dans le cercle
            MoveToAssignedPosition();
            
            // Si en position et proche, attaquer
            if (player != null)
            {
                float distanceToAssigned = Vector3.Distance(enemy.transform.position, assignedPosition);
                float distanceToPlayer = Vector3.Distance(enemy.transform.position, player.position);
                
                if (distanceToAssigned < 2f && distanceToPlayer <= AttackRange * 1.5f)
                {
                    LookAtTarget(player.position);
                    enemy.TryAttack();
                }
            }
        }
        
        /// <summary>
        /// Comportement : Retraite coordonnée.
        /// </summary>
        private void BehaviorRetreat()
        {
            // Vitesse de poursuite pour retraite rapide
            if (enemy.Agent != null)
            {
                enemy.Agent.speed = enemy.ChaseSpeed;
            }
            
            // Reculer vers le point de ralliement
            MoveToAssignedPosition();
        }
        
        /// <summary>
        /// Met à jour la position assignée en formation.
        /// </summary>
        private void UpdateFormationPosition()
        {
            if (enemy.CurrentHorde != null)
            {
                assignedPosition = enemy.CurrentHorde.GetFormationPosition(enemy);
            }
        }
        
        /// <summary>
        /// Déplace l'ennemi vers sa position assignée en formation.
        /// </summary>
        private void MoveToAssignedPosition()
        {
            if (enemy.Agent != null && assignedPosition != Vector3.zero)
            {
                enemy.Agent.isStopped = false;
                enemy.Agent.SetDestination(assignedPosition);
            }
        }
        
        /// <summary>
        /// Oriente l'ennemi vers une cible.
        /// </summary>
        private void LookAtTarget(Vector3 targetPosition)
        {
            Vector3 direction = (targetPosition - enemy.transform.position).normalized;
            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
                enemy.transform.rotation = Quaternion.Slerp(enemy.transform.rotation, lookRotation, Time.deltaTime * 5f);
            }
        }
    }
}

