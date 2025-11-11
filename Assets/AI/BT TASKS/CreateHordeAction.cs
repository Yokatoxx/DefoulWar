using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

[Serializable, GeneratePropertyBag]
[NodeDescription(name: "CreateHordeAction", story: "Créer une horde", category: "Action", id: "678eb1b1b90854528da947e6518c0af0")]
public partial class CreateHordeAction : Action
{
    protected override Status OnStart() => Status.Running;

    protected override Status OnUpdate()
    {
        var go = this.GameObject;
        if (go == null)
        {
            Debug.LogWarning("[CreateHordeAction_Debug] GameObject null");
            return Status.Failure;
        }

        var enemy = go.GetComponent<EnemyAI>();
        if (enemy == null)
        {
            Debug.LogWarning("[CreateHordeAction_Debug] EnemyAI missing on " + go.name);
            return Status.Failure;
        }

        if (HordeManager.instance == null)
        {
            Debug.LogWarning("[CreateHordeAction_Debug] HordeManager.instance is null");
            return Status.Failure;
        }

        bool can = HordeManager.instance.CanFormNewHorde();
        Debug.Log($"[CreateHordeAction_Debug] CanFormNewHorde() => {can} for {go.name}");

        if (!can)
        {
            return Status.Failure;
        }

        if (enemy.currentHorde != null)
        {
            Debug.Log("[CreateHordeAction_Debug] " + go.name + " already in a horde.");
            return Status.Success;
        }

        HordeManager.instance.CreateHorde(enemy);
        Debug.Log("[CreateHordeAction_Debug] Created horde with founder: " + go.name);
        return Status.Success;
    }

    protected override void OnEnd() { }
}