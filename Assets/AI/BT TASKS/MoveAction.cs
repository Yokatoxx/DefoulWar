using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "Move", story: "Déplacement vers", category: "Action", id: "516bef1be81c15a1a85bff2bfb4c5e14")]
public partial class MoveToAction : Action
{
    [SerializeReference, SerializeField]
    public BlackboardVariable<Vector3> TargetPos;
    // Optionnel : lier la variable NearestHorde si vous en avez une sur le blackboard
    [SerializeReference, SerializeField]
    public BlackboardVariable<GameObject> NearestHorde;
    public float updateThreshold = 0.5f;
    public float timeout = 12f;
    private bool _started;
    private bool _reached;
    private float _startTime;
    private Vector3 _lastTarget;

    protected override Status OnStart()
    {
        _started = false;
        _reached = false;
        _startTime = Time.time;
        Debug.Log("[MoveTo_safe] OnStart");
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        var go = this.GameObject;
        if (go == null)
        {
            Debug.LogWarning("[MoveTo_safe] GameObject is null");
            return Status.Failure;
        }

        var enemy = go.GetComponent<EnemyAI>();
        if (enemy == null)
        {
            Debug.LogWarning("[MoveTo_safe] EnemyAI missing on " + go.name);
            return Status.Failure;
        }

        if (TargetPos == null)
        {
            Debug.LogWarning("[MoveTo_safe] TargetPos blackboard var is null");
            return Status.Failure;
        }

        Vector3 target = TargetPos.Value;

        // Guard: si target == Vector3.zero et qu'il n'y a pas de nearest horde -> ne pas bouger
        bool hasNearest = (NearestHorde != null && NearestHorde.Value != null);
        if (target == Vector3.zero && !hasNearest && enemy.currentHorde == null)
        {
            Debug.Log($"[MoveTo_safe] {go.name} target is (0,0,0) and no nearest horde/current horde -> abort MoveTo");
            return Status.Failure;
        }

        // Guard: NavMeshAgent present and on NavMesh
        if (enemy.agent == null)
        {
            Debug.LogWarning("[MoveTo_safe] NavMeshAgent is null on " + go.name);
            return Status.Failure;
        }
        if (!enemy.agent.isOnNavMesh)
        {
            Debug.LogWarning("[MoveTo_safe] Agent NOT on NavMesh for " + go.name + " — isOnNavMesh=false");
            return Status.Failure;
        }

        // Start or update move if target moved enough
        if (!_started || Vector3.Distance(target, _lastTarget) > updateThreshold)
        {
            _lastTarget = target;
            _started = true;
            _startTime = Time.time;
            _reached = false;

            Debug.Log($"[MoveTo_safe] {go.name} MoveTo -> {target}");
            enemy.MoveTo(target);

            enemy.OnReachedDestination -= OnArrived;
            enemy.OnReachedDestination += OnArrived;
        }

        // Timeout
        if (Time.time - _startTime > timeout)
        {
            Debug.LogWarning($"[MoveTo_safe] Timeout for {go.name} after {timeout}s (target {_lastTarget})");
            enemy.OnReachedDestination -= OnArrived;
            enemy.StopMoving();
            return Status.Failure;
        }

        if (_reached)
        {
            Debug.Log($"[MoveTo_safe] {go.name} reached {_lastTarget}");
            enemy.OnReachedDestination -= OnArrived;
            return Status.Success;
        }

        return Status.Running;
    }

    protected override void OnEnd()
    {
        var go = this.GameObject;
        if (go == null) return;
        var enemy = go.GetComponent<EnemyAI>();
        if (enemy != null)
        {
            enemy.OnReachedDestination -= OnArrived;
        }
        Debug.Log("[MoveTo_safe] OnEnd");
    }

    private void OnArrived(Vector3 dest)
    {
        _reached = true;
        Debug.Log("[MoveTo_safe] OnArrived event fired for dest " + dest);
    }
}