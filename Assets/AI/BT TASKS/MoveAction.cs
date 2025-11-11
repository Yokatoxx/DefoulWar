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
    protected override Status OnStart()
    {
        Debug.Log($"[MoveToAction_Debug] OnStart for {this.GameObject?.name}");
        return Status.Running;
    }

    protected override Status OnUpdate()
    {
        var go = this.GameObject;
        if (go == null)
        {
            Debug.LogWarning("[MoveToAction_Debug] GameObject null");
            return Status.Failure;
        }

        var enemy = go.GetComponent<EnemyAI>();
        if (enemy == null)
        {
            Debug.LogWarning($"[MoveToAction_Debug] EnemyAI missing on {go.name}");
            return Status.Failure;
        }

        Vector3 target = TargetPos != null ? TargetPos.Value : Vector3.zero;
        Debug.Log($"[MoveToAction_Debug] {go.name} asked to MoveTo {target} (TargetPos variable {(TargetPos != null ? "OK" : "NULL")})");

        // Log agent state before calling MoveTo
        var agent = enemy.agent;
        if (agent == null)
        {
            Debug.LogWarning($"[MoveToAction_Debug] {go.name} has no NavMeshAgent");
            return Status.Failure;
        }

        Debug.Log($"[MoveToAction_Debug] Agent state: enabled={agent.enabled} isOnNavMesh={agent.isOnNavMesh} isStopped={agent.isStopped} speed={agent.speed} updatePosition={agent.updatePosition}");

        // Call EnemyAI.MoveTo (which we'll also instrument)
        try
        {
            enemy.MoveTo(target);
            Debug.Log($"[MoveToAction_Debug] MoveTo called for {go.name}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[MoveToAction_Debug] Exception calling MoveTo on {go.name}: {ex}");
            return Status.Failure;
        }

        return Status.Success;
    }

    protected override void OnEnd() { }
}