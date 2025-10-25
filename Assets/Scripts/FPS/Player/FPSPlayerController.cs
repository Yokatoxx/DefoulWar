using UnityEngine;

namespace Proto3GD.FPS
{
    /// <summary>
    /// Contrôleur principal du joueur FPS - Orchestre tous les sous-systèmes
    /// </summary>
    [RequireComponent(typeof(FPSInputHandler))]
    [RequireComponent(typeof(FPSMovement))]
    [RequireComponent(typeof(FPSMouseLook))]
    public class FPSPlayerController : MonoBehaviour
    {
        private FPSInputHandler inputHandler;
        private FPSMovement movement;
        private FPSMouseLook mouseLook;
        private FPSCameraEffects cameraEffects;

        private void Awake()
        {
            // Récupérer les composants
            inputHandler = GetComponent<FPSInputHandler>();
            movement = GetComponent<FPSMovement>();
            mouseLook = GetComponent<FPSMouseLook>();
            
            // Le CameraEffects est sur la caméra, pas sur le joueur
            if (mouseLook.CameraTransform != null)
            {
                cameraEffects = mouseLook.CameraTransform.GetComponent<FPSCameraEffects>();
                if (cameraEffects == null)
                {
                    cameraEffects = mouseLook.CameraTransform.gameObject.AddComponent<FPSCameraEffects>();
                }
            }
        }

        private void Update()
        {
            // Récupérer les inputs
            Vector2 moveInput = inputHandler.MoveInput;
            Vector2 lookInput = inputHandler.LookInput;
            bool jump = inputHandler.JumpPressed;
            bool sprint = inputHandler.SprintPressed;
            bool leanLeft = inputHandler.LeanLeftPressed;
            bool leanRight = inputHandler.LeanRightPressed;

            // Appliquer le mouvement
            movement.Move(moveInput, sprint, jump);
            
            // Consommer le jump après utilisation
            if (jump)
            {
                inputHandler.ConsumeJump();
            }

            // Appliquer la caméra
            mouseLook.Look(lookInput, moveInput, leanLeft, leanRight);

            // Mettre à jour les effets de caméra (avec vérification null)
            if (cameraEffects != null)
            {
                cameraEffects.UpdateEffects(
                    movement.IsGrounded,
                    movement.IsMoving,
                    movement.CurrentSpeed,
                    moveInput,
                    sprint  // Nouveau paramètre pour le FOV de sprint
                );
            }
        }

        // Propriétés publiques pour compatibilité avec les autres systèmes
        public Transform CameraTransform => mouseLook.CameraTransform;
        public CharacterController Controller => movement.Controller;
    }
}
