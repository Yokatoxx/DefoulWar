using System.Collections.Generic;
using UnityEngine;

namespace HordeSystem
{
    /// <summary>
    /// Spawner d'ennemis avec support des vagues et intégration au système de horde.
    /// </summary>
    public class HordeEnemySpawner : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [Tooltip("Prefab de l'ennemi à spawner")]
        [SerializeField] private GameObject enemyPrefab;
        
        [Tooltip("Points de spawn possibles")]
        [SerializeField] private Transform[] spawnPoints;
        
        [Header("Wave Settings")]
        [Tooltip("Nombre d'ennemis par vague")]
        [SerializeField] private int enemiesPerWave = 5;
        
        [Tooltip("Délai entre chaque spawn (secondes)")]
        [SerializeField] private float spawnInterval = 2f;
        
        [Tooltip("Délai avant la prochaine vague (secondes)")]
        [SerializeField] private float waveCooldown = 10f;
        
        [Tooltip("Spawner automatiquement au démarrage")]
        [SerializeField] private bool autoStart = true;
        
        [Tooltip("Nombre de vagues (-1 = infini)")]
        [SerializeField] private int maxWaves = -1;
        
        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;
        
        // État interne
        private List<GameObject> spawnedEnemies = new List<GameObject>();
        private int currentWave;
        private int enemiesSpawnedInWave;
        private bool isSpawning;
        private float spawnTimer;
        private float waveTimer;
        private bool waitingForWave;
        
        private void Start()
        {
            // Valider les spawn points
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogWarning($"[HordeEnemySpawner] Aucun spawn point défini sur {gameObject.name}");
                spawnPoints = new[] { transform };
            }
            
            if (autoStart)
            {
                StartNewWave();
            }
        }
        
        private void Update()
        {
            if (!isSpawning) return;
            
            // Si on attend la prochaine vague
            if (waitingForWave)
            {
                waveTimer += Time.deltaTime;
                
                if (waveTimer >= waveCooldown)
                {
                    waveTimer = 0f;
                    waitingForWave = false;
                    StartNewWave();
                }
                return;
            }
            
            // Spawn des ennemis
            if (enemiesSpawnedInWave < enemiesPerWave)
            {
                spawnTimer += Time.deltaTime;
                
                if (spawnTimer >= spawnInterval)
                {
                    spawnTimer = 0f;
                    SpawnEnemy();
                    enemiesSpawnedInWave++;
                    
                    // Vérifier si la vague est terminée
                    if (enemiesSpawnedInWave >= enemiesPerWave)
                    {
                        OnWaveCompleted();
                    }
                }
            }
        }
        
        /// <summary>
        /// Démarre une nouvelle vague.
        /// </summary>
        [ContextMenu("Start New Wave")]
        public void StartNewWave()
        {
            // Vérifier si on a atteint le nombre max de vagues
            if (maxWaves >= 0 && currentWave >= maxWaves)
            {
                if (showDebugInfo)
                {
                    Debug.Log($"[HordeEnemySpawner] Toutes les vagues ont été complétées ({maxWaves})");
                }
                isSpawning = false;
                return;
            }
            
            currentWave++;
            enemiesSpawnedInWave = 0;
            isSpawning = true;
            waitingForWave = false;
            
            if (showDebugInfo)
            {
                Debug.Log($"[HordeEnemySpawner] Vague {currentWave} commence - {enemiesPerWave} ennemis");
            }
            
            // Notifier le HordeManager
            if (HordeManager.Instance != null)
            {
                HordeManager.Instance.OnNewWaveStarted();
            }
        }
        
        /// <summary>
        /// Spawn un ennemi à un point de spawn.
        /// </summary>
        private void SpawnEnemy()
        {
            if (enemyPrefab == null)
            {
                Debug.LogWarning($"[HordeEnemySpawner] Aucun prefab d'ennemi assigné");
                return;
            }
            
            // Choisir un spawn point
            Transform spawnPoint = GetNextSpawnPoint();
            
            // Instancier l'ennemi
            GameObject enemy = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
            spawnedEnemies.Add(enemy);
            
            if (showDebugInfo)
            {
                Debug.Log($"[HordeEnemySpawner] Ennemi spawné à {spawnPoint.name} ({enemiesSpawnedInWave + 1}/{enemiesPerWave})");
            }
        }
        
        /// <summary>
        /// Obtient le prochain spawn point (round-robin).
        /// </summary>
        private Transform GetNextSpawnPoint()
        {
            if (spawnPoints.Length == 1)
                return spawnPoints[0];
            
            // Alterner entre les spawn points
            int index = enemiesSpawnedInWave % spawnPoints.Length;
            return spawnPoints[index];
        }
        
        /// <summary>
        /// Appelé quand tous les ennemis de la vague ont été spawnés.
        /// </summary>
        private void OnWaveCompleted()
        {
            if (showDebugInfo)
            {
                Debug.Log($"[HordeEnemySpawner] Vague {currentWave} complétée");
            }
            
            // Nettoyer les ennemis morts
            spawnedEnemies.RemoveAll(e => e == null);
            
            // Préparer la prochaine vague
            if (maxWaves < 0 || currentWave < maxWaves)
            {
                waitingForWave = true;
                waveTimer = 0f;
            }
            else
            {
                isSpawning = false;
            }
        }
        
        /// <summary>
        /// Arrête le spawning.
        /// </summary>
        [ContextMenu("Stop Spawning")]
        public void StopSpawning()
        {
            isSpawning = false;
            waitingForWave = false;
            
            if (showDebugInfo)
            {
                Debug.Log($"[HordeEnemySpawner] Spawning arrêté");
            }
        }
        
        /// <summary>
        /// Détruit tous les ennemis spawnés.
        /// </summary>
        [ContextMenu("Kill All Enemies")]
        public void KillAllEnemies()
        {
            foreach (var enemy in spawnedEnemies)
            {
                if (enemy != null)
                {
                    Destroy(enemy);
                }
            }
            spawnedEnemies.Clear();
            
            if (showDebugInfo)
            {
                Debug.Log($"[HordeEnemySpawner] Tous les ennemis détruits");
            }
        }
        
        // Debug Gizmos
        private void OnDrawGizmos()
        {
            if (spawnPoints == null || spawnPoints.Length == 0) return;
            
            foreach (var point in spawnPoints)
            {
                if (point == null) continue;
                
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(point.position, 1f);
                Gizmos.DrawLine(point.position, point.position + point.forward * 2f);
            }
        }
    }
}

