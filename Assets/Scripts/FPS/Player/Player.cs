using UnityEngine;

namespace FPS
{
    /// <summary>
    /// Script coordinateur pour le joueur.
    /// Récupère les entrées de FPSInputHandler et les applique
    /// aux composants de mouvement (FPSMovement) et de caméra (FPSCamera).
    /// </summary>
    [RequireComponent(typeof(FPSInputHandler))]
    [RequireComponent(typeof(FPSMovement))]
    public class Player : MonoBehaviour
    {
        private FPSInputHandler inputHandler;
        private FPSMovement fpsMovement;
        private FPSCamera fpsCamera;

        private void Awake()
        {
            inputHandler = GetComponent<FPSInputHandler>();
            fpsMovement = GetComponent<FPSMovement>();
            fpsCamera = GetComponentInChildren<FPSCamera>();

            if (fpsCamera == null)
            {
                Debug.LogError("FPSCamera component not found in children. Player script requires a camera.");
            }
        }

        private void Update()
        {
            // Transmettre les entrées de mouvement
            if (fpsMovement != null && inputHandler != null)
            {
                fpsMovement.Move(inputHandler.MoveInput, inputHandler.SprintPressed, inputHandler.JumpPressed);

                // Consommer l'événement de saut après l'avoir utilisé
                if (inputHandler.JumpPressed)
                {
                    inputHandler.ConsumeJump();
                }
            }

            // Transmettre les entrées de la caméra
            if (fpsCamera != null && inputHandler != null)
            {
                fpsCamera.Look(inputHandler.LookInput);
            }
        }
    }
}
