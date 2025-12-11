using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ArenaRunner : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private ArenaSetter arena;

    [Header("Runtime")]
    [SerializeField, Tooltip("Activer le log debug.")]
    private bool debugLogs = false;

    private int currentWaveIndex = 0;
    private readonly List<GameObject> aliveEnemies = new List<GameObject>();
    private bool running;

    private void Start()
    {
        if (arena == null)
        {
            enabled = false;
            return;
        }
    }

    public void TriggerArena()
    {
        if (running) return;
        arena.TriggerWave();
        running = true;
        currentWaveIndex = 0;
        StartCoroutine(RunWaves());
    }

    private IEnumerator RunWaves()
    {
        int wavesToPlay = Mathf.Min(arena.totalWaves, arena.waves.Count);

        for (; currentWaveIndex < wavesToPlay; currentWaveIndex++)
        {
            ArenaWave wave = arena.waves[currentWaveIndex];
            if (debugLogs) Debug.Log($"[ArenaRunner] Lancement vague {currentWaveIndex + 1}/{wavesToPlay}");

            yield return StartCoroutine(SpawnWave(wave));

            if (arena.waitForWaveClear)
            {
                // Attendre que tous les ennemis de la vague soient morts/retirés
                yield return StartCoroutine(WaitWaveClear());
            }
        }

        // Fin d'arène: ouvrir la porte
        if (arena.door != null) arena.door.isOpen = true;
        running = false;
        if (debugLogs) Debug.Log("[ArenaRunner] Arène terminée.");
    }

    private IEnumerator SpawnWave(ArenaWave wave)
    {
        if (wave == null || wave.batches == null || wave.batches.Count == 0)
            yield break;

        foreach (var batch in wave.batches)
        {
            if (batch == null || batch.enemyPrefab == null || batch.count <= 0)
                continue;

            for (int i = 0; i < batch.count; i++)
            {
                Transform spawn = PickSpawnPoint();
                if (spawn == null)
                {
                    Debug.LogWarning("[ArenaRunner] Aucun spawn point disponible.");
                    yield break;
                }

                GameObject enemy = Instantiate(batch.enemyPrefab, spawn.position, spawn.rotation);
                RegisterEnemy(enemy);

                if (arena.delayBetweenEnemies > 0f)
                    yield return new WaitForSeconds(arena.delayBetweenEnemies);
            }
        }
    }

    private IEnumerator WaitWaveClear()
    {
        // Nettoyer nulls (cas destruction)
        aliveEnemies.RemoveAll(e => e == null);

        while (aliveEnemies.Count > 0)
        {
            // Purger les morts chaque frame
            aliveEnemies.RemoveAll(e => e == null || !e.activeInHierarchy);
            yield return null;
        }

        if (debugLogs) Debug.Log("[ArenaRunner] Vague nettoyée.");
    }

    private Transform PickSpawnPoint()
    {
        var spawns = arena.spawnPoints;
        if (spawns == null || spawns.Count == 0) return null;
        int idx = Random.Range(0, spawns.Count);
        return spawns[idx];
    }

    private void RegisterEnemy(GameObject enemy)
    {
        aliveEnemies.Add(enemy);

        // Si l'ennemi possède EnemyHealth, désinscrire automatiquement à la mort
        var health = enemy.GetComponentInChildren<FPS.EnemyHealth>();
        if (health != null)
        {
            health.OnDeath.AddListener(() =>
            {
                aliveEnemies.Remove(enemy);
            });
        }
        else
        {
            // Fallback: on laisse la purge via WaitWaveClear (activeInHierarchy / null)
        }
    }
}