using UnityEngine;

namespace FPS
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
        [SerializeField] private float defaultFOV = 60f;
        [SerializeField] private float sprintFOV = 70f;
        [SerializeField] private float jumpFOV = 65f;
        [SerializeField] private float fovTransitionSpeed = 8f;

        [Header("References")]
        [SerializeField, Tooltip("Caméra de l'arme à synchroniser avec le FOV principal")]
        private Camera weaponCamera;

        private Camera cam;
        private Vector3 camDefaultLocalPos;
        private float bobTimer;
        private float targetFOV;
        private bool isJumping;

        private void Awake()
        {
            cam = GetComponent<Camera>();
            if (cam == null)
            {
                cam = gameObject.AddComponent<Camera>();
            }
            
            camDefaultLocalPos = transform.localPosition;
        }
        
        private void Start()
        {
            // Initialiser le FOV après que la caméra soit créée
            if (cam != null)
            {
                targetFOV = defaultFOV;
                cam.fieldOfView = defaultFOV;
            }
            
            // Initialiser le FOV de la caméra d'arme
            if (weaponCamera != null)
            {
                weaponCamera.fieldOfView = defaultFOV;
            }
        }

        public void UpdateEffects(float currentSpeed, bool isGrounded, bool isSprinting, bool isMoving)
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
                
                // FOV de sprint ou normal
                if (isSprinting && isMoving)
                {
                    targetFOV = sprintFOV;
                }
                else
                {
                    targetFOV = defaultFOV;
                }
            }

            // Appliquer le headbob
            UpdateHeadbob(currentSpeed, isGrounded, isMoving);
            
            // Appliquer le FOV
            UpdateFOV();
        }

        private void UpdateHeadbob(float currentSpeed, bool isGrounded, bool isMoving)
        {
            if (isGrounded && isMoving && currentSpeed > 0.1f)
            {
                bobTimer += Time.deltaTime * bobFrequency * currentSpeed;
                float bobX = Mathf.Sin(bobTimer) * bobHorizontalAmplitude;
                float bobY = Mathf.Cos(bobTimer * 2f) * bobVerticalAmplitude;
                Vector3 targetPos = camDefaultLocalPos + new Vector3(bobX, bobY, 0f);
                transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, Time.deltaTime * bobSmoothing);
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
        public float CurrentFOV => cam != null ? cam.fieldOfView : defaultFOV;
        public float TargetFOV
        {
            get => targetFOV;
            set => targetFOV = value;
        }
    }
}
