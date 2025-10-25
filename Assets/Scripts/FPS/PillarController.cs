using UnityEngine;
using UnityEngine.AI;

namespace Proto3GD.FPS
{
    /// <summary>
    /// Contrôle le comportement d'un pilier spawné après la mort d'un ennemi.
    /// Peut inclure des animations, des effets visuels, et une durée de vie optionnelle.
    /// Les piliers sont automatiquement configurés comme obstacles NavMesh.
    /// </summary>
    public class PillarController : MonoBehaviour
    {
        [Header("Lifetime Settings")]
        [Tooltip("Si vrai, le pilier disparaîtra automatiquement après un certain temps")]
        [SerializeField] private bool hasLifetime = false;
        
        [Tooltip("Durée de vie du pilier en secondes")]
        [SerializeField] private float lifetime = 10f;
        
        [Header("Spawn Animation")]
        [Tooltip("Si vrai, le pilier apparaîtra progressivement")]
        [SerializeField] private bool animateSpawn = true;
        
        [Tooltip("Durée de l'animation d'apparition en secondes")]
        [SerializeField] private float spawnDuration = 0.5f;
        
        [Tooltip("Hauteur initiale du pilier (échelle Y)")]
        [SerializeField] private float initialScale = 0f;
        
        [Header("Visual Effects")]
        [Tooltip("Effet de particules à jouer lors de l'apparition")]
        [SerializeField] private GameObject spawnVFX;
        
        [Tooltip("Son à jouer lors de l'apparition")]
        [SerializeField] private AudioClip spawnSound;
        
        [Tooltip("Effet de particules à jouer lors de la destruction")]
        [SerializeField] private GameObject destroyVFX;
        
        [Header("NavMesh Settings")]
        [Tooltip("Si vrai, le pilier sera un obstacle pour le NavMesh")]
        [SerializeField] private bool isNavMeshObstacle = true;
        
        [Tooltip("Si vrai, le pilier peut être découpé par le NavMesh (carve)")]
        [SerializeField] private bool carveNavMesh = true;
        
        [Tooltip("Temps avant que l'obstacle devienne actif (utile pendant l'animation de spawn)")]
        [SerializeField] private float navMeshActivationDelay = 0.5f;
        
        private Vector3 targetScale;
        private float spawnTimer;
        private bool isSpawning;
        private AudioSource audioSource;
        private NavMeshObstacle navMeshObstacle;
        
        private void Awake()
        {
            // Sauvegarder l'échelle cible
            targetScale = transform.localScale;
            
            // Configurer l'audio
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null && spawnSound != null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
                audioSource.playOnAwake = false;
            }
            
            // Configurer le NavMeshObstacle
            SetupNavMeshObstacle();
        }
        
        private void Start()
        {
            // Démarrer l'animation d'apparition
            if (animateSpawn)
            {
                transform.localScale = new Vector3(targetScale.x, initialScale, targetScale.z);
                isSpawning = true;
                spawnTimer = 0f;
            }
            
            // Jouer les effets d'apparition
            PlaySpawnEffects();
            
            // Démarrer le compte à rebours de la durée de vie
            if (hasLifetime)
            {
                Destroy(gameObject, lifetime);
            }
            
            // Activer le NavMeshObstacle après un délai
            if (isNavMeshObstacle && navMeshObstacle != null)
            {
                navMeshObstacle.enabled = false;
                Invoke(nameof(ActivateNavMeshObstacle), navMeshActivationDelay);
            }
        }
        
        private void Update()
        {
            // Animation d'apparition progressive
            if (isSpawning)
            {
                spawnTimer += Time.deltaTime;
                float progress = Mathf.Clamp01(spawnTimer / spawnDuration);
                
                // Interpolation avec courbe ease-out pour un effet plus naturel
                float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
                
                float currentHeight = Mathf.Lerp(initialScale, targetScale.y, easedProgress);
                transform.localScale = new Vector3(targetScale.x, currentHeight, targetScale.z);
                
                if (progress >= 1f)
                {
                    isSpawning = false;
                    transform.localScale = targetScale;
                }
            }
        }
        
