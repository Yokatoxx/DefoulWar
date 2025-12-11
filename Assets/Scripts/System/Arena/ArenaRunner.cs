using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FPS; // pour EnemyHealth

public class ArenaRunner : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private ArenaSetter arena;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private int currentWaveIndex;
    private bool running;
    private readonly List<GameObject> alive = new();
    private readonly Dictionary<string, SpawnPointGroup> groupsById = new();

    private void Awake()
    {
        if (arena == null)
        {
            Debug.LogError("[ArenaRunner] ArenaSetter non assigné.");
            enabled = false;
            return;
        }
        IndexSpawnGroupsInScene();
    }

    private void IndexSpawnGroupsInScene()
    {
        groupsById.Clear();
        var groups = FindObjectsOfType<SpawnPointGroup>(true);
        foreach (var g in groups)
        {
            if (string.IsNullOrEmpty(g.groupId)) continue;
            groupsById[g.groupId] = g;
        }
        if (debugLogs) Debug.Log($"[ArenaRunner] Groupes indexés: {groupsById.Count}");
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
            var wave = arena.waves[currentWaveIndex];
            if (debugLogs) Debug.Log($"[ArenaRunner] Vague {currentWaveIndex + 1}/{wavesToPlay}");
            yield return SpawnWave(wave);

            if (arena.waitForWaveClear)
                yield return WaitWaveClear();
        }

        if (arena.door != null) arena.door.isOpen = true;
        running = false;
        if (debugLogs) Debug.Log("[ArenaRunner] Arène terminée.");
    }

    private IEnumerator SpawnWave(ArenaWave wave)
    {
        if (wave == null || wave.batches == null) yield break;

        foreach (var batch in wave.batches)
        {
            if (batch == null || batch.enemyPrefab == null || batch.count <= 0) continue;

            var group = ResolveGroup(batch.spawnGroupId);
            for (int i = 0; i < batch.count; i++)
            {
                Transform spawn = group?.GetRandom();
                if (spawn == null)
                {
                    Debug.LogWarning($"[ArenaRunner] Aucun point de spawn valide pour groupe '{batch.spawnGroupId}'.");
                    yield break;
                }

                var enemy = Instantiate(batch.enemyPrefab, spawn.position, spawn.rotation);
                RegisterEnemy(enemy);

                if (arena.delayBetweenEnemies > 0f)
                    yield return new WaitForSeconds(arena.delayBetweenEnemies);
            }
        }
    }

    private SpawnPointGroup ResolveGroup(string id)
    {
        if (!string.IsNullOrEmpty(id) && groupsById.TryGetValue(id, out var g))
            return g;

        // Fallback: premier groupe trouvé dans la scène
        foreach (var kv in groupsById) return kv.Value;
        return null;
    }

    private IEnumerator WaitWaveClear()
    {
        alive.RemoveAll(e => e == null);
        while (alive.Count > 0)
        {
            alive.RemoveAll(e => e == null || !e.activeInHierarchy);
            yield return null;
        }
        if (debugLogs) Debug.Log("[ArenaRunner] Vague nettoyée.");
    }

    private void RegisterEnemy(GameObject enemy)
    {
        alive.Add(enemy);

        var health = enemy.GetComponentInChildren<EnemyHealth>();
        if (health != null)
        {
            health.OnDeath.AddListener(() =>
            {
                alive.Remove(enemy);
            });
        }
    }
}