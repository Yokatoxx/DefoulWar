using UnityEngine;

namespace HordeSystem
{
    /// <summary>
    /// État où l'ennemi se déplace vers le point de ralliement de sa horde.
    /// </summary>
    public class JoiningHordeState : BaseEnemyState
    {
        private const float ArrivalDistance = 3f;
        private float updatePathTimer;
        private const float PathUpdateInterval = 0.5f;
        
        public JoiningHordeState(NormalEnemyAI enemy) : base(enemy) { }
        
        public override void OnEnter()
        {
            updatePathTimer = 0f;
            Debug.Log($"[{enemy.name}] État: JoiningHorde - Rejoindre la horde {enemy.CurrentHorde?.HordeId}");
            
            // Définir la destination vers le point de ralliement
            UpdateDestination();
        }
        
        public override void OnUpdate()
        {
            // Vérifier si la horde existe encore
            if (enemy.CurrentHorde == null || enemy.IsAlone)
            {
                Debug.Log($"[{enemy.name}] Horde perdue, retour à SearchingHorde");
                enemy.ChangeState(new SearchingHordeState(enemy));
                return;
            }
            
            // Mettre à jour le chemin périodiquement
            updatePathTimer += Time.deltaTime;
            if (updatePathTimer >= PathUpdateInterval)
            {
                updatePathTimer = 0f;
                UpdateDestination();
            }
            
            // Vérifier si on est arrivé au point de ralliement
            if (enemy.Agent != null)
            {
                float distanceToRally = Vector3.Distance(enemy.transform.position, enemy.CurrentHorde.RallyPoint);
                
                if (distanceToRally <= ArrivalDistance)
                {
                    Debug.Log($"[{enemy.name}] Arrivé à la horde {enemy.CurrentHorde.HordeId}");
                    enemy.ChangeState(new InHordeState(enemy));
                }
            }
        }
        
        public override void OnExit()
        {
            // Rien de spécial
        }
        
        private void UpdateDestination()
        {
            if (enemy.Agent != null && enemy.CurrentHorde != null)
            {
                enemy.Agent.SetDestination(enemy.CurrentHorde.RallyPoint);
            }
        }
    }
}

