using UnityEngine;

namespace FPS
{
    /// Contrôleur principal du joueur FPS 
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
            inputHandler = GetComponent<FPSInputHandler>();
            movement = GetComponent<FPSMovement>();
            mouseLook = GetComponent<FPSMouseLook>();
            
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

            // Si le joueur est stun, neutraliser déplacement et actions mais laisser la caméra
            var stun = GetComponent<PlayerStunAutoFire>();
            if (stun != null && stun.IsStunned)
            {
                moveInput = Vector2.zero;
                jump = false;
                sprint = false;
                leanLeft = false;
                leanRight = false;
            }

            // Appliquer le mouvement
            movement.Move(moveInput, sprint, jump);
            
            // Consommer le jump après utilisation
            if (jump)
            {
                inputHandler.ConsumeJump();
            }
            
            mouseLook.Look(lookInput, moveInput, leanLeft, leanRight);
            
            if (cameraEffects != null)
            {
                cameraEffects.UpdateEffects(
                    movement.CurrentSpeed,
                    movement.IsGrounded,
                    sprint,
                    movement.IsMoving
                );
            }
        }

        // Propriétés publiques pour compatibilité avec les autres systèmes
        public Transform CameraTransform => mouseLook.CameraTransform;
        public Rigidbody Rb => movement.Rb;
    }
}
