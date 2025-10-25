using UnityEngine;

namespace Proto3GD.FPS
{
    /// <summary>
    /// Gère la rotation de la caméra et le lean (inclinaison)
    /// </summary>
    public class FPSMouseLook : MonoBehaviour
    {
        [Header("Camera Settings")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private float minVerticalAngle = -80f;
        [SerializeField] private float maxVerticalAngle = 80f;

        [Header("Lean Settings")]
        [SerializeField] private float leanAngle = 15f;
        [SerializeField] private float leanSpeed = 8f;

        private float verticalRotation = 0f;
        private float currentLean = 0f;

        private void Awake()
        {
            // Créer la caméra si elle n'existe pas
            if (cameraTransform == null)
            {
                GameObject camObj = new GameObject("PlayerCamera");
                camObj.transform.SetParent(transform);
                camObj.transform.localPosition = new Vector3(0, 0.8f, 0);
                cameraTransform = camObj.transform;

                Camera createdCam = camObj.AddComponent<Camera>();
                createdCam.fieldOfView = 60f;
                camObj.tag = "MainCamera";
            }
            
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        public void Look(Vector2 lookInput, Vector2 moveInput, bool leanLeft, bool leanRight)
        {
            // Rotation horizontale (corps du joueur)
            float mouseX = lookInput.x * mouseSensitivity;
            transform.Rotate(Vector3.up * mouseX);
            
            // Rotation verticale (caméra)
            float mouseY = lookInput.y * mouseSensitivity;
            verticalRotation -= mouseY;
            verticalRotation = Mathf.Clamp(verticalRotation, minVerticalAngle, maxVerticalAngle);
            
            // Lean (inclinaison)
            float targetLean = 0f;
            if (Mathf.Abs(moveInput.x) > 0.01f)
            {
                targetLean = Mathf.Clamp(moveInput.x, -1f, 1f) * leanAngle;
            }
            if (leanLeft) targetLean = -leanAngle;
            if (leanRight) targetLean = leanAngle;
            
            currentLean = Mathf.Lerp(currentLean, targetLean, Time.deltaTime * leanSpeed);
            
            // Appliquer les rotations à la caméra
            cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, currentLean);
        }

        public Transform CameraTransform => cameraTransform;
    }
}

