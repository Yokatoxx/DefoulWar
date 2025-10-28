using UnityEngine;

namespace Proto3GD.FPS
{
    /// <summary>
    /// Gère les effets visuels de la caméra (headbob, FOV dynamique)
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class FPSCameraEffects : MonoBehaviour
    {
        [Header("Headbob Settings")]
        [SerializeField] private float bobFrequency = 1.8f;
        [SerializeField] private float bobHorizontalAmplitude = 0.08f;
        [SerializeField] private float bobVerticalAmplitude = 0.05f;
        [SerializeField] private float bobSmoothing = 8f;

        [Header("FOV Settings")]
        [SerializeField] private float minFOV = 60f;
        [SerializeField] private float maxFOV = 90f;
        [SerializeField] private float minSpeedForFOV = 5f;
        [SerializeField] private float maxSpeedForFOV = 20f;
        [SerializeField] private float jumpFOV = 45f;
        [SerializeField] private float fovTransitionSpeed = 8f;
        [SerializeField, Tooltip("Caméra de l'arme à synchroniser avec le FOV principal")]
        private Camera weaponCamera;

        private Camera cam;
        private float targetFOV;
        private float bobTimer;
        private Vector3 camDefaultLocalPos;
        private bool isJumping;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam == null)
            {
                cam = gameObject.AddComponent<Camera>();
            }
            
            camDefaultLocalPos = cam.transform.localPosition;
        }
        
        private void Start()
        {
            // Initialiser le FOV après que la caméra soit créée
            if (cam != null)
            {
                targetFOV = minFOV;
                cam.fieldOfView = minFOV;
            }
            
            // Initialiser le FOV de la caméra d'arme
            if (weaponCamera != null)
            {
                weaponCamera.fieldOfView = minFOV;
            }
        }

        private void Update()
        {
            UpdateFOV();
        }

        public void UpdateEffects(bool isGrounded, bool isMoving, float currentSpeed, Vector2 moveInput, bool isSprinting)
        {
            // Gérer le FOV selon l'état
            if (!isGrounded && !isJumping)
            {
                targetFOV = jumpFOV;
                isJumping = true;
            }
            else if (isGrounded)
            {
                if (isJumping)
                {
                    isJumping = false;
                }
                
                // FOV basé sur la vitesse
                float speedRatio = Mathf.InverseLerp(minSpeedForFOV, maxSpeedForFOV, currentSpeed);
                targetFOV = Mathf.Lerp(minFOV, maxFOV, speedRatio);
            }

            // Headbob
            if (isMoving && isGrounded)
            {
                bobTimer += Time.deltaTime * bobFrequency * currentSpeed;
                float bobX = Mathf.Sin(bobTimer) * bobHorizontalAmplitude;
                float bobY = Mathf.Cos(bobTimer * 2f) * bobVerticalAmplitude;
                Vector3 targetPos = camDefaultLocalPos + new Vector3(bobX, bobY, 0f);
                transform.localPosition = Vector3.Lerp(cam.transform.localPosition, targetPos, Time.deltaTime * bobSmoothing);

            }
            else
            {
                bobTimer = 0f;
                transform.localPosition = Vector3.Lerp(transform.localPosition, camDefaultLocalPos, Time.deltaTime * bobSmoothing);
            }
        }

        private void UpdateFOV()
        {
            if (cam != null)
            {
                cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, targetFOV, Time.deltaTime * fovTransitionSpeed);
                
                // Synchroniser le FOV de la caméra d'arme
                if (weaponCamera != null)
                {
                    weaponCamera.fieldOfView = cam.fieldOfView;
                }
            }
        }

        public Camera Camera => cam;
    }
}
