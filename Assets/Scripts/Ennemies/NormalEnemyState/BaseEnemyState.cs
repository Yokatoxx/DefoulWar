using UnityEngine;

namespace HordeSystem
{
    /// <summary>
    /// Classe de base pour tous les états de l'ennemi dans le système de horde.
    /// </summary>
    public abstract class BaseEnemyState
    {
        protected NormalEnemyAI enemy;
        
        public BaseEnemyState(NormalEnemyAI enemy)
        {
            this.enemy = enemy;
        }
        
        /// <summary>
        /// Appelé quand on entre dans cet état.
        /// </summary>
        public virtual void OnEnter() { }
        
        /// <summary>
        /// Appelé chaque frame pendant que l'état est actif.
        /// </summary>
        public virtual void OnUpdate() { }
        
        /// <summary>
        /// Appelé quand on sort de cet état.
        /// </summary>
        public virtual void OnExit() { }
        
        /// <summary>
        /// Obtient le nom de l'état pour le debug.
        /// </summary>
        public virtual string GetStateName()
        {
            return GetType().Name;
        }
    }
}

