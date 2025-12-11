using UnityEngine;

public class ArenaTriggerBox : MonoBehaviour
{
    [SerializeField] private ArenaRunner arenaRunner;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            arenaRunner.TriggerArena();
            gameObject.SetActive(false);
        }
    }
}
