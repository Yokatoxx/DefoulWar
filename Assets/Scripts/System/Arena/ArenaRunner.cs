using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FPS; // EnemyHealth

public class ArenaRunner : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private ArenaSetter arena;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private int currentWave = 0;
    private int ennemiesAlive = 0;
    private bool startingWave = false;

    private void Awake()
    {
        if (arena == null)
        {
            Debug.LogError("[ArenaRunner] ArenaSetter non assigné sur ce composant.");
            enabled = false;
            return;
        }
        // Validation initiale utile pour voir tout de suite les soucis de config
        ValidateArenaConfig();
    }

    private void Update()
    {
        // Si on attend le nettoyage de la vague et qu'il n'y a plus d'ennemis vivants, on lance la suivante
        if (arena.waveStarted && arena.waitForWaveClear && ennemiesAlive <= 0 && !startingWave)
        {
            StartCoroutine(StartNextWave());
            arena.waveStarted = false;
        }
    }

    public void TriggerArena()
    {
        if (startingWave) return;
        StartCoroutine(StartNextWave());
    }

    private IEnumerator StartNextWave()
    {
        startingWave = true;

        if (currentWave >= arena.totalWaves)
        {
            if (debugLogs) Debug.Log("[ArenaRunner] Arena complete!");
            if (arena.door != null) arena.door.isOpen = true;
            startingWave = false;
            yield break;
        }

        // délai entre vagues
        if (arena.delayBetweenWaves > 0f)
            yield return new WaitForSeconds(arena.delayBetweenWaves);

        if (debugLogs) Debug.Log($"[ArenaRunner] Lancement de la vague {currentWave + 1}/{arena.totalWaves}");

        int spawnedThisWave = 0;

        // Parcourir les WaveSetter et ne spawn que celles ciblant l'index courant
        foreach (var wave in arena.waves)
        {
            if (wave == null)
            {
                Debug.LogWarning("[ArenaRunner] WaveSetter null dans la liste arena.waves.");
                continue;
            }

            if (wave.spawnWaveNb != currentWave)
                continue;

            // Validation WaveSetter
            if (wave.enemyPrefab == null)
            {
                Debug.LogWarning($"[ArenaRunner] WaveSetter (index cible {wave.spawnWaveNb}) sans enemyPrefab.");
                continue;
            }
            if (wave.count <= 0)
            {
                if (debugLogs) Debug.LogWarning($"[ArenaRunner] WaveSetter (index {wave.spawnWaveNb}) count <= 0, rien à spawn.");
                continue;
            }
            if (wave.spawnPoints == null || wave.spawnPoints.Count == 0)
            {
                Debug.LogWarning($"[ArenaRunner] WaveSetter (index {wave.spawnWaveNb}) sans spawnPoints.");
                continue;
            }

            // Instancier les ennemis de cette entrée
            for (int i = 0; i < wave.count; i++)
            {
                Transform spawnPoint = PickValidSpawnPoint(wave.spawnPoints);
                if (spawnPoint == null)
                {
                    Debug.LogWarning($"[ArenaRunner] Aucun spawn point valide pour la WaveSetter index {wave.spawnWaveNb}.");
                    continue; // ne pas casser toute la vague, poursuivre
                }

                GameObject enemy = Instantiate(wave.enemyPrefab, spawnPoint.position, spawnPoint.rotation);
                var enemyHealth = enemy.GetComponent<EnemyHealth>();
                if (enemyHealth != null)
                {
                    enemyHealth.OnDeath.AddListener(OnEnemyDeath);
                }
                else
                {
                    if (debugLogs) Debug.LogWarning("[ArenaRunner] Enemy instancié sans EnemyHealth (le décrément ne se fera pas automatiquement).");
                }

                ennemiesAlive++;
                spawnedThisWave++;

                // délai entre ennemis
                if (arena.delayBetweenEnemies > 0f)
                    yield return new WaitForSeconds(arena.delayBetweenEnemies);
            }
        }

        if (spawnedThisWave == 0)
        {
            // Rien spawné pour cette vague (mauvaise config ?), on passe à la suivante pour éviter le blocage
            Debug.LogWarning($"[ArenaRunner] Aucune entrée WaveSetter correspondant à la vague {currentWave} ou config invalide. On passe à la suivante.");
        }

        currentWave++;
        if (debugLogs) Debug.Log($"[ArenaRunner] currentWave -> {currentWave}");

        startingWave = false;

        // Si on ne veut pas attendre la fin de vague, relancer la suivante directement (comportement optionnel)
        if (!arena.waitForWaveClear)
        {
            // eviter boucle infinie: ne lance pas Next si on a atteint totalWaves
            if (currentWave < arena.totalWaves)
                StartCoroutine(StartNextWave());
            else if (arena.door != null) arena.door.isOpen = true;
        }
    }

    private Transform PickValidSpawnPoint(List<Transform> points)
    {
        // Filtrer nulls
        var valid = new List<Transform>();
        foreach (var p in points)
            if (p != null) valid.Add(p);

        if (valid.Count == 0) return null;
        return valid[Random.Range(0, valid.Count)];
    }

    private void OnEnemyDeath()
    {
        ennemiesAlive = Mathf.Max(0, ennemiesAlive - 1);
        if (debugLogs) Debug.Log($"[ArenaRunner] Ennemi mort. Restants: {ennemiesAlive}");
    }

    private void ValidateArenaConfig()
    {
        if (arena.waves == null || arena.waves.Count == 0)
        {
            Debug.LogWarning("[ArenaRunner] ArenaSetter.waves est vide. Aucune vague ne sera spawné.");
            return;
        }

        // Log d’aperçu
        if (debugLogs)
        {
            for (int i = 0; i < arena.waves.Count; i++)
            {
                var w = arena.waves[i];
                if (w == null)
                {
                    Debug.LogWarning($"[ArenaRunner] waves[{i}] est null.");
                    continue;
                }
                Debug.Log($"[ArenaRunner] WaveSetter #{i}: cible spawnWaveNb={w.spawnWaveNb}, prefab={(w.enemyPrefab ? w.enemyPrefab.name : "null")}, count={w.count}, spawnPoints={(w.spawnPoints != null ? w.spawnPoints.Count : 0)}");
            }
        }
    }
}