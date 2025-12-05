using UnityEngine;
using UnityEngine.AI;
using Ennemies.Settings;

namespace Ennemies.Behaviors
{
    /// <summary>
    /// Interface définissant le contrat pour tous les comportements d'ennemis.
    /// </summary>
    public interface IEnemyBehavior
    {
        /// <summary>
        /// Initialise le comportement avec les dépendances nécessaires.
        /// </summary>
        /// <param name="agent">Le NavMeshAgent de l'ennemi</param>
        /// <param name="player">Le Transform du joueur</param>
        /// <param name="settings">Les paramètres de comportement</param>
        /// <param name="owner">Le Transform de l'ennemi</param>
        void Initialize(NavMeshAgent agent, Transform player, EnemyBehaviorSettings settings, Transform owner);

        /// <summary>
        /// Exécute la logique de comportement (appelé dans Update).
        /// </summary>
        void Execute();

        /// <summary>
        /// Retourne true si l'ennemi peut attaquer le joueur.
        /// </summary>
        bool CanAttack();

        /// <summary>
        /// Retourne true si l'ennemi est actuellement en mode poursuite.
        /// </summary>
        bool IsChasing();

        /// <summary>
        /// Retourne true si l'ennemi est actuellement en mode patrouille.
        /// </summary>
        bool IsPatrolling();

        /// <summary>
        /// Appelé quand l'ennemi prend des dégâts.
        /// </summary>
        void OnDamageTaken();

        /// <summary>
        /// Dessine les gizmos de debug dans l'éditeur.
        /// </summary>
        void DrawGizmos();
    }
}

