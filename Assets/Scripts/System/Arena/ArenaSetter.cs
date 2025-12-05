using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;


public class Enemies : MonoBehaviour 
{
    public GameObject enemyPrefab;
    public int nbEnnemies;
}

[CreateAssetMenu(fileName = "ArenaSetter", menuName = "REDACTED_PROJECT_NAME/Arena/ArenaSetter", order = 1)]
public class ArenaSetter : ScriptableObject
{
    public List<Transform> spawnerPoints;
    public List<GameObject> ennemiesPrefabs;
    public DoorArena door;
    public bool waveStarted = false;

    [SerializeField] private float delaySpawnBetweenEnemies = 0.5f;

    public void TriggerWave()
    {
        waveStarted = true;
        door.isOpen = false;
    }



    private void RandomEnemySelector()
    {
        
    }
}
