using UnityEngine;

public class TeleportPlayer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform player;
    [SerializeField] private Transform destination; 

    [Header("Settings")]
    public KeyCode teleportKey = KeyCode.T;

    private void Update()
    {
        if (Input.GetKeyDown(teleportKey))
        {
            Teleport();
        }
    }

    private void Teleport()
    {
        if (player == null || destination == null)
        {
            Debug.LogWarning("TeleportPlayer: Player or Destination reference missing.");
            return;
        }
        
        player.position = destination.position;
        player.rotation = destination.rotation;
        
    }
}
