// EnemyAI.cs
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyAI : MonoBehaviour
{
    [Header("Composants")]
    public NavMeshAgent agent;

    [Header("Horde Info")]
    // Empêche la sérialisation de la référence de horde dans le prefab/inspector.
    [HideInInspector, NonSerialized]
    public Horde currentHorde = null;

    [Header("Stats")]
    public float baseMoveSpeed = 3.5f;
    public float currentMoveSpeed;

    [Header("Spawn Info (optionnel)")]
    public Transform spawnPoint;

    // Event fired when the agent reaches its destination (within stoppingDistance)
    public event Action<Vector3> OnReachedDestination;

    private Coroutine _arrivalCheckerCoroutine;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        currentMoveSpeed = baseMoveSpeed;
        if (agent != null)
            agent.speed = currentMoveSpeed;

        // Force clear any serialized reference from prefab or previous state
        currentHorde = null;
        Debug.Log($"{name} Awake: currentHorde forced to null");
    }

    void OnEnable()
    {
        // Ensure registration with manager when enabled
        if (HordeManager.instance != null)
            HordeManager.instance.RegisterEnemy(this);
    }

    void Start()
    {
        if (HordeManager.instance != null)
        {
            HordeManager.instance.RegisterEnemy(this);
        }
    }

    void OnDisable()
    {
        if (HordeManager.instance != null)
        {
            HordeManager.instance.UnregisterEnemy(this);
        }
    }

    void OnDestroy()
    {
        if (HordeManager.instance != null)
        {
            if (currentHorde != null)
            {
                HordeManager.instance.LeaveHorde(this);
            }

            HordeManager.instance.UnregisterEnemy(this);
        }
    }

    void Update()
    {
        if (agent != null && agent.isActiveAndEnabled)
        {
            if (!Mathf.Approximately(agent.speed, currentMoveSpeed))
                agent.speed = currentMoveSpeed;
        }
    }

    public void MoveTo(Vector3 destination)
    {
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.SetDestination(destination);
        }
    }

    public void StopMoving()
    {
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.ResetPath();
        }
    }

    private IEnumerator CheckArrival(Vector3 destination)
    {
        yield return null;

        if (agent == null || !agent.isOnNavMesh)
            yield break;

        while (agent.pathPending || agent.remainingDistance > Mathf.Max(agent.stoppingDistance, 0.1f))
        {
            if (!agent.isActiveAndEnabled)
                yield break;
            yield return null;
        }

        _arrivalCheckerCoroutine = null;
        OnReachedDestination?.Invoke(destination);
    }
}