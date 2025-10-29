using UnityEngine;

namespace Proto3GD.FPS
{
    /// <summary>
    /// Système qui fait apparaître un pilier à la position d'un ennemi lorsqu'il meurt.
    /// Le pilier apparaît avec une rotation aléatoire entre deux angles définis.
    /// </summary>
    public class PillarSpawner : MonoBehaviour
    {
        [Header("Pillar Prefab")]
        [Tooltip("Le prefab du pilier à faire apparaître")]
        [SerializeField] private GameObject pillarPrefab;
        
        [Header("Rotation Settings")]
        [Tooltip("Angle minimum de rotation (en degrés) sur l'axe X (inclinaison avant/arrière)")]
        [SerializeField] private float minAngleX = -15f;
        
        [Tooltip("Angle maximum de rotation (en degrés) sur l'axe X (inclinaison avant/arrière)")]
        [SerializeField] private float maxAngleX = 15f;
        
        [Tooltip("Angle minimum de rotation (en degrés) sur l'axe Y (rotation horizontale)")]
        [SerializeField] private float minAngleY;
        
        [Tooltip("Angle maximum de rotation (en degrés) sur l'axe Y (rotation horizontale)")]
        [SerializeField] private float maxAngleY = 360f;
        
        [Tooltip("Angle minimum de rotation (en degrés) sur l'axe Z (inclinaison gauche/droite)")]
        [SerializeField] private float minAngleZ = -15f;
        
        [Tooltip("Angle maximum de rotation (en degrés) sur l'axe Z (inclinaison gauche/droite)")]
        [SerializeField] private float maxAngleZ = 15f;
        
        [Header("Spawn Settings")]
        [Tooltip("Décalage vertical par rapport à la position de l'ennemi mort")]
        [SerializeField] private Vector3 spawnOffset;
        
        [Header("Screen Shake Settings")]
        [Tooltip("Active le screenshake lors du spawn")]
        [SerializeField] private bool enableScreenShake = true;
        
        [Tooltip("Durée du screenshake en secondes")]
        [SerializeField] private float shakeDuration = 0.3f;
        
        [Tooltip("Intensité du déplacement de la caméra")]
        [SerializeField] private float shakePositionMagnitude = 0.2f;
        
        [Tooltip("Intensité de la rotation de la caméra")]
        [SerializeField] private float shakeRotationMagnitude = 1.5f;
        
        [Header("Optional Settings")]
        [Tooltip("Parent pour organiser les piliers dans la hiérarchie")]
        [SerializeField] private Transform pillarsContainer;
        
        private void Awake()
        {
            // Créer automatiquement un conteneur pour les piliers si non assigné
            if (pillarsContainer == null)
            {
                GameObject container = new GameObject("Pillars Container");
                pillarsContainer = container.transform;
            }
        }
        
        private void Start()
        {
            // S'abonner aux événements de mort de tous les ennemis existants
            RegisterExistingEnemies();
        }
        
        /// <summary>
        /// Enregistre tous les ennemis existants dans la scène pour écouter leur mort.
        /// </summary>
        private void RegisterExistingEnemies()
        {
            EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(FindObjectsSortMode.None);
            foreach (EnemyHealth enemy in enemies)
            {
                RegisterEnemy(enemy);
            }
        }
        
        /// <summary>
        /// Enregistre un ennemi spécifique pour écouter son événement de mort (seulement si tag "Minion").
        /// </summary>
        /// <param name="enemy">L'ennemi à enregistrer</param>
        public void RegisterEnemy(EnemyHealth enemy)
        {
            if (enemy == null) return;
            // Ne s'intéresser qu'aux ennemis avec le tag "Minion"
            if (!enemy.CompareTag("Minion")) return;

            GameObject enemyObj = enemy.gameObject;
            enemy.OnDeath.AddListener(() => OnEnemyDeath(enemy.transform.position, enemyObj));
        }
        
        /// <summary>
        /// Appelé lorsqu'un ennemi meurt. Fait apparaître un pilier à sa position (uniquement si tag "Minion").
        /// </summary>
        /// <param name="enemyPosition">La position de l'ennemi mort</param>
        /// <param name="enemyObject">Le GameObject de l'ennemi mort</param>
        private void OnEnemyDeath(Vector3 enemyPosition, GameObject enemyObject)
        {
            // Filtrage: uniquement si l'ennemi a le tag "Minion"
            if (enemyObject == null || !enemyObject.CompareTag("Minion"))
            {
                return;
            }

            // Vérifier si l'ennemi a été tué par le dash
            var enemyHealth = enemyObject.GetComponent<EnemyHealth>();
            if (enemyHealth != null && enemyHealth.KilledByDash)
            {
                Debug.Log($"Ennemi tué par dash - Pas de pilier spawné");
                return; // Ne pas spawner de pilier si tué par dash
            }

            if (pillarPrefab == null)
            {
                Debug.LogWarning("PillarSpawner: Aucun prefab de pilier assigné!");
                return;
            }

            SpawnPillar(enemyPosition);
        }
        
        /// <summary>
        /// Fait apparaître un pilier à la position spécifiée avec une rotation aléatoire.
        /// </summary>
        /// <param name="position">La position où faire apparaître le pilier</param>
        private void SpawnPillar(Vector3 position)
        {
            // Calculer la position finale avec l'offset
            Vector3 spawnPosition = position + spawnOffset;
            
            // Calculer la rotation aléatoire
            Quaternion rotation = CalculateRandomRotation();
            
            // Instancier le pilier
            Instantiate(pillarPrefab, spawnPosition, rotation, pillarsContainer);
            
            // Log pour debug
            Debug.Log($"Pilier spawné à {spawnPosition} avec rotation {rotation.eulerAngles}");
            
            // Appliquer le screenshake si activé
            if (enableScreenShake && CameraShake.Instance != null)
            {
                CameraShake.Instance.ShakeWithRotation(shakeDuration, shakePositionMagnitude, shakeRotationMagnitude);
            }
        }
        
        /// <summary>
        /// Calcule une rotation aléatoire basée sur les paramètres définis.
        /// </summary>
        /// <returns>La rotation calculée</returns>
        private Quaternion CalculateRandomRotation()
        {
            // Rotation aléatoire sur tous les axes
            float randomX = Random.Range(minAngleX, maxAngleX);
            float randomY = Random.Range(minAngleY, maxAngleY);
            float randomZ = Random.Range(minAngleZ, maxAngleZ);
            return Quaternion.Euler(randomX, randomY, randomZ);
        }
        
        /// <summary>
        /// Permet de faire apparaître manuellement un pilier (utile pour tester ou appeler depuis d'autres scripts).
        /// </summary>
        /// <param name="position">La position où faire apparaître le pilier</param>
        public void SpawnPillarManually(Vector3 position)
        {
            SpawnPillar(position);
        }
        
        /// <summary>
        /// Nettoie tous les piliers existants.
        /// </summary>
        public void ClearAllPillars()
        {
            if (pillarsContainer != null)
            {
                foreach (Transform child in pillarsContainer)
                {
                    Destroy(child.gameObject);
                }
            }
        }
    }
}
