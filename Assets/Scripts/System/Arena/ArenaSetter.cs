using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemyWaveSetter
{
    public GameObject enemyPrefab;
    public int count = 1;
}

[CreateAssetMenu(fileName = "ArenaWave", menuName = "REDACTED_PROJECT_NAME/Arena/ArenaWave", order = 1)]
public class ArenaWave : ScriptableObject
{
    public List<EnemyWaveSetter> batches = new List<EnemyWaveSetter>();
}

[CreateAssetMenu(fileName = "ArenaSetter", menuName = "REDACTED_PROJECT_NAME/Arena/ArenaSetter", order = 0)]
public class ArenaSetter : ScriptableObject
{
    public int totalWaves = 1;
    public List<ArenaWave> waves = new List<ArenaWave>();

    public List<Transform> spawnPoints = new List<Transform>();

    public float delayBetweenEnemies = 0.35f;

    public bool waitForWaveClear = true;

    public DoorArena door;

    [HideInInspector]
    public bool waveStarted = false;

    public void TriggerWave()
    {
        waveStarted = true;
        if (door != null)
        {
            door.isOpen = false;
        }
    }
}