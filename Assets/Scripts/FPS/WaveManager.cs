using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace Proto3GD.FPS
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
            [Tooltip("Durée maximale de la vague en secondes. Si le timer expire, la vague suivante démarre.")]
            [Min(1f)] public float duration = 30f;
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
        [Tooltip("Temps de repos après qu'une vague soit terminée avant le début de la suivante (si tous les ennemis sont morts avant le timer).")]
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
        [SerializeField] private float waveTimeRemaining;
        [SerializeField] private float currentWaveDuration;

        private readonly List<GameObject> activeEnemies = new List<GameObject>();
        private bool isWaveActive;
        private bool isSpawning;

        // Armure persistante très simple (compatible FPSDebugTools)
        private readonly Dictionary<string, int> zoneArmorLevels = new Dictionary<string, int>();
        // Statistiques de hits pour la vague courante
        private readonly Dictionary<string, int> currentWaveHits = new Dictionary<string, int>();

        // Intégration optionnelle avec le spawner de piliers
        private PillarSpawner pillarSpawner;

        private void Start()
        {
            pillarSpawner = FindFirstObjectByType<PillarSpawner>();
            if (autoStart)
            {
                StartNextWave();
            }
        }

        private void Update()
        {
            if (isWaveActive)
            {
                // Décompte du timer de la vague
                waveTimeRemaining -= Time.deltaTime;
                
                // Si le timer expire, passer à la vague suivante immédiatement
                if (waveTimeRemaining <= 0f)
                {
                    Debug.Log($"[WaveManager] Timer de la vague {CurrentWave} expiré. Passage à la vague suivante.");
                    ForceEndCurrentWaveAndStartNext();
                }
                // Si tous les ennemis sont morts avant la fin du timer
                else if (!isSpawning && enemiesRemaining <= 0)
                {
                    float restTime = waveTimeRemaining;
                    Debug.Log($"[WaveManager] Tous les ennemis éliminés avec {restTime:F1}s restantes. Temps de repos.");
                    EndWave();
                }
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
            currentWaveHits.Clear();

            // Initialiser le timer de la vague
            currentWaveDuration = wave.duration;
            waveTimeRemaining = currentWaveDuration;

            isWaveActive = true;
            onWaveStart?.Invoke(CurrentWave);
            onEnemyCountChanged?.Invoke(enemiesRemaining);

            StopAllCoroutines();
            StartCoroutine(SpawnWaveCoroutine(wave));

            Debug.Log($"[WaveManager] Vague {CurrentWave} démarrée. Ennemis: {totalEnemiesInWave}, Durée: {currentWaveDuration}s");
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

            // Enregistrer auprès du PillarSpawner pour les ennemis dynamiques
            EnemyHealth enemyHealth = enemyRoot.GetComponentInChildren<EnemyHealth>();
            if (enemyHealth != null)
            {
                // Appliquer les armures persistantes si défini
                if (zoneArmorLevels.Count > 0)
                {
                    enemyHealth.ApplyArmorLevels(zoneArmorLevels);
                }

                if (pillarSpawner != null)
                {
                    pillarSpawner.RegisterEnemy(enemyHealth);
                }
            }
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

        private void ForceEndCurrentWaveAndStartNext()
        {
            // Termine immédiatement la vague en cours et démarre la suivante sans délai
            StopAllCoroutines();
            isSpawning = false;
            isWaveActive = false;
            
            onWaveComplete?.Invoke(CurrentWave);
            Debug.Log($"[WaveManager] Vague {CurrentWave} terminée par expiration du timer.");
            
            // Démarrer la vague suivante immédiatement (pas de temps de repos)
            StartNextWave();
        }

        // --------------------
        // API minimale de compatibilité
        // --------------------
        public void RecordHit(string zoneName)
        {
            // Incrémente les statistiques de hits pour la vague en cours
            string key = NormalizeZoneKey(zoneName);
            if (string.IsNullOrEmpty(key)) key = "body";
            if (!currentWaveHits.ContainsKey(key)) currentWaveHits[key] = 0;
            currentWaveHits[key]++;
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
        // Armure persistante (simple, pilotée par DebugTools)
        // --------------------
        public IReadOnlyDictionary<string, int> ArmorLevels => zoneArmorLevels;
        public IReadOnlyDictionary<string, int> CurrentWaveHits => currentWaveHits;

        public void SetArmorLevel(string zoneName, int level)
        {
            string key = NormalizeZoneKey(zoneName);
            if (string.IsNullOrEmpty(key)) return;
            level = Mathf.Clamp(level, 0, 3);
            if (level <= 0)
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
            Debug.Log($"[WaveManager] Reapplied armor to {enemies.Length} enemies: {info}");
#endif
        }

        // --------------------
        // Helpers & propriétés
        // --------------------
        public int CurrentWave => currentWaveIndex + 1; // 1-based pour l'UI
        public int EnemiesRemaining => enemiesRemaining;
        public int TotalEnemiesInWave => totalEnemiesInWave;
        public bool IsWaveActive => isWaveActive;
        public float WaveTimeRemaining => waveTimeRemaining;
        public float CurrentWaveDuration => currentWaveDuration;

        private static string NormalizeZoneKey(string zone)
        {
            return string.IsNullOrWhiteSpace(zone) ? string.Empty : zone.Trim().ToLowerInvariant();
        }
    }
}
