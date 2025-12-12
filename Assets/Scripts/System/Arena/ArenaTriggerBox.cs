using UnityEngine;

public class ArenaTriggerBox : MonoBehaviour
{
    [SerializeField] private WaveSpawner arena;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            arena.TriggerArena();    
            gameObject.SetActive(false);
        }
    }
}
