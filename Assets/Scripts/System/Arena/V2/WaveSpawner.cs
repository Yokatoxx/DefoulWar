using FPS;
using Ennemies;
using System.Collections;
using UnityEngine;

public class WaveSpawner : MonoBehaviour
{
    [Header("Configuration")]
    [SerializeField] public Wave[] waves;
    [SerializeField] public Transform[] spawnpoints;
    [SerializeField] public DoorArena[] doors;

    private int waveIndex = 0;
    private Wave currentWave;
    private int aliveCount = 0;
    private bool stopSpawning = false;
    private bool waitingForClear = false;
    private bool arenaTriggered = false;

    public void TriggerArena()
    {
        if (arenaTriggered) return;
        arenaTriggered = true;

        Debug.Log("[WaveSpawner] Arena triggered!");

        if (waves == null || waves.Length == 0)
        {
            Debug.LogWarning("[WaveSpawner] No waves configured!");
            return;
        }

        waveIndex = 0;
        currentWave = waves[waveIndex];

        // Ferme toutes les portes au début de l'arène
        CloseDoors();
        
        // Active le mode arène : tous les ennemis connaissent la position du joueur
        EnemyAlertSystem.Instance.SetAllEnemiesArenaMode(true);

        StartCoroutine(StartWaveAfterDelay(currentWave.TimeBeforeThisWave));
    }

    private IEnumerator StartWaveAfterDelay(float delay)
    {
        if (delay > 0)
            yield return new WaitForSeconds(delay);

        SpawnWave();
    }

    private void Update()
    {
        if (waitingForClear && aliveCount <= 0)
        {
            Debug.Log("[WaveSpawner] Wave cleared! Alive count: " + aliveCount);
            waitingForClear = false;
            OnWaveCleared();
        }
    }

    private void OnWaveCleared()
    {
        // Passe à la vague suivante (ou met stopSpawning = true si c'était la dernière)
        IncWave();

        // Vérifie si toutes les vagues sont terminées
        if (stopSpawning)
        {
            Debug.Log("[WaveSpawner] All waves complete! Opening doors...");
            OpenDoors();
            return;
        }

        Debug.Log("[WaveSpawner] Starting wave " + (waveIndex + 1));
        StartCoroutine(StartWaveAfterDelay(currentWave.TimeBeforeThisWave));
    }

    private void SpawnWave()
    {
        if (stopSpawning || waves == null || waves.Length == 0) return;

        if (currentWave.EnemySpawnList == null || currentWave.EnemySpawnList.Length == 0)
        {
            Debug.LogWarning("[WaveSpawner] No enemies configured in wave!");
            waitingForClear = true;
            return;
        }

        if (spawnpoints == null || spawnpoints.Length == 0)
        {
            Debug.LogWarning("[WaveSpawner] No spawnpoints configured!");
            waitingForClear = true;
            return;
        }

        int totalSpawned = 0;
        int spawnIndex = 0;

        // Spawn chaque type d'ennemi avec son nombre respectif
        foreach (var spawnInfo in currentWave.EnemySpawnList)
        {
            if (spawnInfo == null || spawnInfo.EnemyPrefab == null || spawnInfo.Count <= 0)
                continue;

            for (int i = 0; i < spawnInfo.Count; i++)
            {
                Transform spawnPoint = spawnpoints[spawnIndex % spawnpoints.Length];
                
                // Décalage aléatoire pour éviter les superpositions
                Vector3 offset = new Vector3(
                    Random.Range(-1.5f, 1.5f),
                    0f,
                    Random.Range(-1.5f, 1.5f)
                );

                GameObject enemy = Instantiate(
                    spawnInfo.EnemyPrefab,
                    spawnPoint.position + offset,
                    spawnPoint.rotation
                );

                aliveCount++;
                totalSpawned++;
                spawnIndex++;

                var health = enemy.GetComponent<EnemyHealth>();
                if (health != null)
                {
                    health.OnDeath.AddListener(() =>
                    {
                        aliveCount = Mathf.Max(0, aliveCount - 1);
                        Debug.Log("[WaveSpawner] Enemy died, alive count: " + aliveCount);
                    });
                }
                else
                {
                    Debug.LogWarning("[WaveSpawner] Enemy has no EnemyHealth component, using fallback tracking");
                    StartCoroutine(TrackDestructionAndDecrement(enemy));
                }
            }
        }

        Debug.Log("[WaveSpawner] Spawning wave " + (waveIndex + 1) + " with " + totalSpawned + " enemies");

        if (totalSpawned == 0)
        {
            Debug.LogWarning("[WaveSpawner] Wave has 0 enemies, moving to next wave");
        }

        waitingForClear = true;
    }

    private IEnumerator TrackDestructionAndDecrement(GameObject go)
    {
        while (go != null)
            yield return null;

        aliveCount = Mathf.Max(0, aliveCount - 1);
        Debug.Log("[WaveSpawner] Object destroyed (fallback), alive count: " + aliveCount);
    }

    private void IncWave()
    {
        if (waveIndex + 1 < waves.Length)
        {
            waveIndex++;
            currentWave = waves[waveIndex];
        }
        else
        {
            stopSpawning = true;
            Debug.Log("[WaveSpawner] No more waves, stopSpawning = true");
        }
    }

    private void CloseDoors()
    {
        if (doors == null || doors.Length == 0)
        {
            Debug.LogWarning("[WaveSpawner] No doors assigned!");
            return;
        }

        foreach (var door in doors)
        {
            if (door != null)
                door.isClosed = true;
        }
        Debug.Log("[WaveSpawner] All doors closed");
    }

    private void OpenDoors()
    {
        if (doors == null || doors.Length == 0) return;

        foreach (var door in doors)
        {
            if (door != null)
                door.isClosed = false;
        }
        
        // Désactive le mode arène : les ennemis retrouvent leur comportement normal
        EnemyAlertSystem.Instance.SetAllEnemiesArenaMode(false);
        
        Debug.Log("[WaveSpawner] All doors opened, arena mode disabled");
    }
}
