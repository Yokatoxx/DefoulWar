using FPS;
using UnityEngine;

[RequireComponent(typeof(PlayerHealth))]
public class Death : MonoBehaviour
{
    [SerializeField] private Transform spawnRoot;

    private PlayerHealth playerHealth;
    private Rigidbody rb;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        rb = GetComponent<Rigidbody>();
        spawnRoot = spawnRoot != null ? spawnRoot : transform;
    }

    private void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnDeath.AddListener(HandleDeath);
        }
    }

    private void Start()
    {
        CacheSpawnPoint();
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnDeath.RemoveListener(HandleDeath);
        }
    }

    private void CacheSpawnPoint()
    {
        spawnPosition = spawnRoot.position;
        spawnRotation = spawnRoot.rotation;
    }

    public void SetSpawnPoint(Transform newSpawn)
    {
        if (newSpawn == null) return;
        spawnRoot = newSpawn;
        CacheSpawnPoint();
    }

    private void HandleDeath()
    {
        RespawnPlayer();
        playerHealth.ResetHealth();
    }

    private void RespawnPlayer()
    {
        if (rb != null)
        {
            // Reset velocity before teleporting
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            
            // Teleport using Rigidbody
            rb.position = spawnPosition;
            rb.rotation = spawnRotation;
        }
        else
        {
            transform.SetPositionAndRotation(spawnPosition, spawnRotation);
        }
    }
}
