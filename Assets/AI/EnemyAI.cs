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
    }

    void OnEnable()
    {
        // Ensure registration with manager when enabled
        if (HordeManager.instance != null)
            HordeManager.instance.RegisterEnemy(this);
    }

    void Start()
    {
        // Defensive: Start also registers, but OnEnable handles most cases
        if (HordeManager.instance != null)
        {
            HordeManager.instance.RegisterEnemy(this);
        }
    }

    void OnDisable()
    {
        // If disabled but not destroyed, unregister from alone list
        if (HordeManager.instance != null)
        {
            HordeManager.instance.UnregisterEnemy(this);
        }
    }

    void OnDestroy()
    {
        // Important: leave horde and unregister
        if (HordeManager.instance != null)
        {
            if (currentHorde != null)
            {
                // use LeaveHorde to trigger proper cleanup and buff removal
                HordeManager.instance.LeaveHorde(this);
            }

            HordeManager.instance.UnregisterEnemy(this);
        }
    }

    void Update()
    {
        // Keep agent speed in sync with currentMoveSpeed (buffs/debuffs)
        if (agent != null && agent.isActiveAndEnabled)
        {
            if (!Mathf.Approximately(agent.speed, currentMoveSpeed))
                agent.speed = currentMoveSpeed;
        }

        // Optional: behavior tree controls movement. Keep local logic light.
    }

    // --- Movement API used by Behavior Tree actions ---
    public void MoveTo(Vector3 destination)
    {
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.SetDestination(destination);
            if (_arrivalCheckerCoroutine != null)
                StopCoroutine(_arrivalCheckerCoroutine);
            _arrivalCheckerCoroutine = StartCoroutine(CheckArrival(destination));
        }
    }

    public void StopMoving()
    {
        if (agent != null && agent.isActiveAndEnabled)
        {
            agent.ResetPath();
            if (_arrivalCheckerCoroutine != null)
            {
                StopCoroutine(_arrivalCheckerCoroutine);
                _arrivalCheckerCoroutine = null;
            }
        }
    }

    private IEnumerator CheckArrival(Vector3 destination)
    {
        // Wait until agent path is computed
        yield return null;

        // If no path or agent inactive, exit
        if (agent == null || !agent.isOnNavMesh)
            yield break;

        // Loop until reached (or path invalid)
        while (agent.pathPending || agent.remainingDistance > Mathf.Max(agent.stoppingDistance, 0.1f))
        {
            // If no path or agent disabled, stop checking
            if (!agent.isActiveAndEnabled)
                yield break;
            yield return null;
        }

        // Reached
        _arrivalCheckerCoroutine = null;
        OnReachedDestination?.Invoke(destination);
    }
}