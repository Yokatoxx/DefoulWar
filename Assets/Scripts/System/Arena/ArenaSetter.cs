using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "ArenaSetter", menuName = "REDACTED_PROJECT_NAME/Arena/ArenaSetter", order = 1)]
public class ArenaSetter : ScriptableObject
{
    public int nbEnnemies;
    public List<Transform> spawnerPoints;
    public List<GameObject> ennemiesPrefabs;


}
