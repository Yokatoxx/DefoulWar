using FPS;
using UnityEngine;

[RequireComponent(typeof(PlayerHealth))]
public class Death : MonoBehaviour
{
    [SerializeField] private Transform spawnRoot;

    private PlayerHealth playerHealth;
    private CharacterController characterController;
    private Vector3 spawnPosition;
    private Quaternion spawnRotation;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        playerHealth = GetComponent<PlayerHealth>();
        characterController = GetComponent<CharacterController>();
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
        bool controllerInitiallyEnabled = characterController != null && characterController.enabled;
        if (characterController != null)
        {
            characterController.enabled = false;
        }

        transform.SetPositionAndRotation(spawnPosition, spawnRotation);

        if (controllerInitiallyEnabled)
        {
            characterController.enabled = true;
        }
    }
}
