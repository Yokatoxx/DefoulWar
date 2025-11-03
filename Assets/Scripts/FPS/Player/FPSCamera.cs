using UnityEngine;

namespace FPS
{
    /// <summary>
    /// Gère la rotation de la caméra FPS, y compris le pitch vertical.
    /// </summary>
    public class FPSCamera : MonoBehaviour
    {
        [Header("Camera Settings")]
        [SerializeField] private float lookSpeed = 2f;
        [SerializeField] private Transform playerBody; // Référence au corps du joueur pour la rotation horizontale

        private float cameraPitch = 0f;

        private void Start()
        {
            if (playerBody == null)
            {
                // Essayer de trouver le corps du joueur dynamiquement s'il n'est pas assigné
                playerBody = transform.root;
                Debug.LogWarning("Player Body not assigned on FPSCamera. Defaulting to root transform.");
            }
        }

        /// <summary>
        /// Applique la rotation de la caméra en fonction des entrées de la souris.
        /// </summary>
        /// <param name="lookInput">Le vecteur 2D des entrées de la souris (X, Y).</param>
        public void Look(Vector2 lookInput)
        {
            // Rotation horizontale (affecte le corps du joueur)
            float mouseX = lookInput.x * lookSpeed;
            if (playerBody != null)
            {
                playerBody.Rotate(Vector3.up * mouseX);
            }

            // Rotation verticale (affecte uniquement la caméra)
            float mouseY = lookInput.y * lookSpeed;
            cameraPitch -= mouseY;
            cameraPitch = Mathf.Clamp(cameraPitch, -89f, 89f);

            // Appliquer la rotation verticale à la caméra
            transform.localEulerAngles = new Vector3(cameraPitch, 0, 0);
        }
    }
}
