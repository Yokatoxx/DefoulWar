using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class WaveSetter
{
    public GameObject enemyPrefab;
    public List<Transform> spawnPoints = new List<Transform>();
    public int count = 1;
    public int spawnWaveNb = 0;


}

public class ArenaSetter : MonoBehaviour
{
    public int totalWaves = 1;
    public float delayBetweenEnemies = 0.35f;
    public float delayBetweenWaves = 2.5f;

    public List<WaveSetter> waves = new List<WaveSetter>();

    public DoorArena door;

    public bool waitForWaveClear = true;
    [HideInInspector] public bool waveStarted = false;

    public void TriggerWave()
    {
        waveStarted = true;
        if (door != null) door.isOpen = false;
    }
}