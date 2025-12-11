using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class EnemyWaveSetter
{
    [Tooltip("Prefab d'ennemi (type).")]
    public GameObject enemyPrefab;

    [Tooltip("Nombre d'ennemis à spawn pour ce type.")]
    public int count = 1;

    [Tooltip("Identifiant du groupe de points de spawn à utiliser dans la scène (SpawnPointGroup.groupId).")]
    public string spawnGroupId = "";
}

[CreateAssetMenu(fileName = "ArenaWave", menuName = "REDACTED_PROJECT_NAME/Arena/ArenaWave", order = 1)]
public class ArenaWave : ScriptableObject
{
    [Tooltip("Batches d'ennemis pour cette vague.")]
    public List<EnemyWaveSetter> batches = new List<EnemyWaveSetter>();
}

[CreateAssetMenu(fileName = "ArenaSetter", menuName = "REDACTED_PROJECT_NAME/Arena/ArenaSetter", order = 0)]
public class ArenaSetter : ScriptableObject
{
    [Header("Paramètres")]
    public int totalWaves = 1;
    public List<ArenaWave> waves = new List<ArenaWave>();

    [Tooltip("Délai entre chaque ennemi (seconds).")]
    public float delayBetweenEnemies = 0.35f;

    [Tooltip("Attendre que la vague soit entièrement nettoyée avant de lancer la suivante.")]
    public bool waitForWaveClear = true;

    [Header("Porte de l'arène")]
    public DoorArena door;

    [HideInInspector] public bool waveStarted = false;

    public void TriggerWave()
    {
        waveStarted = true;
        if (door != null) door.isOpen = false;
    }
}