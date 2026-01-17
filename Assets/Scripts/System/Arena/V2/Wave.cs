using System;
using UnityEngine;

[Serializable]
public class EnemySpawnInfo
{
    public GameObject EnemyPrefab;
    public int Count;
}

[CreateAssetMenu(fileName = "Wave", menuName = "ScriptableObjects/Waves", order = 1)]
public class Wave : ScriptableObject
{
    public float TimeBeforeThisWave;
    
    [Tooltip("Liste des types d'ennemis avec leur nombre respectif")]
    public EnemySpawnInfo[] EnemySpawnList;

    // Calcule le nombre total d'ennemis à spawner
    public int TotalEnemyCount
    {
        get
        {
            int total = 0;
            if (EnemySpawnList != null)
            {
                foreach (var info in EnemySpawnList)
                {
                    if (info != null && info.EnemyPrefab != null)
                        total += info.Count;
                }
            }
            return total;
        }
    }
}