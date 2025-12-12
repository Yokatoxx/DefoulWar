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

    private bool waitingForStartDelay = true;
    private bool waitingForClear = false;

    private bool stopSpawning = false;

    private void Awake()
    {
        currentWave = waves[waveIndex];
        nextWaveStartTime = Time.time + Mathf.Max(0f, currentWave.TimeBeforeThisWave);
        waitingForStartDelay = true;
        waitingForClear = false;
    }

    private void Update()
    {
        if (stopSpawning)
            return;

        // Étape 1: attendre le délai avant de démarrer la vague
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
                // Tous les ennemis de la vague sont morts -> passer à la suivante
                IncWave();

                if (!stopSpawning)
                {
                    // Programmer le démarrage de la prochaine vague après son délai
                    nextWaveStartTime = Time.time + Mathf.Max(0f, currentWave.TimeBeforeThisWave);
                    waitingForStartDelay = true;
                    waitingForClear = false;
                }
            }
        }
    }

    private void SpawnWave()
    {
        // Réinitialiser le compteur pour la nouvelle vague
        aliveCount = 0;

        // Assurer un entier pour NumberToSpawn
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

            // Tenter de récupérer EnemyHealth pour suivre la mort
            var health = enemy.GetComponent<EnemyHealth>();
            if (health != null)
            {
                aliveCount++;

                // Abonnement à OnDeath pour décrémenter le compteur
                health.OnDeath.AddListener(() =>
                {
                    // Protéger contre les multiples appels potentiels
                    aliveCount = Mathf.Max(0, aliveCount - 1);
                });
            }
            else
            {
                // Si pas de composant santé, considérer l'ennemi comme "non traçable" mais le compter
                // et prévoir une décrémentation à la destruction (fallback)
                aliveCount++;

                // Fallback: décrémenter quand le GameObject est détruit
                StartCoroutine(TrackDestructionAndDecrement(enemy));
            }
        }
    }

    private IEnumerator TrackDestructionAndDecrement(GameObject go)
    {
        // Attendre tant que l'objet existe
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