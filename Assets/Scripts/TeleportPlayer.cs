using UnityEngine;

public class TeleportPlayer : MonoBehaviour
{
    [Header("Références")]
    [Tooltip("Référence vers le joueur à téléporter")]
    public Transform player;

    [Tooltip("Position ou Transform de destination")]
    public Transform destination;

    [Header("Déclenchement manuel")]
    public KeyCode teleportKey = KeyCode.T;

    private void Update()
    {
        // Si tu veux déclencher la téléportation avec une touche
        if (Input.GetKeyDown(teleportKey))
        {
            Teleport();
        }
    }

    public void Teleport()
    {
        if (player == null || destination == null)
        {
            Debug.LogWarning("⚠️ Player ou Destination non assigné !");
            return;
        }

        // Déplace directement le joueur
        player.position = destination.position;
        player.rotation = destination.rotation;
        
    }

    // Si tu veux que ça se fasse automatiquement à l’entrée d’un trigger :
    private void OnTriggerEnter(Collider other)
    {
        if (other.transform == player)
        {
            Teleport();
        }
    }
}
