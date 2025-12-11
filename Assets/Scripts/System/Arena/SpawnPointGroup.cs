using System.Collections.Generic;
using UnityEngine;

public class SpawnPointGroup : MonoBehaviour
{
    [Tooltip("Identifiant lu par la config (EnemyWaveSetter.spawnGroupId).")]
    public string groupId = "Default";

    [Tooltip("Points de spawn (déposez vos empties de la scène ici).")]
    public List<Transform> points = new List<Transform>();

    public Transform GetRandom()
    {
        if (points == null || points.Count == 0) return null;
        return points[Random.Range(0, points.Count)];
    }
}