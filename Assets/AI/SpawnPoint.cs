using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    [Tooltip("Id unique du spawn point (peut être défini manuellement)")]
    public int spawnId = 0;

    void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(transform.position, 0.3f);
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, $"Spawn {spawnId}");
#endif
    }
}