using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Proto3GD.FPS
{

    // Gestionnaire de vagues d'ennemis: renforce plusieurs zones en parallèle.
    // Progression par vague
    public class WaveManager : MonoBehaviour
    {
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
        
        [Header("Wave Settings")]
        [SerializeField] private int startingEnemiesPerWave = 5;
        [SerializeField] private float enemiesIncreasePerWave = 2f;
        [SerializeField] private float timeBetweenWaves = 5f;
        
        [Header("Enemy Spawning")]
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private float spawnDelay = 0.5f;
        
        [Header("Armor Progression (par vague)")]
        [Tooltip("Nombre de hits sur une zone pendant UNE vague pour gagner +1 niveau d'armure")]
        [SerializeField] private int hitsPerWaveForLevelUp = 10;
        
        [Header("Current Wave Info")]
        [SerializeField] private int currentWave;
        [SerializeField] private int enemiesRemaining;
        [SerializeField] private int totalEnemiesInWave;
        
        [Header("Events")]
        public UnityEvent<int> onWaveStart = new UnityEvent<int>();
        public UnityEvent<int> onWaveComplete = new UnityEvent<int>();
        public UnityEvent<int> onEnemyCountChanged = new UnityEvent<int>();
        
        private Dictionary<string, int> currentWaveHits = new Dictionary<string, int>();
        
        // Niveaux d'armure par zone (0..3)
        private Dictionary<string, int> zoneArmorLevels = new Dictionary<string, int>();
        
        private List<GameObject> activeEnemies = new List<GameObject>();
        private bool isWaveActive;
        private bool isSpawning;
        
        // Référence au système de spawn de piliers
        private PillarSpawner pillarSpawner;
        
        private void Start()
        {
            // Trouver le PillarSpawner dans la scène
            pillarSpawner = FindFirstObjectByType<PillarSpawner>();
            
            StartNextWave();
        }
        
        private void Update()
        {
            if (isWaveActive && !isSpawning && enemiesRemaining <= 0)
            {
                EndWave();
            }
        }
        
        public void StartNextWave()
        {
            if (isWaveActive) return;
            
            currentWave++;
            totalEnemiesInWave = Mathf.RoundToInt(startingEnemiesPerWave + (currentWave - 1) * enemiesIncreasePerWave);
            enemiesRemaining = totalEnemiesInWave;
            
            // Appliquer les gains d'armure en fonction des hits de la vague précédente
            ApplyArmorProgressionFromPreviousWave();
            
            // Réappliquer les niveaux persistants aux ennemis déjà présents (debug)
            ReapplyArmorToActiveEnemies();
            
            // Réinitialiser les stats de la nouvelle vague
            currentWaveHits.Clear();
            activeEnemies.Clear();
            
            isWaveActive = true;
            onWaveStart?.Invoke(currentWave);
            onEnemyCountChanged?.Invoke(enemiesRemaining);
            
            Debug.Log($"Wave {currentWave} started! Enemies: {totalEnemiesInWave}");
            StartCoroutine(SpawnEnemiesCoroutine());
        }
        
        private System.Collections.IEnumerator SpawnEnemiesCoroutine()
        {
            isSpawning = true;
            for (int i = 0; i < totalEnemiesInWave; i++)
            {
                SpawnEnemy();
                yield return new WaitForSeconds(spawnDelay);
            }
            isSpawning = false;
        }
        
        private void SpawnEnemy()
        {
            if (enemyPrefab == null || spawnPoints.Length == 0)
            {
                Debug.LogError("Enemy prefab or spawn points not set!");
                return;
            }
            
            Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
            GameObject enemyRoot = Instantiate(enemyPrefab, spawnPoint.position, spawnPoint.rotation);
            activeEnemies.Add(enemyRoot);

            // synchroniseur d'armure pour ce nouvel ennemi
            if (enemyRoot.GetComponentInChildren<EnemyArmorSync>() == null)
            {
                enemyRoot.AddComponent<EnemyArmorSync>();
            }
            
            // Enregistrer l'ennemi auprès du PillarSpawner pour le système de spawn de piliers
            EnemyHealth enemyHealth = enemyRoot.GetComponentInChildren<EnemyHealth>();
            if (enemyHealth != null)
            {
                // Appliquer les armures
                if (zoneArmorLevels.Count > 0)
                {
                    enemyHealth.ApplyArmorLevels(zoneArmorLevels);
                    #if UNITY_EDITOR
                    string info = string.Join(", ", zoneArmorLevels.Select(p => $"{p.Key}=L{p.Value}"));
                    Debug.Log($"[WaveManager] Applied armor to spawned enemy: {info}");
                    #endif
                }
                
                // Enregistrer auprès du PillarSpawner si disponible
                if (pillarSpawner != null)
                {
                    pillarSpawner.RegisterEnemy(enemyHealth);
                }
            }
        }
        

        public void RecordHit(string zoneName)
        {
            string key = NormalizeZoneKey(zoneName);
            if (!currentWaveHits.ContainsKey(key))
            {
                currentWaveHits[key] = 0;
            }
            currentWaveHits[key]++;
        }
        

        public void OnEnemyDeath(EnemyHealth enemy)
        {
            enemiesRemaining--;
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
            onWaveComplete?.Invoke(currentWave);
            
            Debug.Log($"Wave {currentWave} complete!");
            foreach (var kvp in currentWaveHits.OrderByDescending(x => x.Value))
            {
                Debug.Log($"  {kvp.Key}: {kvp.Value} hits");
            }
            
            Invoke(nameof(StartNextWave), timeBetweenWaves);
        }
        
        private void ApplyArmorProgressionFromPreviousWave()
        {
            if (currentWaveHits == null || currentWaveHits.Count == 0) return;
            
            foreach (var kvp in currentWaveHits)
            {
                string zone = kvp.Key;
                int hits = kvp.Value;
                if (hits >= hitsPerWaveForLevelUp)
                {
                    int prev = zoneArmorLevels.ContainsKey(zone) ? zoneArmorLevels[zone] : 0;
                    int next = Mathf.Clamp(prev + 1, 1, 3);
                    zoneArmorLevels[zone] = next;
                }
            }
            
            if (zoneArmorLevels.Count > 0)
            {
                string info = string.Join(", ", zoneArmorLevels.Select(p => $"{p.Key}=L{p.Value}"));
                Debug.Log($"Armor levels (persistent) applied next wave: {info}");
            }
        }
        

        public int CurrentWave => currentWave;
        public int EnemiesRemaining => enemiesRemaining;
        public int TotalEnemiesInWave => totalEnemiesInWave;
        public bool IsWaveActive => isWaveActive;
        public IReadOnlyDictionary<string, int> ArmorLevels => zoneArmorLevels;
        public Dictionary<string, int> CurrentWaveHits => currentWaveHits;
        

        public void SetArmorLevel(string zoneName, int level)
        {
            string key = NormalizeZoneKey(zoneName);
            if (string.IsNullOrEmpty(key)) return;
            level = Mathf.Clamp(level, 0, 3);
            if (level == 0)
            {
                if (zoneArmorLevels.ContainsKey(key)) zoneArmorLevels.Remove(key);
            }
            else
            {
                zoneArmorLevels[key] = level;
            }
        }
        

        public void IncreaseArmorLevel(string zoneName, int delta)
        {
            string key = NormalizeZoneKey(zoneName);
            if (string.IsNullOrEmpty(key) || delta == 0) return;
            int prev = zoneArmorLevels.ContainsKey(key) ? zoneArmorLevels[key] : 0;
            int next = Mathf.Clamp(prev + delta, 0, 3);
            if (next <= 0) zoneArmorLevels.Remove(key);
            else zoneArmorLevels[key] = next;
        }
        

        public void IncreaseArmorLevels(Dictionary<string, int> deltas)
        {
            if (deltas == null) return;
            foreach (var kv in deltas)
            {
                IncreaseArmorLevel(kv.Key, kv.Value);
            }
        }
        

        public void ResetAllArmorLevels()
        {
            zoneArmorLevels.Clear();
        }
        
 
        public void ReapplyArmorToActiveEnemies()
        {
            var enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
            foreach (var e in enemies)
            {
                e.ApplyArmorLevels(zoneArmorLevels);
            }
            #if UNITY_EDITOR
            string info = zoneArmorLevels.Count > 0 ? string.Join(", ", zoneArmorLevels.Select(p => $"{p.Key}=L{p.Value}")) : "(none)";
            Debug.Log($"[WaveManager] Reapplied armor to {enemies.Length} active enemies: {info}");
            #endif
        }
        

        // Démarre immédiatement la prochaine vague(debug)

        public void ForceNextWaveNow()
        {
            if (!isWaveActive)
            {
                StartNextWave();
                return;
            }
            
            isWaveActive = false;
            StartNextWave();
        }
        
        private static string NormalizeZoneKey(string zone)
        {
            if (string.IsNullOrWhiteSpace(zone)) return string.Empty;
            string k = zone.Trim().ToLowerInvariant();
            switch (k)
            {
                case "chest":
                case "torso":
                case "abdomen":
                case "stomach":
                case "trunk":
                    return "body";
                case "skull":
                case "headshot":
                    return "head";
                default:
                    return k;
            }
        }
    }
}
