using UnityEngine;

namespace HordeSystem
{
    // 'ennemi fait partie d'une horde active.

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
            
            if (enemy.Agent != null)
            {
                enemy.Agent.speed = enemy.MoveSpeed;
            }
        }
        
        public override void OnUpdate()
        {
            if (enemy.CurrentHorde == null || enemy.IsAlone)
            {
                Debug.Log($"[{enemy.name}] Horde dissoute, retour à SearchingHorde");
                enemy.ChangeState(new SearchingHordeState(enemy));
                return;
            }
            
            ExecuteHordeBehavior();
            
            repositionTimer += Time.deltaTime;
            if (repositionTimer >= RepositionInterval)
            {
                repositionTimer = 0f;
                UpdateFormationPosition();
            }
            
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
            if (enemy.Agent != null)
            {
                enemy.Agent.speed = enemy.MoveSpeed;
            }
        }
        
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
        
        private void BehaviorPatrol()
        {
            if (enemy.Agent != null && Mathf.Abs(enemy.Agent.speed - enemy.MoveSpeed) > 0.1f)
            {
                enemy.Agent.speed = enemy.MoveSpeed;
            }
            
            MoveToAssignedPosition();
        }
        
        private void BehaviorChase()
        {
            if (enemy.Agent != null && Mathf.Abs(enemy.Agent.speed - enemy.ChaseSpeed) > 0.1f)
            {
                enemy.Agent.speed = enemy.ChaseSpeed;
            }
            
            MoveToAssignedPosition();
        }
        
        private void BehaviorAttack()
        {
            if (enemy.Agent != null)
            {
                enemy.Agent.speed = enemy.ChaseSpeed;
            }
            
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
        
        // encerclement coordonné de la cible.
        private void BehaviorSurround()
        {
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

        private void BehaviorRetreat()
        {
            if (enemy.Agent != null)
            {
                enemy.Agent.speed = enemy.ChaseSpeed;
            }
            
            MoveToAssignedPosition();
        }
        
        private void UpdateFormationPosition()
        {
            if (enemy.CurrentHorde != null)
            {
                assignedPosition = enemy.CurrentHorde.GetFormationPosition(enemy);
            }
        }
        
        private void MoveToAssignedPosition()
        {
            if (enemy.Agent != null && assignedPosition != Vector3.zero)
            {
                enemy.Agent.isStopped = false;
                enemy.Agent.SetDestination(assignedPosition);
            }
        }
        
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

