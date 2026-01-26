using UnityEngine;

public class NPCLookAtPlayer : MonoBehaviour
{
    public Transform player;          // assigner le Player dans l'Inspector
    public float rotationSpeed = 5f;  // vitesse de rotation

    private void Update()
    {
        if (player == null) return;

        // direction vers le joueur
        Vector3 direction = player.position - transform.position;
        direction.y = 0; // ignore la hauteur pour ne pas pencher la tête

        if (direction.magnitude > 0.1f)
        {
            // rotation cible
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            // rotation lissée
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }
    }
}