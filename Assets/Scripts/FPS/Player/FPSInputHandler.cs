using UnityEngine;

namespace Proto3GD.FPS
{
    /// <summary>
    /// Gère les inputs du joueur (clavier, souris, gamepad)
    /// </summary>
    public class FPSInputHandler : MonoBehaviour
    {
        [Header("Input Settings")]
        [SerializeField] private bool useNewInputSystem = false;
        
        // Events pour communiquer avec les autres composants
        public Vector2 MoveInput { get; private set; }
        public Vector2 LookInput { get; private set; }
        public bool JumpPressed { get; private set; }
        public bool SprintPressed { get; private set; }
        public bool LeanLeftPressed { get; private set; }
        public bool LeanRightPressed { get; private set; }

        private void Update()
        {
            if (!useNewInputSystem)
            {
                HandleOldInput();
            }
        }

        private void HandleOldInput()
        {
            // Mouvement
            float horizontal = Input.GetAxisRaw("Horizontal");
            float vertical = Input.GetAxisRaw("Vertical");
            MoveInput = new Vector2(horizontal, vertical);
            
            // Regard
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            LookInput = new Vector2(mouseX, mouseY);
            
            // Actions
            LeanLeftPressed = Input.GetKey(KeyCode.Q);
            LeanRightPressed = Input.GetKey(KeyCode.E);
            
            // Jump - ne pas utiliser GetKeyDown ici, le gérer directement
            if (Input.GetButtonDown("Jump"))
            {
                JumpPressed = true;
            }
            
            // Cursor
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (Cursor.lockState == CursorLockMode.Locked)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;
                }
                else
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }

        // Méthode publique pour réinitialiser le jump (appelée par le PlayerController après utilisation)
        public void ConsumeJump()
        {
            JumpPressed = false;
        }

        public void OnMove(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (useNewInputSystem)
            {
                MoveInput = context.ReadValue<Vector2>();
            }
        }

        public void OnLook(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (useNewInputSystem)
            {
                LookInput = context.ReadValue<Vector2>();
            }
        }

        public void OnJump(UnityEngine.InputSystem.InputAction.CallbackContext context)
        {
            if (useNewInputSystem && context.performed)
            {
                JumpPressed = true;
            }
        }
    }
}
