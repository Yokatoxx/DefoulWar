using System.Collections;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Spawner simple: maintient un seul ennemi en vie. Quand l'instance est détruite (null),
/// elle est réapparue après un délai configurable.
/// </summary>
[DisallowMultipleComponent]
[AddComponentMenu("Game/Enemy Spawner")]
public class EnemySpawner : MonoBehaviour
{
    [Header("Réglages")]
    [Tooltip("Prefab de l'ennemi à instancier.")]
    [SerializeField] private GameObject enemyPrefab;

    [Tooltip("Point de spawn (position/rotation). Si vide, utilise le transform du spawner.")]
    [SerializeField] private Transform spawnPoint;

    [Tooltip("Temps d'attente avant de respawn (en secondes) après destruction.")]
    [Min(0f)]
    [SerializeField] private float respawnDelay = 3f;

    [Tooltip("Instancier automatiquement au démarrage.")]
    [SerializeField] private bool spawnOnStart = true;

    [Tooltip("Parenté l'ennemi au spawner pour garder la hiérarchie propre.")]
    [SerializeField] private bool parentToSpawner;

    // Instance courante gérée par ce spawner
    private GameObject currentInstance;
    private bool isRespawning;

    private Coroutine watchRoutine;

    private void Start()
    {
        if (spawnOnStart)
        {
            Spawn();
        }
        watchRoutine = StartCoroutine(WatchAndRespawn());
    }

    private void OnEnable()
    {
        if (Application.isPlaying && watchRoutine == null)
        {
            watchRoutine = StartCoroutine(WatchAndRespawn());
        }
    }

    private void OnDisable()
    {
        if (watchRoutine != null)
        {
            StopCoroutine(watchRoutine);
            watchRoutine = null;
        }
        isRespawning = false;
    }

    /// <summary>
    /// Instancie l'ennemi si aucune instance n'existe déjà.
    /// </summary>
    public void Spawn()
    {
        if (enemyPrefab == null)
        {
            Debug.LogWarning($"[{nameof(EnemySpawner)}] Aucun prefab assigné.", this);
            return;
        }
        if (currentInstance != null)
        {
            // Une instance existe déjà; ne rien faire.
            return;
        }

        Transform point = spawnPoint != null ? spawnPoint : transform;
        Transform parent = parentToSpawner ? transform : null;

        currentInstance = Instantiate(enemyPrefab, point.position, point.rotation, parent);
        DisableNavMeshAgents(currentInstance);
    }

    /// <summary>
    /// Détruit l'instance courante si elle existe (utile pour test).
    /// </summary>
    [ContextMenu("Force Kill Current")]
    public void ForceKill()
    {
        if (currentInstance != null)
        {
            Destroy(currentInstance);
            // currentInstance deviendra null à la frame suivante
        }
    }

    [ContextMenu("Spawn Now")] 
    private void SpawnNowContext()
    {
        Spawn();
    }

    /// <summary>
    /// Retourne l'instance courante (peut être null si détruite).
    /// </summary>
    public GameObject CurrentInstance => currentInstance;

    private IEnumerator WatchAndRespawn()
    {
        // Boucle légère qui surveille la destruction de l'instance
        while (enabled && gameObject.activeInHierarchy)
        {
            // Si l'instance a été détruite (référence null) et qu'on n'est pas déjà en attente de respawn
            if (currentInstance == null && !isRespawning)
            {
                isRespawning = true;

                // Attendre le délai demandé
                if (respawnDelay > 0f)
                    yield return new WaitForSeconds(respawnDelay);
                else
                    yield return null; // laisser au moins une frame

                // Conditions toujours valides ?
                if (currentInstance == null && enabled && gameObject.activeInHierarchy)
                {
                    Spawn();
                }

                isRespawning = false;
            }

            yield return null; // vérifie à chaque frame, coût négligeable
        }

        watchRoutine = null;
    }

    private void DisableNavMeshAgents(GameObject go)
    {
        if (go == null) return;
        var agents = go.GetComponentsInChildren<NavMeshAgent>(true);
        for (int i = 0; i < agents.Length; i++)
        {
            if (agents[i] != null) agents[i].enabled = false;
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Transform point = spawnPoint != null ? spawnPoint : transform;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(point.position, 0.25f);
        Gizmos.DrawRay(point.position, point.forward * 0.75f);
    }
#endif
}
