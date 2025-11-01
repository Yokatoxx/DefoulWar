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
    [RequireComponent(typeof(FPSMouseLook))]
    public class Player : MonoBehaviour
    {
        private FPSInputHandler inputHandler;
        private FPSMovement fpsMovement;
        private FPSMouseLook fpsMouseLook;
        private PlayerStunAutoFire stunComponent;

        private void Awake()
        {
            inputHandler = GetComponent<FPSInputHandler>();
            fpsMovement = GetComponent<FPSMovement>();
            fpsMouseLook = GetComponent<FPSMouseLook>();
            stunComponent = GetComponent<PlayerStunAutoFire>();
        }

        private void Start()
        {
            // Connecter l'événement de changement de vitesse à l'UI Manager
            if (fpsMovement != null && UIManager.Instance != null)
            {
                fpsMovement.OnSpeedChanged.AddListener(UIManager.Instance.UpdateSpeedDisplay);
            }
        }

        private void OnDestroy()
        {
            // Se désabonner de l'événement pour éviter les fuites de mémoire
            if (fpsMovement != null && UIManager.Instance != null)
            {
                fpsMovement.OnSpeedChanged.RemoveListener(UIManager.Instance.UpdateSpeedDisplay);
            }
        }

        private void Update()
        {
            // Récupérer les entrées brutes
            Vector2 moveInput = inputHandler.MoveInput;
            bool jumpInput = inputHandler.JumpPressed;
            bool sprintInput = inputHandler.SprintPressed;
            bool leanLeftInput = inputHandler.LeanLeftPressed;
            bool leanRightInput = inputHandler.LeanRightPressed;

            // Vérifier si le joueur est étourdi
            if (stunComponent != null && stunComponent.IsStunned)
            {
                // Neutraliser les entrées de mouvement
                moveInput = Vector2.zero;
                jumpInput = false;
                sprintInput = false;
                leanLeftInput = false;
                leanRightInput = false;
            }

            // Transmettre les entrées de mouvement
            if (fpsMovement != null)
            {
                fpsMovement.Move(moveInput, sprintInput, jumpInput);

                // Consommer l'événement de saut après l'avoir utilisé
                if (jumpInput)
                {
                    inputHandler.ConsumeJump();
                }
            }

            // Transmettre les entrées de la caméra
            if (fpsMouseLook != null)
            {
                fpsMouseLook.Look(
                    inputHandler.LookInput,
                    moveInput,
                    leanLeftInput,
                    leanRightInput
                );
            }
        }
    }
}
