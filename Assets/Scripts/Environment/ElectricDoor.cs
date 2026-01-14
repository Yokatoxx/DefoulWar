using UnityEngine;

namespace Environment
{
    /// <summary>
    /// Porte qui s'ouvre en descendant quand elle est activée par un ElectricDoorCharger.
    /// </summary>
    public class ElectricDoor : MonoBehaviour
    {
        [Header("Door Settings")]
        [Tooltip("L'objet porte à déplacer (si différent de ce GameObject)")]
        [SerializeField] private Transform doorTransform;
        
        [Tooltip("Vitesse de déplacement de la porte")]
        [SerializeField] private float doorSpeed = 3f;
        
        [Tooltip("Offset de position quand la porte est ouverte (relatif à la position fermée)")]
        [SerializeField] private Vector3 openOffset = new Vector3(0, -5f, 0);
        
        [Header("Visual Effects")]
        [Tooltip("Effet visuel à jouer lors de l'ouverture")]
        [SerializeField] private GameObject openEffectPrefab;
        [Tooltip("Son à jouer lors de l'ouverture")]
        [SerializeField] private AudioClip openSound;
        
        private Vector3 closedPosition;
        private Vector3 openPosition;
        private bool isOpen;
        private bool isMoving;
        private AudioSource audioSource;
        
        private void Awake()
        {
            if (doorTransform == null)
            {
                doorTransform = transform;
            }
            
            audioSource = GetComponent<AudioSource>();
        }
        
        private void Start()
        {
            closedPosition = doorTransform.position;
            openPosition = closedPosition + openOffset;
        }
        
        private void Update()
        {
            if (!isMoving) return;
            
            Vector3 targetPosition = isOpen ? openPosition : closedPosition;
            
            doorTransform.position = Vector3.MoveTowards(
                doorTransform.position, 
                targetPosition, 
                doorSpeed * Time.deltaTime
            );
            
            // Arrêter le mouvement quand on atteint la cible
            if (Vector3.Distance(doorTransform.position, targetPosition) < 0.01f)
            {
                doorTransform.position = targetPosition;
                isMoving = false;
            }
        }
        
        /// <summary>
        /// Ouvre la porte (la fait descendre).
        /// </summary>
        public void Open()
        {
            if (isOpen) return;
            
            isOpen = true;
            isMoving = true;
            
            PlayOpenEffects();
        }
        
        /// <summary>
        /// Ferme la porte (la fait remonter).
        /// </summary>
        public void Close()
        {
            if (!isOpen) return;
            
            isOpen = false;
            isMoving = true;
        }
        
        /// <summary>
        /// Bascule l'état de la porte.
        /// </summary>
        public void Toggle()
        {
            if (isOpen) Close();
            else Open();
        }
        
        private void PlayOpenEffects()
        {
            if (openEffectPrefab != null)
            {
                GameObject effect = Instantiate(openEffectPrefab, doorTransform.position, Quaternion.identity);
                Destroy(effect, 3f);
            }
            
            if (openSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(openSound);
            }
        }
        
        // Propriétés publiques
        public bool IsOpen => isOpen;
        public bool IsMoving => isMoving;
        
        private void OnDrawGizmosSelected()
        {
            Vector3 currentPos = doorTransform != null ? doorTransform.position : transform.position;
            Vector3 openPos = currentPos + openOffset;
            
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(currentPos, Vector3.one * 0.5f);
            
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(openPos, Vector3.one * 0.5f);
            
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(currentPos, openPos);
        }
    }
}
