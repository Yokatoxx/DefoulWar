using FPS;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveSpawner : MonoBehaviour
{
    public Wave[] waves;

    private Wave currentWave;

    [SerializeField]
    private Transform[] spawnpoints;

    private float nextWaveStartTime;

    private int waveIndex = 0;
    private int aliveCount = 0;

    private bool waitingForStartDelay = false; // ne pas démarrer automatiquement
    private bool waitingForClear = false;

    private bool stopSpawning = false;

    private void Awake()
    {
        // Préparer la première vague sans la lancer
        if (waves != null && waves.Length > 0)
        {
            waveIndex = 0;
            currentWave = waves[waveIndex];
        }
        // Aucun délai programmé tant que TriggerArena n'est pas appelé
        waitingForStartDelay = false;
        waitingForClear = false;
    }

    // Lance la première vague à la demande.
    // respectDelay=true: attend TimeBeforeThisWave avant de spawner.
    // respectDelay=false: spawn immédiat.
    public void TriggerArena(bool respectDelay = true)
    {
        if (stopSpawning || waves == null || waves.Length == 0)
            return;

        waveIndex = 0;
        currentWave = waves[waveIndex];

        if (respectDelay)
        {
            nextWaveStartTime = Time.time + Mathf.Max(0f, currentWave.TimeBeforeThisWave);
            waitingForStartDelay = true;
            waitingForClear = false;
        }
        else
        {
            SpawnWave();
            waitingForStartDelay = false;
            waitingForClear = true;
        }
    }

    private void Update()
    {
        if (stopSpawning)
            return;

        // Étape 1: attendre le délai avant de démarrer la vague (uniquement après TriggerArena)
        if (waitingForStartDelay)
        {
            if (Time.time >= nextWaveStartTime)
            {
                SpawnWave();
                waitingForStartDelay = false;
                waitingForClear = true;
            }
            return;
        }

        // Étape 2: attendre que tous les ennemis de la vague soient morts
        if (waitingForClear)
        {
            if (aliveCount <= 0)
            {
                IncWave();

                if (!stopSpawning)
                {
                    nextWaveStartTime = Time.time + Mathf.Max(0f, currentWave.TimeBeforeThisWave);
                    waitingForStartDelay = true;
                    waitingForClear = false;
                }
            }
        }
    }

    private void SpawnWave()
    {
        aliveCount = 0;

        int countToSpawn = Mathf.Max(0, Mathf.RoundToInt(currentWave.NumberToSpawn));

        for (int i = 0; i < countToSpawn; i++)
        {
            int enemyIndex = Random.Range(0, currentWave.EnemiesInWave.Length);
            int spawnIndex = Random.Range(0, spawnpoints.Length);

            GameObject enemy = Instantiate(
                currentWave.EnemiesInWave[enemyIndex],
                spawnpoints[spawnIndex].position,
                spawnpoints[spawnIndex].rotation
            );

            var health = enemy.GetComponent<EnemyHealth>();
            aliveCount++;
            if (health != null)
            {
                health.OnDeath.AddListener(() =>
                {
                    aliveCount = Mathf.Max(0, aliveCount - 1);
                });
            }
            else
            {
                StartCoroutine(TrackDestructionAndDecrement(enemy));
            }
        }
    }

    private IEnumerator TrackDestructionAndDecrement(GameObject go)
    {
        while (go != null)
            yield return null;

        aliveCount = Mathf.Max(0, aliveCount - 1);
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
        }
    }
}