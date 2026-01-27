using UnityEngine;

public class HelicopterTrigger : MonoBehaviour
{
    public HelicopterPathMover helicopter;

    private bool triggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggered)
            return;

        if (other.CompareTag("Player"))
        {
            triggered = true;
            helicopter.StartMovement();
        }
    }
}
