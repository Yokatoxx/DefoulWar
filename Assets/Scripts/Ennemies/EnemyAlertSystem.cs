using UnityEngine;
using System.Collections.Generic;
using Ennemies.Behaviors;

namespace Ennemies
{
    /// <summary>
    /// Système singleton pour coordonner les alertes entre ennemis.
    /// Quand un ennemi détecte le joueur, il peut alerter les autres dans un rayon.
    /// </summary>
    public class EnemyAlertSystem : MonoBehaviour
    {
        private static EnemyAlertSystem instance;
        public static EnemyAlertSystem Instance
        {
            get
            {
                if (instance == null)
                {
                    // Créer automatiquement si n'existe pas
                    var go = new GameObject("EnemyAlertSystem");
                    instance = go.AddComponent<EnemyAlertSystem>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        private List<BaseEnemyBehavior> registeredEnemies = new List<BaseEnemyBehavior>();

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
        }

        /// <summary>
        /// Enregistre un ennemi dans le système d'alerte.
        /// </summary>
        public void RegisterEnemy(BaseEnemyBehavior enemy)
        {
            if (!registeredEnemies.Contains(enemy))
            {
                registeredEnemies.Add(enemy);
            }
        }

        /// <summary>
        /// Désenregistre un ennemi du système.
        /// </summary>
        public void UnregisterEnemy(BaseEnemyBehavior enemy)
        {
            registeredEnemies.Remove(enemy);
        }

        /// <summary>
        /// Alerte tous les ennemis dans un rayon de la position du joueur.
        /// </summary>
        /// <param name="alertOrigin">Position de l'ennemi qui alerte</param>
        /// <param name="playerPosition">Dernière position connue du joueur</param>
        /// <param name="alertRadius">Rayon d'alerte</param>
        public void AlertEnemiesInRadius(Vector3 alertOrigin, Vector3 playerPosition, float alertRadius)
        {
            // Nettoyer les références nulles
            registeredEnemies.RemoveAll(e => e == null);

            float radiusSqr = alertRadius * alertRadius;

            foreach (var enemy in registeredEnemies)
            {
                if (enemy == null) continue;

                Vector3 enemyPosition = enemy.GetOwnerPosition();
                float distanceSqr = (enemyPosition - alertOrigin).sqrMagnitude;
                if (distanceSqr <= radiusSqr)
                {
                    enemy.ReceiveAlert(playerPosition);
                }
            }
        }

        /// <summary>
        /// Alerte tous les ennemis enregistrés (utile pour les arènes fermées).
        /// </summary>
        public void AlertAllEnemies(Vector3 playerPosition)
        {
            registeredEnemies.RemoveAll(e => e == null);

            foreach (var enemy in registeredEnemies)
            {
                if (enemy != null)
                {
                    enemy.ReceiveAlert(playerPosition);
                }
            }
        }
    }
}
