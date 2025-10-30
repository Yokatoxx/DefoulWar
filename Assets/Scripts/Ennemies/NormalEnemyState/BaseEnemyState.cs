using UnityEngine;

namespace HordeSystem
{
    public abstract class BaseEnemyState
    {
        protected NormalEnemyAI enemy;
        
        public BaseEnemyState(NormalEnemyAI enemy)
        {
            this.enemy = enemy;
        }
        

        public virtual void OnEnter() { }
        public virtual void OnUpdate() { }
        
        public virtual void OnExit() { }
        
        public virtual string GetStateName()
        {
            return GetType().Name;
        }
    }
}