        /// <summary>
        /// Joue les effets visuels et sonores lors de l'apparition du pilier.
        /// </summary>
        private void PlaySpawnEffects()
        {
            // Effet de particules
            if (spawnVFX != null)
            {
                GameObject vfx = Instantiate(spawnVFX, transform.position, Quaternion.identity);
                Destroy(vfx, 3f); // Nettoyer après 3 secondes
            }
            
            // Son
            if (spawnSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(spawnSound);
            }
        }
        
        /// <summary>
        /// Permet de détruire le pilier de manière programmée (peut être appelé depuis d'autres scripts).
        /// </summary>
        /// <param name="delay">Délai avant destruction en secondes</param>
        public void DestroyPillar(float delay = 0f)
        {
            // Jouer les effets de destruction
            PlayDestroyEffects();
            
            Destroy(gameObject, delay);
        }
        
        /// <summary>
        /// Change la durée de vie du pilier de manière dynamique.
        /// </summary>
        /// <param name="newLifetime">Nouvelle durée de vie en secondes</param>
        public void SetLifetime(float newLifetime)
        {
            hasLifetime = true;
            lifetime = newLifetime;
            
            // Annuler la destruction précédente et en programmer une nouvelle
            CancelInvoke(nameof(DestroyPillar));
            Invoke(nameof(DestroyPillar), newLifetime);
        }
        
        /// <summary>
        /// Joue les effets visuels lors de la destruction du pilier.
        /// </summary>
        private void PlayDestroyEffects()
        {
            // Effet de particules
            if (destroyVFX != null)
            {
                GameObject vfx = Instantiate(destroyVFX, transform.position, Quaternion.identity);
                Destroy(vfx, 3f); // Nettoyer après 3 secondes
            }
        }
        
        /// <summary>
        /// Configure le composant NavMeshObstacle pour que le pilier soit pris en compte par le NavMesh.
        /// </summary>
        private void SetupNavMeshObstacle()
        {
            if (!isNavMeshObstacle)
                return;
            
            // Ajouter ou récupérer le NavMeshObstacle
            navMeshObstacle = GetComponent<NavMeshObstacle>();
            if (navMeshObstacle == null)
            {
                navMeshObstacle = gameObject.AddComponent<NavMeshObstacle>();
            }
            
            // Configuration de base
            navMeshObstacle.carving = carveNavMesh;
            navMeshObstacle.shape = NavMeshObstacleShape.Box;
            
            // Calculer la taille du collider pour le NavMeshObstacle
            Collider col = GetComponent<Collider>();
            if (col != null)
            {
                if (col is BoxCollider boxCol)
                {
                    navMeshObstacle.center = boxCol.center;
                    navMeshObstacle.size = boxCol.size;
                }
                else if (col is CapsuleCollider capsuleCol)
                {
                    navMeshObstacle.center = capsuleCol.center;
                    navMeshObstacle.size = new Vector3(capsuleCol.radius * 2f, capsuleCol.height, capsuleCol.radius * 2f);
                    navMeshObstacle.shape = NavMeshObstacleShape.Capsule;
                }
                else
                {
                    // Utiliser les bounds du collider
                    Bounds bounds = col.bounds;
                    navMeshObstacle.center = transform.InverseTransformPoint(bounds.center);
                    navMeshObstacle.size = bounds.size;
                }
            }
            else
            {
                // Taille par défaut basée sur l'échelle
                navMeshObstacle.size = Vector3.one;
                navMeshObstacle.center = Vector3.zero;
            }
        }
        
        /// <summary>
        /// Active le NavMeshObstacle après le délai spécifié.
        /// </summary>
        private void ActivateNavMeshObstacle()
        {
            if (navMeshObstacle != null)
            {
                navMeshObstacle.enabled = true;
                Debug.Log($"NavMeshObstacle activé pour {gameObject.name}");
            }
        }
    }
}