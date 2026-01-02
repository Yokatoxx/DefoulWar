using System.Collections.Generic;
using UnityEngine;

namespace FPS
{
    /// <summary>
    /// Registre centralisé de tous les ennemis actifs.
    /// Évite les appels coûteux à FindObjectsByType.
    /// </summary>
    public class EnemyRegistry : MonoBehaviour
    {
        private static EnemyRegistry instance;
        public static EnemyRegistry Instance
        {
            get
            {
                if (instance == null)
                {
                    // Créer automatiquement si n'existe pas
                    var go = new GameObject("EnemyRegistry");
                    instance = go.AddComponent<EnemyRegistry>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }
        
        private readonly HashSet<EnemyHealth> enemies = new HashSet<EnemyHealth>();
        
        /// <summary>
        /// Liste en lecture seule de tous les ennemis vivants.
        /// </summary>
        public IReadOnlyCollection<EnemyHealth> Enemies => enemies;
        
        /// <summary>
        /// Nombre d'ennemis vivants.
        /// </summary>
        public int Count => enemies.Count;
        
        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
        }
        
        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
        
        /// <summary>
        /// Enregistre un ennemi (appelé dans EnemyHealth.Awake).
        /// </summary>
        public void Register(EnemyHealth enemy)
        {
            if (enemy != null)
            {
                enemies.Add(enemy);
            }
        }
        
        /// <summary>
        /// Désenregistre un ennemi (appelé dans EnemyHealth.OnDestroy).
        /// </summary>
        public void Unregister(EnemyHealth enemy)
        {
            enemies.Remove(enemy);
        }
        
        /// <summary>
        /// Retourne tous les ennemis vivants (non morts).
        /// </summary>
        public IEnumerable<EnemyHealth> GetAliveEnemies()
        {
            foreach (var enemy in enemies)
            {
                if (enemy != null && !enemy.IsDead)
                {
                    yield return enemy;
                }
            }
        }
        
        /// <summary>
        /// Nettoie les références nulles (appelé périodiquement ou manuellement).
        /// </summary>
        public void Cleanup()
        {
            enemies.RemoveWhere(e => e == null);
        }
    }
}
