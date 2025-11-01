using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace FPS
{
    // Système de vagues simple: chaque vague définit une liste de types d'ennemis et leur quantité.
    public class WaveManager : MonoBehaviour
    {
        // --------------------
        // Types de données
        // --------------------
        [System.Serializable]
        public class EnemySpawn
        {
            public GameObject prefab;
            [Min(0)] public int count = 1;
        }

        [System.Serializable]
        public class Wave
        {
            public List<EnemySpawn> enemies = new List<EnemySpawn>();
        }

        // --------------------
        // Singleton basique
        // --------------------
        private static WaveManager instance;
        public static WaveManager Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = FindFirstObjectByType<WaveManager>();
                }
                return instance;
            }
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Debug.LogWarning("Multiple WaveManager instances found. Destroying this one.");
                Destroy(gameObject);
                return;
            }
            instance = this;
        }

        // --------------------
        // Réglages
        // --------------------
        [Header("Waves")]
        [Tooltip("Liste des vagues. Chaque vague contient des types d'ennemis et une quantité.")]
        [SerializeField] private List<Wave> waves = new List<Wave>();
        [SerializeField] private bool autoStart = true;
        [SerializeField] private bool loopWaves = false;

        [Header("Spawning")]
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private float spawnDelay = 0.25f;
        [SerializeField] private float timeBetweenWaves = 5f;

        [Header("Events")]
        public UnityEvent<int> onWaveStart = new UnityEvent<int>();
        public UnityEvent<int> onWaveComplete = new UnityEvent<int>();
        public UnityEvent<int> onEnemyCountChanged = new UnityEvent<int>();

        // --------------------
        // État courant
        // --------------------
        [SerializeField] private int currentWaveIndex = -1; // 0-based interne
        [SerializeField] private int enemiesRemaining;
        [SerializeField] private int totalEnemiesInWave;

        private readonly List<GameObject> activeEnemies = new List<GameObject>();
        private bool isWaveActive;
        private bool isSpawning;
        

        private void Start()
        {
            if (autoStart)
            {
                StartNextWave();
            }
        }

        private void Update()
        {
            if (isWaveActive && !isSpawning && enemiesRemaining <= 0)
            {
                EndWave();
            }
        }

        // --------------------
        // Logique de vagues
        // --------------------
        public void StartNextWave()
        {
            if (waves == null || waves.Count == 0)
            {
                Debug.LogWarning("[WaveManager] Aucune vague configurée.");
                return;
            }

            int nextIndex = currentWaveIndex + 1;
            if (!loopWaves && nextIndex >= waves.Count)
            {
                Debug.Log("[WaveManager] Toutes les vagues sont terminées.");
                isWaveActive = false;
                return;
            }

            if (loopWaves)
            {
                nextIndex = Mathf.Abs(nextIndex) % waves.Count;
            }

            currentWaveIndex = nextIndex;

            // Préparer la vague
            Wave wave = waves[currentWaveIndex];
            totalEnemiesInWave = wave.enemies.Where(es => es != null && es.prefab != null && es.count > 0).Sum(es => es.count);
            enemiesRemaining = totalEnemiesInWave;
            activeEnemies.Clear();

            isWaveActive = true;
            onWaveStart?.Invoke(CurrentWave);
            onEnemyCountChanged?.Invoke(enemiesRemaining);

            StopAllCoroutines();
            StartCoroutine(SpawnWaveCoroutine(wave));

            Debug.Log($"[WaveManager] Vague {CurrentWave} démarrée. Ennemis: {totalEnemiesInWave}");
        }

        private IEnumerator SpawnWaveCoroutine(Wave wave)
        {
            isSpawning = true;

            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogError("[WaveManager] Aucun point de spawn défini.");
                isSpawning = false;
                yield break;
            }

            foreach (var pack in wave.enemies)
            {
                if (pack == null || pack.prefab == null || pack.count <= 0) continue;
                for (int i = 0; i < pack.count; i++)
                {
                    SpawnEnemy(pack.prefab);
                    if (spawnDelay > 0f)
                        yield return new WaitForSeconds(spawnDelay);
                    else
                        yield return null; // laisser un frame
                }
            }

            isSpawning = false;
        }

        private void SpawnEnemy(GameObject prefab)
        {
            if (prefab == null) return;
            if (spawnPoints == null || spawnPoints.Length == 0) return;

            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            GameObject enemyRoot = Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
            activeEnemies.Add(enemyRoot);
        }

        public void OnEnemyDeath(EnemyHealth enemy)
        {
            enemiesRemaining = Mathf.Max(0, enemiesRemaining - 1);
            onEnemyCountChanged?.Invoke(enemiesRemaining);

            if (enemy != null)
            {
                GameObject root = enemy.transform.root != null ? enemy.transform.root.gameObject : enemy.gameObject;
                activeEnemies.Remove(root);
            }
        }

        private void EndWave()
        {
            isWaveActive = false;
            onWaveComplete?.Invoke(CurrentWave);
            Debug.Log($"[WaveManager] Vague {CurrentWave} terminée.");

            // Programmer la suivante si possible
            if ((loopWaves && waves.Count > 0) || currentWaveIndex + 1 < (waves?.Count ?? 0))
            {
                Invoke(nameof(StartNextWave), Mathf.Max(0f, timeBetweenWaves));
            }
        }

        public void ForceNextWaveNow()
        {
            // Arrête les spawns en cours et passe directement à la prochaine vague
            StopAllCoroutines();
            isSpawning = false;
            isWaveActive = false;
            enemiesRemaining = 0;
            onEnemyCountChanged?.Invoke(enemiesRemaining);
            StartNextWave();
        }

        // --------------------
        // Helpers & propriétés
        // --------------------
        public int CurrentWave => currentWaveIndex + 1; // 1-based pour l'UI
        public int EnemiesRemaining => enemiesRemaining;
        public int TotalEnemiesInWave => totalEnemiesInWave;
        public bool IsWaveActive => isWaveActive;
    }
}
