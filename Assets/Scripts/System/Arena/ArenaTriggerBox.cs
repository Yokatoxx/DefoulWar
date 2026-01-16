using UnityEngine;

public class ArenaTriggerBox : MonoBehaviour
{
    [SerializeField] private WaveSpawner arena;
    [SerializeField] private DoorArena door;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            arena.TriggerArena();
            
            // Ferme la porte de l'arène
            if (door != null)
            {
                door.isClosed = true;
            }
            
            gameObject.SetActive(false);
        }
    }
}
