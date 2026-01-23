using UnityEngine;

namespace FPS
{
    /// <summary>
    /// Gère la rotation de la caméra
    /// </summary>
    public class FPSMouseLook : MonoBehaviour
    {
        [Header("Camera Settings")]
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private float mouseSensitivity = 2f;
        [SerializeField] private float minVerticalAngle = -80f;
        [SerializeField] private float maxVerticalAngle = 80f;

        private float verticalRotation;
        private float yaw;

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
            
            // Initialiser avec la rotation actuelle
            yaw = transform.eulerAngles.y;
            verticalRotation = 0f;
        }

        public void Look(Vector2 lookInput)
        {
            // Rotation horizontale (corps du joueur)
            float mouseX = lookInput.x * mouseSensitivity;
            yaw += mouseX;
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            
            // Rotation verticale (caméra)
            float mouseY = lookInput.y * mouseSensitivity;
            verticalRotation -= mouseY;
            verticalRotation = Mathf.Clamp(verticalRotation, minVerticalAngle, maxVerticalAngle);
            
            // Appliquer la rotation à la caméra
            cameraTransform.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
        }

        public Transform CameraTransform => cameraTransform;
    }
}
