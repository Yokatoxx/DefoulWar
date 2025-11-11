using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "SetTargetToRallyPoint", story: "SetTargetToRallyPoint", category: "Action", id: "c0a49b5a826cef481df49109e5d265d2")]
public partial class SetTargetToRallyPointAction : Action
{
    public BlackboardVariable<Vector3> TargetPos;

    protected override Status OnStart() => Status.Running;

    protected override Status OnUpdate()
    {
        var go = this.GameObject;
        if (go == null) return Status.Failure;

        var enemy = go.GetComponent<EnemyAI>();
        if (enemy == null) return Status.Failure;

        if (enemy.currentHorde == null) return Status.Failure;

        if (TargetPos != null)
            TargetPos.Value = enemy.currentHorde.rallyPoint;

        return Status.Success;
    }

    protected override void OnEnd() { }
}

