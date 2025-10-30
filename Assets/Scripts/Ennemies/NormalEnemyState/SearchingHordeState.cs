using UnityEngine;

namespace HordeSystem
{
    public class SearchingHordeState : BaseEnemyState
    {
        private float searchTimer;
        private const float SearchInterval = 3f;
        
        public SearchingHordeState(NormalEnemyAI enemy) : base(enemy) { }
        
        public override void OnEnter()
        {
            searchTimer = 0f;
            Debug.Log($"[{enemy.name}] État: SearchingHorde - Recherche d'une horde");
            
            // Réessayer de s'enregistrer pour trouver une horde
            if (HordeManager.Instance != null)
            {
                HordeManager.Instance.RegisterEnemy(enemy);
            }
        }
        
        public override void OnUpdate()
        {
            searchTimer += Time.deltaTime;
            
            // Vérifier périodiquement si une horde est disponible
            if (searchTimer >= SearchInterval)
            {
                searchTimer = 0f;
                
                // Si assigné à une horde maintenant
                if (enemy.CurrentHorde != null && !enemy.IsAlone)
                {
                    enemy.ChangeState(new JoiningHordeState(enemy));
                }
            }
            
            // En attendant, patrouiller lentement ou rester sur place
            if (enemy.Agent != null && !enemy.Agent.hasPath)
            {
                // Mouvement aléatoire dans un petit rayon
                Vector3 randomPoint = enemy.transform.position + Random.insideUnitSphere * 5f;
                randomPoint.y = enemy.transform.position.y;
                enemy.Agent.SetDestination(randomPoint);
            }
        }
        
        public override void OnExit()
        {
            // Rien de spécial à faire
        }
    }
}

