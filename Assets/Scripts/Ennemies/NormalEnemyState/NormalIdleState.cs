using UnityEngine;

namespace HordeSystem
{
    /// <summary>
    /// État initial - L'ennemi attend d'être assigné à une horde ou reste isolé.
    /// </summary>
    public class NormalIdleState : BaseEnemyState
    {
        private float idleTimer;
        private const float IdleDuration = 2f;
        
        public NormalIdleState(NormalEnemyAI enemy) : base(enemy) { }
        
        public override void OnEnter()
        {
            idleTimer = 0f;
            
            // Arrêter le mouvement
            if (enemy.Agent != null)
            {
                enemy.Agent.isStopped = true;
            }
            
            Debug.Log($"[{enemy.name}] État: Idle - En attente d'assignation");
        }
        
        public override void OnUpdate()
        {
            idleTimer += Time.deltaTime;
            
            // Après un court délai, vérifier l'état de horde
            if (idleTimer >= IdleDuration)
            {
                // Si assigné à une horde, passer en mode JoiningHorde
                if (enemy.CurrentHorde != null && !enemy.IsAlone)
                {
                    enemy.ChangeState(new JoiningHordeState(enemy));
                }
                // Si aucune horde disponible, chercher
                else if (enemy.IsAlone)
                {
                    enemy.ChangeState(new SearchingHordeState(enemy));
                }
            }
        }
        
        public override void OnExit()
        {
            if (enemy.Agent != null)
            {
                enemy.Agent.isStopped = false;
            }
        }
    }
}

