using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Wave", menuName = "ScriptableObjects/Waves", order = 1)]
public class Wave : ScriptableObject
{
    public GameObject[] EnemiesInWave;

    public float TimeBeforeThisWave;
    public float NumberToSpawn;
}