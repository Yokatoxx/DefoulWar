using System.Collections.Generic;
using UnityEngine;

public class WavManager : MonoBehaviour
{
    [Header("Spawn")]
    public GameObject enemyPrefab;
    public List<Transform> spawnPoints = new List<Transform>();

    [Header("Auto spawn (one wave)")]
    public bool spawnOnStart = true;
    [Tooltip("Nombre d'ennemis à spawn pour la vague automatique")]
    public int initialWaveCount = 50;

    // Spawns 'count' enemies evenly across spawnPoints
    public void SpawnWave(int count)
    {
        if (enemyPrefab == null || spawnPoints.Count == 0) return;

        int spCount = spawnPoints.Count;
        int basePerSpawn = count / spCount;
        int remainder = count % spCount;

        for (int i = 0; i < spCount; i++)
        {
            int spawnForThis = basePerSpawn + (i < remainder ? 1 : 0);
            for (int j = 0; j < spawnForThis; j++)
            {
                var go = Instantiate(enemyPrefab, spawnPoints[i].position, spawnPoints[i].rotation);
                var enemy = go.GetComponent<EnemyAI>();
                if (enemy != null)
                    enemy.spawnPoint = spawnPoints[i];
            }
        }
    }

    // Spawn one wave automatically on Start (if enabled).
    private void Start()
    {
        if (spawnOnStart && initialWaveCount > 0)
        {
            SpawnWave(initialWaveCount);
        }
    }
}